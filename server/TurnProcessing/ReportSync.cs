namespace advisor.TurnProcessing;

using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using AutoMapper;
using advisor.Model;
using advisor.Persistence;

using RegionDic = System.Collections.Generic.Dictionary<string, Persistence.DbRegion>;
using FactionsDic = System.Collections.Generic.Dictionary<int, Persistence.DbFaction>;
using UnitsDic = System.Collections.Generic.Dictionary<int, Persistence.DbUnit>;
using StructuresDic = System.Collections.Generic.Dictionary<string, Persistence.DbStructure>;
using Microsoft.EntityFrameworkCore;

public class ReportSync : InTurnContext {
    public ReportSync(Database db, long playerId, int turnNumber, JReport report) {
        Db = db;
        PlayerId = playerId;
        TurnNumber = turnNumber;
        Report = report;
    }

    int InTurnContext.TurnNumber { get => this.TurnNumber; set { } }

    long InPlayerContext.PlayerId { get => this.PlayerId; set { } }

    public Database Db { get; }
    public long PlayerId { get; }
    public int TurnNumber { get; }
    public JReport Report { get; }
    public RegionDic Regions { get; } = [];
    public StructuresDic Structures { get; } = [];
    public UnitsDic Units { get; } = [];
    public FactionsDic Factions { get; } = [];

    public HashSet<string> ProcessedShips = [];
    public Dictionary<string, DbStructure> PendingShips = [];

    public HashSet<int> NewFactions { get; } = [];
    public HashSet<string> NewRegions { get; } = [];
    public HashSet<string> NewStructures { get; } = [];
    public HashSet<int> NewUnits { get; } = [];

    public void Load(DbPlayerTurn turn) {
        foreach (var region in turn.Regions) {
            Regions.Add(region.Id, region);
        }

        foreach (var str in turn.Structures) {
            Structures.Add(str.Id, str);
        }

        foreach (var fac in turn.Factions) {
            Factions.Add(fac.Number, fac);
        }

        foreach (var unit in turn.Units) {
            Units.Add(unit.Number, unit);
        }

        foreach (var order in Report.OrdersTemplate?.Units ?? Enumerable.Empty<JUnitOrders>()) {
            if (Units.TryGetValue(order.Unit, out var unit)) {
                unit.Orders = order.Orders;
            }
        }
    }

    public void Copy(DbPlayerTurn turn, IMapper mapper) {
        foreach (var region in turn.Regions.Select(mapper.Map<DbRegion>)) {
            region.Units.Clear();

            Regions.Add(region.Id, region);
            NewRegions.Add(region.Id);

            foreach (var str in region.Structures.ToList()) {
                str.Units.Clear();

                if (DbStructure.IsShip(str.Number)) {
                    region.Structures.Remove(str);
                    continue;
                }

                Structures.Add(str.Id, str);
                NewStructures.Add(str.Id);
            }
        }

        foreach (var fac in turn.Factions.Select(mapper.Map<DbFaction>)) {
            fac.Events.Clear();
            fac.Units.Clear();

            var attitudes = fac.Attitudes.Select(x => new DbAttitude {
                FactionNumber = x.FactionNumber,
                Stance = x.Stance,
                TargetFactionNumber = x.TargetFactionNumber
            }).ToList();

            fac.Attitudes.Clear();
            fac.Attitudes.AddRange(attitudes);

            Factions.Add(fac.Number, fac);
            NewFactions.Add(fac.Number);
        }
    }

    public async Task SyncReportAsync() {
        SyncFactions();
        SyncAttitudes();
        SyncRegions();
        SyncEvents();

        await SyncBattles();

        await ApplyFactionsAsync();
        await ApplyRegionsAsync();
    }

