namespace advisor.Features;

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using advisor.Persistence;
using advisor.Schema;
using MediatR;

public record GameCreateLocal(string Name, long EngineId, GameOptions Options) : IRequest<GameCreateLocalResult>;

public record GameCreateLocalResult(DbGame Game, bool IsSuccess, string Error) : MutationResult(IsSuccess, Error);

public class GameCreateLocalHandler : IRequestHandler<GameCreateLocal, GameCreateLocalResult> {
    public GameCreateLocalHandler(IGameRepository games, IUnitOfWork unit, IMediator mediator) {
        this.games = games;
        this.unit = unit;
        this.mediator = mediator;
    }

    private readonly IUnitOfWork unit;
    private readonly IMediator mediator;
    private readonly IGameRepository games;

    public async Task<GameCreateLocalResult> Handle(GameCreateLocal request, CancellationToken cancellationToken) {
        DbGame game = await games.CreateLocalAsync(
            name: request.Name,
            engineId: request.EngineId,
            ruleset: "",
            options: request.Options,
            cancellationToken
        );

        await unit.SaveChangesAsync(cancellationToken);

        await mediator.Send(new Reconcile(game.Id), cancellationToken);

        return new GameCreateLocalResult(game, true, null);
    }
}
