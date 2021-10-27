namespace advisor.Features {
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using advisor.Persistence;
    using MediatR;
    using Microsoft.EntityFrameworkCore;

    public record SetOrders(long PlayerId, int TurnNumber, int UnitNumber, string Orders): IRequest<string>;

    public class SetOrdersHandler : IRequestHandler<SetOrders, string> {
        public SetOrdersHandler(Database db) {
            this.db = db;
        }

        private readonly Database db;

        public async Task<string> Handle(SetOrders request, CancellationToken cancellationToken) {
            var unit = await db.Units
                .FilterByTurn(request.PlayerId, request.TurnNumber)
                .FirstOrDefaultAsync(x => x.Number == request.UnitNumber);

            if (unit == null) return "Unit not found";

            unit.Orders = request.Orders;

            await db.SaveChangesAsync();

            return "Ok";
        }
    }
}