    private async Task ApplyFactionsAsync() {
        foreach (var faction in Factions.Values) {
            ApplyTurnContext(faction);

            foreach (var ev in faction.Events) {
                ApplyTurnContext(ev);
            }

            foreach (var attitude in faction.Attitudes) {
                ApplyTurnContext(attitude);
            }
        }

        foreach (var faction in Factions.Values.Where(x => NewFactions.Contains(x.Number))) {
            await Db.AddAsync(faction);
        }
    }

    private async Task ApplyRegionsAsync() {
        foreach (var region in Regions.Values) {
            ApplyTurnContext(region);

            foreach (var exit in region.Exits) {
                ApplyTurnContext(exit);
            }

            foreach (var item in region.Markets) {
                ApplyTurnContext(item);
            }

            foreach (var item in region.Produces) {
                ApplyTurnContext(item);
            }

            foreach (var unit in region.Units) {
                ApplyTurnContext(unit);

                foreach (var item in unit.Items) {
                    ApplyTurnContext(item);
                }
            }

            foreach (var str in region.Structures) {
                ApplyTurnContext(str);
            }
        }

        foreach (var region in Regions.Values.Where(x => NewRegions.Contains(x.Id))) {
            await Db.AddAsync(region);
        }
    }

    private void ApplyTurnContext(InTurnContext ctx) {
        ctx.PlayerId = PlayerId;
        ctx.TurnNumber = TurnNumber;
    }

    private DbFaction GetFaction(int number) {
        return Factions[number];
    }

    private DbRegion GetRegion(JCoords coords) {
        return Regions[DbRegion.MakeId(coords.X, coords.Y, coords.Z)];
    }

    private DbUnit GetUnit(int number) {
        return Units.TryGetValue(number, out var unit) ? unit : null;
    }

    private void SyncFactions() {
        var factions = new List<JFaction>();

        var allAttitudes = Report.OtherReports.Select(x => x.Attitudes).Concat(new[] { Report.Attitudes });
        foreach (var attitudes in allAttitudes) {
            factions.AddRange(attitudes.Ally);
            factions.AddRange(attitudes.Friendly);
            factions.AddRange(attitudes.Neutral);
            factions.AddRange(attitudes.Unfriendly);
            factions.AddRange(attitudes.Hostile);
        }

        factions.Add(Report.Faction);
        factions.AddRange(Report.OtherReports.Select(x => x.Faction));

        foreach (var faction in factions) {
            SyncFaction(faction);
        }
    }

    private void SyncAttitudes() {
        var faction = Report.Faction;
        SyncAttitudes(GetFaction(faction.Number), faction, Report.Attitudes);

        foreach (var other in Report.OtherReports) {
            faction = other.Faction;
            SyncAttitudes(GetFaction(faction.Number), faction, other.Attitudes);
        }
    }

    private void SyncAttitudes(DbFaction faction, JFaction source, JAttitudes attitudes) {
        void syncStances(Stance stance, IEnumerable<DbAttitude> current, IEnumerable<int> incoming) {
            var index = current.Where(x => x.Stance == stance).ToDictionary(x => x.TargetFactionNumber);

            var toRemove = index.Keys.Except(incoming).ToArray();
            var toAdd = incoming.Except(index.Keys).ToArray();

            foreach (var number in toAdd) {
                faction.Attitudes.Add(new DbAttitude {
                    Stance = stance,
                    TargetFactionNumber = number
                });
            }

            foreach (var number in toRemove) {
                var attitude = index[number];
                faction.Attitudes.Remove(attitude);

                if (attitude.TurnNumber != default) {
                    Db.Remove(attitude);
                }
            }
        }

        faction.DefaultAttitude = attitudes.Default;

        syncStances(Stance.Ally, faction.Attitudes, attitudes.Ally.Select(x => x.Number));
        syncStances(Stance.Friendly, faction.Attitudes, attitudes.Friendly.Select(x => x.Number));
        syncStances(Stance.Neutral, faction.Attitudes, attitudes.Neutral.Select(x => x.Number));
        syncStances(Stance.Unfriendly, faction.Attitudes, attitudes.Unfriendly.Select(x => x.Number));
        syncStances(Stance.Hostile, faction.Attitudes, attitudes.Hostile.Select(x => x.Number));
    }

