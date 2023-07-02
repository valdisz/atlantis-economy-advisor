namespace advisor.Schema
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using HotChocolate;
    using HotChocolate.Resolvers;
    using HotChocolate.Types;
    using Microsoft.EntityFrameworkCore;
    using Persistence;

    public class AllianceType : ObjectType<DbAlliance> {
        protected override void Configure(IObjectTypeDescriptor<DbAlliance> descriptor) {
            descriptor
                .ImplementsNode()
                .IdField(x => x.Id)
                .ResolveNode((ctx, id) => {
                    var db = ctx.Service<Database>();
                    return db.Alliances
                        .AsNoTracking()
                        .Include(x => x.Members)
                        .ThenInclude(x => x.Player)
                        .FirstOrDefaultAsync(x => x.Id == id);
                });
        }
    }

    public class AllianceMemberType : ObjectType<DbAllianceMember> {
        protected override void Configure(IObjectTypeDescriptor<DbAllianceMember> descriptor) {
        }
    }

    [ExtendObjectType(typeof(DbAllianceMember))]
    public class AllianceMemberResolvers {
        public string Name([Parent] DbAllianceMember member) => member.Player.Name;

        public int? Number([Parent] DbAllianceMember member) => member.Player.Number;

        public async Task<List<AllianceMemberTurn>> Turns(Database db, [Parent] DbAllianceMember member) {
            return (await db.PlayerTurns
                .AsNoTracking()
                .OnlyPlayer(member)
                .Include(x => x.Player)
                .OrderBy(x => x.TurnNumber)
                .Select(x => new { TurnNumber = x.TurnNumber, FactionNumber = x.Player.Number, FactionName = x.Player.Name })
                .ToListAsync())
                .Select(x => new AllianceMemberTurn(member.PlayerId, x.FactionNumber, x.FactionName, x.TurnNumber))
                .ToList();
        }

        public async Task<AllianceMemberTurn> Turn(Database db, [Parent] DbAllianceMember member, int number) {
            var data = await db.PlayerTurns
                .AsNoTracking()
                .OnlyPlayer(member)
                .Include(x => x.Player)
                .Where(x => x.TurnNumber == number)
                .OrderBy(x => x.TurnNumber)
                .Select(x => new { TurnNumber = x.TurnNumber, FactionNumber = x.Player.Number, FactionName = x.Player.Name })
                .FirstOrDefaultAsync();

            return new AllianceMemberTurn(member.PlayerId, data.FactionNumber, data.FactionName, data.TurnNumber);
        }
    }

    public class AllianceMemberTurn {
        public AllianceMemberTurn(long playerId, int factionNumber, string factionName, int turnNumber) {
            this.playerId = playerId;
            this.factionNumber = factionNumber;
            this.factionName = factionName;
            this.Number = turnNumber;
        }

        private readonly long playerId;
        private readonly int factionNumber;
        private readonly string factionName;

        public int Number { get; }

        [UseOffsetPaging(IncludeTotalCount = true, MaxPageSize = 1000)]
        public async Task<IQueryable<DbUnit>> Units(IResolverContext context, Database db, UnitsFilter filter = null) {
            var fields = context.CollectSelectedFields<DbUnit>();

            var query = db.Units
                .AsNoTrackingWithIdentityResolution()
                .InTurn(playerId, Number);

            if (fields.Contains(nameof(DbUnit.Items))) {
                query = query.Include(x => x.Items);
            }

            if (fields.Contains(nameof(DbUnit.StudyPlan))) {
                query = query.Include(x => x.StudyPlan);
            }

            if (filter != null) {
                if (filter.Own != null) {
                    query = filter.Own.Value
                        ? query.Where(x => x.FactionNumber == factionNumber)
                        : query.Where(x => x.FactionNumber != factionNumber);
                }

                if (filter.Mages != null) {
                    query = (await query.ToListAsync())
                        .Where(x => x.Skills.Any(s => s.Code == "FORC" || s.Code == "PATT" || s.Code == "SPIR"))
                        .AsQueryable();
                }
            }

            return query.OrderBy(x => x.Number);
        }
    }
}
