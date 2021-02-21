namespace atlantis
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using atlantis.Persistence;
    using HotChocolate.Types.Relay;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Model;

    using RegionDic = System.Collections.Generic.Dictionary<string, Persistence.DbRegion>;

    [Route("report")]
    public class ApiController : ControllerBase {
        public ApiController(Database db, IIdSerializer relayId) {
            this.db = db;
            this.relayId = relayId;
        }

        private readonly Database db;
        private readonly IIdSerializer relayId;

        [HttpPost("{gameId}")]
        public async Task<IActionResult> UploadReports([Required, FromRoute] string gameId) {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            if (Request.Form.Files.Count == 0) return UnprocessableEntity();

            DbGame game = await FindGameAsync(db, relayId, gameId);
            if (game == null) return NotFound();

            // 1. upload reports
            int earliestTurn = int.MaxValue;
            foreach (var file in Request.Form.Files) {
                await using var stream = file.OpenReadStream();
                var turnNumber = await LoadReportFileAsync(db, stream, game);

                earliestTurn = Math.Min(earliestTurn, turnNumber);
            }
            await db.SaveChangesAsync();

            // 2. parse & merge reports and update history
            await UpdateTurnsAsync(db, game.Id, earliestTurn);

            return Ok();
        }

        private static Task<DbGame> FindGameAsync(Database db, IIdSerializer relayId, string gameId) {
            var id = (long) relayId.Deserialize(gameId).Value;

            return db.Games.SingleOrDefaultAsync(x => x.Id == id);
        }

        private static async Task<int> LoadReportFileAsync(Database db, Stream reportStream, DbGame game) {
            using var textReader = new StreamReader(reportStream);

            using var atlantisReader = new AtlantisReportJsonConverter(textReader);
            var jObj = await atlantisReader.ReadAsJsonAsync();
            dynamic json = jObj;

            string factionName = json.faction.name;
            int factionNumber = json.faction.number;
            int year = json.date.year;
            int month = json.date.month;
            int turnNumber = month + (year - 1) * 12;

            reportStream.Seek(0, SeekOrigin.Begin);
            var source = await textReader.ReadToEndAsync();

            var turn = await db.Turns
                .FirstOrDefaultAsync(x => x.GameId == game.Id && x.Number == turnNumber);
            DbReport report = null;

            if (turn == null) {
                turn = new DbTurn {
                    GameId = game.Id,
                    Month = month,
                    Year = year,
                    Number = turnNumber
                };

                game.Turns.Add(turn);
            }
            else {
                report = await db.Reports
                    .FirstOrDefaultAsync(x => x.FactionNumber == factionNumber && x.TurnId == turn.Id);
            }

            if (report == null) {
                report = new DbReport {
                    GameId = game.Id,
                    TurnId = turn.Id
                };

                game.Reports.Add(report);
                turn.Reports.Add(report);
            }

            report.FactionNumber = factionNumber;
            report.FactionName = factionName;
            report.Source = source;
            report.Json = jObj.ToString(Formatting.Indented);

            game.PlayerFactionNumber ??= report.FactionNumber;
            if (report.FactionNumber == game.PlayerFactionNumber && turnNumber > game.LastTurnNumber) {
                game.PlayerFactionName = report.FactionName;
                game.LastTurnNumber = turnNumber;
            }

            game.EngineVersion ??= json.engine?.version;
            game.RulesetName ??= json.engine?.ruleset?.name;
            game.RulesetVersion ??= json.engine?.ruleset?.version;
            game.Password ??= json.ordersTemplate?.password;

            return turnNumber;
        }

        private static async Task UpdateTurnsAsync(Database db, long gameId, int earliestTurn) {
            var lastTurn = await db.Turns
                .OrderByDescending(x => x.Number)
                .Where(x => x.GameId == gameId)
                .Select(x => x.Number)
                .FirstOrDefaultAsync();

            if (lastTurn > earliestTurn) {
                lastTurn = earliestTurn - 1;
            }

            var turns = await db.Turns
                .Where(x => x.GameId == gameId && x.Number >= lastTurn)
                .OrderBy(x => x.Number)
                .Select(x => new { x.Id, x.Number })
                .AsNoTracking()
                .ToListAsync();

            RegionDic regions = new RegionDic();
            if (earliestTurn > 1) {
                regions = (await db.Regions
                        .Where(x => x.TurnId == turns[0].Id)
                        .AsNoTracking()
                        .ToArrayAsync())
                    .ToDictionary(k => k.UID);
            }

            foreach (var turn in turns) {
                regions = await UpdateTurnAsync(db, turn.Id, turn.Number, regions);

                await RemoveRegions(db, turn.Id);
                await InsertRegions(db, turn.Id, regions.Values);
            }
        }

        // This method will be memory intensive as it must merge all reports into one
        private static async Task<RegionDic> UpdateTurnAsync(Database db, long turnId, int turnNumber, RegionDic regionsIn) {
            RegionDic regions = regionsIn
                .ToDictionary(k => k.Key, v => CopyRegion(v.Value));

            var reports = db.Reports
                .Where(x => x.TurnId == turnId)
                .AsNoTracking()
                .AsAsyncEnumerable();

            await foreach (var report in reports) {
                var json = JObject.Parse(report.Json);
                var jsonRegions = json["regions"] as JArray;

                foreach (JObject region in jsonRegions) {
                    CreateOrUpdateRegion(turnNumber, regions, region.ToObject<JRegion>());
                }
            }

            return regions;
        }

        private static DbRegion CopyRegion(DbRegion region) {
            return new DbRegion {
                X = region.X,
                Y = region.Y,
                Z = region.Z,
                Label = region.Label,
                UpdatedAtTurn = region.UpdatedAtTurn,
                Province = region.Province,
                Terrain = region.Terrain,
                Tax = region.Tax,
                Wages = region.Wages,
                TotalWages = region.TotalWages,
                Entertainment = region.Entertainment,
                Exits = region.Exits.Select(x => new DbRegionExit { RegionUID = x.RegionUID }).ToList(),
                Products = region.Products.Select(x => new DbItem { Code = x.Code, Amount = x.Amount }).ToList(),
                ForSale = region.ForSale.Select(x => new DbTradableItem { Code = x.Code, Amount = x.Amount, Price = x.Price }).ToList(),
                Wanted = region.Wanted.Select(x => new DbTradableItem { Code = x.Code, Amount = x.Amount, Price = x.Price }).ToList()
            };
        }

        private static void CreateOrUpdateRegion(int turnNumber, RegionDic regions, JRegion source) {
            var x = source.Coords.X;
            var y = source.Coords.Y;
            var z = source.Coords.Z ?? 1;

            var uid = DbRegion.GetUID(x, y, z);

            DbRegion region;
            if (!regions.TryGetValue(uid, out region)) {
                region = new DbRegion();
                regions.Add(uid, region);
            }

            region.UpdatedAtTurn = turnNumber;
            region.X = x;
            region.Y = y;
            region.Z = z;
            region.Label = source.Coords.Label ?? "surface";
            region.Province = source.Province;
            region.Terrain = source.Terrain;
            region.Population = source.Population?.Amount ?? 0;
            region.Race = source.Population?.Race;
            region.Tax = source.Tax ?? 0;
            region.Entertainment = source.Entertainment ?? 0;
            region.Wages = source.Wages;
            region.TotalWages = source.TotalWages ?? 0;

            SetRegionItems(region.Products, source.Products);
            SetRegionItems(region.Wanted, source.Wanted);
            SetRegionItems(region.ForSale, source.ForSale);
        }

        private static async Task RemoveRegions(Database db, long turnId) {
            await db.Database
                .ExecuteSqlRawAsync($"delete from {db.Model.FindEntityType(typeof(DbRegion)).GetTableName()} where TurnId = {turnId}");
        }

        private static async Task InsertRegions(Database db, long turnId, IEnumerable<DbRegion> regions) {
            foreach (var region in regions) {
                region.TurnId = turnId;

                foreach (var wanted in region.Wanted) {
                    wanted.TurnId = turnId;
                }

                foreach (var forSale in region.ForSale) {
                    forSale.TurnId = turnId;
                }

                foreach (var product in region.Products) {
                    product.TurnId = turnId;
                }

                await db.AddAsync(region);
            }

            await db.SaveChangesAsync();
        }

        public static void SetRegionItems(ICollection<DbItem> dbItems, IEnumerable<JItem> items) {
            var dest = dbItems.ToDictionary(x => x.Code);

            foreach (var item in items) {
                if (!dest.TryGetValue(item.Code, out var i)) {
                    i = new DbItem();
                    dbItems.Add(i);
                }

                i.Code = item.Code;
                i.Amount = item.Amount;
            }
        }

        public static void SetRegionItems(ICollection<DbTradableItem> dbItems, IEnumerable<JTradableItem> items) {
            var dest = dbItems.ToDictionary(x => x.Code);

            foreach (var item in items) {
                if (!dest.TryGetValue(item.Code, out var i)) {
                    i = new DbTradableItem();
                    dbItems.Add(i);
                }

                i.Code = item.Code;
                i.Amount = item.Amount;
                i.Price = item.Price;
            }
        }
    }
}