    private void SyncRegions() {
        foreach (var region in Report.Regions) {
            SyncRegion(region);
        }

        foreach (var region in Report.Regions) {
            SyncExits(region);
        }
    }

    private void SyncEvents() {
        SyncEvents(Report.Faction.Number, Report.Errors, Report.Events);

        foreach (var othetReport in Report.OtherReports) {
            SyncEvents(othetReport.Faction.Number, othetReport.Errors, othetReport.Events);
        }
    }

    private async Task SyncBattles() {
        var knownBattles = (await Db.Battles
            .AsNoTracking()
            .InTurn(this)
            .Select(x => $"{x.X}-{x.Y}-{x.Z}-{x.Attacker.Number}-{x.Defender.Number}")
            .ToListAsync())
            .ToHashSet();

        foreach (var battle in Report.Battles) {
            var loc = battle.Location;
            var key = $"{loc.Coords.X}-{loc.Coords.Y}-{loc.Coords.Z}-{battle.Attacker.Number}-{battle.Defender.Number}";

            RegionFromBattle(battle);

            if (knownBattles.Contains(key)) {
                continue;
            }

            await Db.Battles.AddAsync(new DbBattle {
                TurnNumber = TurnNumber,
                PlayerId = PlayerId,
                X = loc.Coords.X,
                Y = loc.Coords.Y,
                Z = loc.Coords.Z,
                Terrain = loc.Terrain,
                Province = loc.Province,
                Label = loc.Coords.Label,
                Attacker = new DbArmy {
                    Number = battle.Attacker.Number,
                    Name = battle.Attacker.Name
                },
                Defender = new DbArmy {
                    Number = battle.Defender.Number,
                    Name = battle.Defender.Name
                },
                Battle = battle
            });
        }
    }

    private DbFaction SyncFaction(JFaction source) {
        if (!Factions.TryGetValue(source.Number, out var faction)) {
            faction = new DbFaction {
                Number = source.Number
            };

            Factions.Add(faction.Number, faction);
            NewFactions.Add(faction.Number);
        }

        faction.Name = source.Name;

        return faction;
    }

    private DbRegion SyncRegion(JRegion source) {
        var x = source.Coords.X;
        var y = source.Coords.Y;
        var z = source.Coords.Z;

        var id = DbRegion.MakeId(x, y, z);

        DbRegion region;
        if (!Regions.TryGetValue(id, out region)) {
            region = new DbRegion { Id = id };

            Regions.Add(id, region);
            NewRegions.Add(id);
        }

        var containsOwnUnits = source.Units.Any(x => x.Own) || source.Structures.Any(s => s.Units.Any(x => x.Own));
        if (containsOwnUnits) {
            region.LastVisitedAt = TurnNumber;
        }

        region.Explored = true;
        region.X = x;
        region.Y = y;
        region.Z = z;
        region.Label = source.Coords.Label;
        region.Province = source.Province;
        region.Terrain = source.Terrain;
        region.Gate ??= source.Gate;

        if (source.Settlement != null) {
            region.Settlement ??= new DbSettlement();
            region.Settlement.Name = source.Settlement.Name;
            region.Settlement.Size = source.Settlement.Size;
        }
        else {
            region.Settlement = null;
        }

        region.Population = source.Population?.Amount ?? 0;
        region.Race = source.Population?.Race;
        region.Tax = source.Tax;
        region.Entertainment = source.Entertainment;
        region.Wages = source.Wages;
        region.TotalWages = source.TotalWages;

        SetItems(region.Produces, source.Products);
        SetMarketItems(Market.WANTED, region.Markets, source.Wanted);
        SetMarketItems(Market.FOR_SALE, region.Markets, source.ForSale);

        int unitSeq = 0;
        foreach (var unit in source.Units) {
            SyncUnit(unit, unitSeq++, region: region, structure: null);
        }

        int structureSeq = 0;

        HashSet<string> ships = new HashSet<string>();
        foreach (var str in source.Structures) {
            var dbStr = SyncStructure(str, structureSeq++, region);
            if (DbStructure.IsShip(dbStr.Number)) {
                ships.Add(dbStr.Id);
                ProcessedShips.Add(dbStr.Id);
            }

            foreach (var unit in str.Units) {
                var strUnit = SyncUnit(unit, unitSeq++, region: region, structure: dbStr);
            }
        }

        // remove ships that are not present anymore
        for (int i = region.Structures.Count - 1; i >= 0 ; i--) {
            var str = region.Structures[i];
            if (DbStructure.IsBuilding(str.Number)) {
                continue;
            }

            if (!ships.Contains(str.Id)) {
                PendingShips.Add(str.Id, str);

                region.Structures.RemoveAt(i);
                str.RegionId = null;
                str.Region = null;
            }
        }

        return region;
    }

    private DbStructure SyncStructure(JStructure source, int seq, DbRegion region) {
        string id = DbStructure.MakeId(source.Structure.Number, region.Id);
        if (!Structures.TryGetValue(id, out var str)) {
            str = new DbStructure { Id = id };
            Structures.Add(id, str);

            str.RegionId = region.Id;
            str.Region = region;
            region.Structures.Add(str);

            NewStructures.Add(str.Id);
        }
        else if (str?.RegionId != region.Id) {
            if (PendingShips.ContainsKey(id)) {
                PendingShips.Remove(id);
            }

            if (str.Region != null) {
                str.Region.Structures.Remove(str);
            }

            str.RegionId = region.Id;
            str.Region = region;
            region.Structures.Add(str);
        }

        var info = source.Structure;

        str.Sequence = seq;
        str.Number = info.Number;
        str.Name = info.Name;
        str.Type = info.Type;
        str.Description = info.Description;
        str.X = region.X;
        str.Y = region.Y;
        str.Z = region.Z;

        str.Speed = info.Speed ?? str.Speed;
        str.Needs = info.Needs;

        if (info.Load != null) {
            str.Load ??= new DbTransportationLoad();
            str.Load.Used = info.Load.Used;
            str.Load.Max = info.Load.Max;
        }

        if (info.Sailors != null) {
            str.Sailors ??= new DbSailors();
            str.Sailors.Current = info.Sailors.Current;
            str.Sailors.Required = info.Sailors.Required;
        }

        SetFleetContent(str.Contents, info.Contents);

        if (info.Flags.Count > 0) {
            str.Flags.Clear();
            str.Flags.AddRange(info.Flags);
        }

        if (info.SailDirections.Count > 0) {
            str.SailDirections.Clear();
            str.SailDirections.AddRange(info.SailDirections);
        }

        return str;
    }

    private DbUnit SyncUnit(JUnit source, int seq, DbRegion region, DbStructure structure) {
        if (!Units.TryGetValue(source.Number, out var unit)) {
            unit = new DbUnit {
                Number = source.Number
            };

            unit.Region = region;
            unit.RegionId = region.Id;
            region.Units.Add(unit);

            if (structure != null) {
                unit.StrcutureId = structure.Id;
                unit.StructureNumber = structure.Number;
                unit.Structure = structure;
                structure.Units.Add(unit);
            }

            Units.Add(unit.Number, unit);
            NewUnits.Add(unit.Number);
        }

        if (source.Faction != null && unit.Faction == null) {
            var faction = SyncFaction(source.Faction);

            unit.Faction = faction;
            faction.Units.Add(unit);
        }

        unit.X = region.X;
        unit.Y = region.Y;
        unit.Z = region.Z;
        unit.Sequence = seq;
        unit.Name = source.Name;
        unit.Description = source.Description;
        unit.OnGuard = source.OnGuard;
        unit.Flags = source.Flags.ToList();
        unit.Weight = source.Weight;
        unit.IsMage = source.Skills
            .Select(x => x.Code.ToLower())
            .Any(x => x == "forc" || x == "patt" || x == "spir");

        if (source.Capacity != null) {
            unit.Capacity = new DbCapacity {
                Walking = source.Capacity.Walking,
                Swimming = source.Capacity.Swimming,
                Riding = source.Capacity.Riding,
                Flying = source.Capacity.Flying
            };
        }

        if (source.ReadyItem != null) {
            unit.ReadyItem = source.ReadyItem.Code;
        }

        if (source.CombatSpell != null) {
            unit.CombatSpell = source.CombatSpell.Code;
        }

        if (unit.CanStudy.Count < source.CanStudy.Count) {
            unit.CanStudy.Clear();
            unit.CanStudy.AddRange(source.CanStudy.Select(x => x.Code));
        }

        SetItems(unit.Items, source.Items);
        SetSkills(unit.Skills, source.Skills);

        return unit;
    }

    // todo: event sync must take inot account new report data
    private void SyncEvents(int factionNumber, IEnumerable<string> errors, IEnumerable<JEvent> events) {
        var faction = GetFaction(Report.Faction.Number);

        if (faction.Events.Count > 0) {
            Db.Events.RemoveRange(faction.Events);
        }

        foreach (var error in errors) {
            faction.Events.Add(new DbEvent {
                Type = EventType.Error,
                Message = error
            });
        }

        foreach (var ev in events) {
            var region = ev.Coords == null
                ? null
                : GetRegion(ev.Coords);

            var unit = GetUnit(ev.Unit?.Number ?? 0);

            var dbEv = new DbEvent {
                Faction = faction,
                Type = EventType.Info,
                Category = ev.Category,
                Amount = ev.Amount,
                ItemCode = ev.Code,
                ItemName = ev.Name,
                ItemPrice = ev.Price,
                Message = ev.Message,
                RegionId = region?.Id,
                Region = region
            };

            if (unit != null) {
                dbEv.Unit = unit;
                dbEv.UnitNumber = unit.Number;
                dbEv.UnitName = unit.Name;
            }
            else {
                dbEv.MissingUnitNumber = ev.Unit?.Number;
                dbEv.UnitName = ev.Unit?.Name;
            }

            faction.Events.Add(dbEv);
        }
    }

    private static void SetItems<T>(
        ICollection<T> dbItems,
        IEnumerable<JItem> items,
        Func<T> factory,
        Action<JItem, T> mapper)
    where T: AnItem {
        var dest = dbItems.ToDictionary(x => x.Code);

        foreach (var item in items) {
            if (!dest.TryGetValue(item.Code, out var d)) {
                d = factory();
                dbItems.Add(d);
            }

            mapper(item, d);
        }
    }

    private static void SetItems(ICollection<DbUnitItem> dbItems, IEnumerable<JItem> items) {
        SetItems(dbItems, items, () => new DbUnitItem(), (src, dest) => {
            dest.Code = src.Code;
            dest.Amount = src.Amount;
            dest.Illusion = src.Props?.Contains("illusion") ?? false;
            dest.Unfinished = src.Props?.Contains("unfinished") ?? false;
            dest.Props = src.Props;
        });
    }

    private static void SetItems(ICollection<DbProductionItem> dbItems, IEnumerable<JItem> items) {
        SetItems(dbItems, items, () => new DbProductionItem(), (src, dest) => {
            dest.Code = src.Code;
            dest.Amount = src.Amount;
        });
    }

    private static void SetMarketItems(Market market, ICollection<DbTradableItem> dbItems, IEnumerable<JTradableItem> items) {
        var dest = dbItems
            .Where(x => x.Market == market)
            .ToDictionary(x => x.Code);

        foreach (var item in items) {
            if (!dest.TryGetValue(item.Code, out var d)) {
                d = new DbTradableItem();
                dbItems.Add(d);
            }

            d.Market = market;
            d.Code = item.Code;
            d.Amount = item.Amount;
            d.Price = item.Price;
        }

        // missing items are removed
        foreach (var key in dest.Keys.Except(items.Select(x => x.Code))) {
            dbItems.Remove(dest[key]);
        }
    }

    private static void SetSkills(ICollection<DbSkill> dbItems, IEnumerable<JSkill> items) {
        var dest = dbItems.ToDictionary(x => x.Code);

        foreach (var item in items) {
            if (!dest.TryGetValue(item.Code, out var d)) {
                d = new DbSkill();
                dbItems.Add(d);
            }

            d.Code = item.Code;
            d.Level = item.Level;
            d.Days = item.Days;
        }
    }

    private static void SetFleetContent(ICollection<DbFleetContent> dbItems, IEnumerable<JFleetContent> items) {
        var dest = dbItems.ToDictionary(x => x.Type);

        foreach (var item in items) {
            var type = item.Type.TrimEnd('s');
            if (!dest.TryGetValue(type, out var d)) {
                d = new DbFleetContent {
                    Type = type
                };

                dbItems.Add(d);
            }

            d.Count = item.Count;
        }
    }

    private void SyncExits(JRegion region) {
        var origin = GetRegion(region.Coords);
        foreach (var exit in region.Exits) {
            var target = RegionFromExit(exit);
            AddExit(origin, target, exit.Direction);

            if (!target.Explored) {
                AddExit(target, origin, exit.Direction.Opposite());
            }
        }
    }

    private DbRegion RegionFromExit(JExit exit) {
        var regionId = DbRegion.MakeId(exit.Coords.X, exit.Coords.Y, exit.Coords.Z);
        if (!Regions.TryGetValue(regionId, out var region)) {
            region = new DbRegion {
                Id = regionId,
                X = exit.Coords.X,
                Y = exit.Coords.Y,
                Z = exit.Coords.Z,
                Terrain = exit.Terrain,
                Explored = false,
                Label = exit.Coords.Label,
                Province = exit.Province
            };

            if (exit.Settlement != null) {
                region.Settlement = new DbSettlement {
                    Name = exit.Settlement.Name,
                    Size = exit.Settlement.Size
                };
            }

            Regions.Add(regionId, region);
            NewRegions.Add(regionId);
        }

        return region;
    }

    private DbRegion RegionFromBattle(JBattle battle) {
        var location = battle.Location;
        var regionId = DbRegion.MakeId(location.Coords.X, location.Coords.Y, location.Coords.Z);

        if (!Regions.TryGetValue(regionId, out var region)) {
            region = new DbRegion {
                Id = regionId,
                X = location.Coords.X,
                Y = location.Coords.Y,
                Z = location.Coords.Z,
                Terrain = location.Terrain,
                Explored = false,
                Label = location.Coords.Label,
                Province = location.Province
            };

            Regions.Add(regionId, region);
            NewRegions.Add(regionId);
        }

        return region;
    }

    private static void AddExit(DbRegion origin, DbRegion target, Direction direction) {
        var exit = origin.Exits.Find(x => x.Direction == direction);
        if (exit == null) {
            exit = new DbExit { Direction = direction };
            origin.Exits.Add(exit);
        }

        exit.X = target.X;
        exit.Y = target.Y;
        exit.Z = target.Z;
        exit.Label = target.Label;
        exit.Terrain = target.Terrain;
        exit.Province = target.Province;
        exit.Settlement = target.Settlement != null
            ? new  DbSettlement {
                Name = target.Settlement.Name,
                Size = target.Settlement.Size
            }
            : null;

        exit.OriginRegionId = origin.Id;
        exit.Origin = origin;

        exit.TargetRegionId = target.Id;
        exit.Target = target;
    }
}
