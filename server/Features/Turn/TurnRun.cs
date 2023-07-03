// FIXME
namespace advisor.Features;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using advisor.Persistence;
using advisor.Remote;
using advisor.Schema;
using advisor.TurnProcessing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public record TurnRun(long GameId, int TurnNumber, int NextTurnNumber, List<DbRegistration> NewFactions): IRequest<TurnRunResult>;
public record TurnRunResult(bool IsSuccess, string Error = null, DbTurn Turn = null) : IMutationResult;

// todo: make smaller. this is too big and it is why it is buggy.
// todo: take this out of Mediator
// public class TurnRunHandler : IRequestHandler<TurnRun, TurnRunResult> {
//     public TurnRunHandler(IUnitOfWork unit, IHttpClientFactory httpFactory, IMediator mediator, ILogger<TurnRunHandler> logger) {
//         this.unit = unit;
//         this.httpFactory = httpFactory;
//         this.mediator = mediator;
//         this.logger = logger;
//     }

//     record GameProjection(long Id, byte[] Engine, int? LastTurnNumber, int? NextTurnNumber, Persistence.GameType Type, GameOptions Options);

//     record FactionProjection(long PlayerId, int Number, string Name, string Password) {
//         public bool IsClaimed => !string.IsNullOrWhiteSpace(Password);
//     }

//     record PendingFaction(long RegistrationId, string Name, string Password);

//     private readonly IUnitOfWork unit;
//     private readonly IHttpClientFactory httpFactory;
//     private readonly IMediator mediator;
//     private readonly ILogger<TurnRunHandler> logger;

//     public async Task<TurnRunResult> Handle(TurnRun request, CancellationToken cancellationToken) {
//         var gamesRepo = unit.Games;

//         var game = await gamesRepo.GetOneAsync(request.GameId);

//         switch (game?.Status) {
//             case null: return new TurnRunResult(false, "Game not found.");
//             case GameStatus.NEW: return new TurnRunResult(false, "Game not yet started.");

//             case GameStatus.RUNNING:
//                 await unit.Games.LockAsync(request.GameId, cancellationToken);
//                 await unit.SaveChanges(cancellationToken);
//                 break;

//             case GameStatus.LOCKED:
//                 break;

//             case GameStatus.PAUSED: return new TurnRunResult(false, "Game is paused.");

//             case GameStatus.STOPED: return new TurnRunResult(false, "Game already compleated.");

//             default: return new TurnRunResult(false, "Game is in the unknonw state.");
//         }

//         var playersRepo = unit.Players(game);
//         var turnsRepo = unit.Turns(game);

//         await unit.BeginTransaction(cancellationToken);

//         var factions = await playersRepo.ActivePlayers
//             .AsNoTracking()
//             .Select(x => new FactionProjection(x.Id, x.Number, x.Name, x.Password))
//             .ToListAsync(cancellationToken);

//         int turnNumber;
//         DbTurn turn;
//         DbTurn nextTurn;

//         switch (game.Type) {
//             case Persistence.GameType.LOCAL:
//                 turnNumber = game.NextTurnNumber.Value;
//                 turn = await turnsRepo.GetOneAsync(turnNumber, cancellationToken);

//                 // run local turn

//                 var engine = await unit.Engines.GetOneNoTrackingAsync(game.EngineId.Value, cancellationToken);

//                 var allOrders = new Dictionary<FactionProjection, IEnumerable<UnitOrdersRec>>();
//                 foreach (var player in factions) {
//                     var playerRepo = await unit.PlayerAsync(player.PlayerId);
//                     var orders = await playerRepo.Orders(turnNumber)
//                         .Select(x => new UnitOrdersRec(x.UnitNumber, x.Orders))
//                         .ToListAsync(cancellationToken);

//                     allOrders.Add(player, orders);
//                 }

//                 var newFactions = await gamesRepo.GetRegistrations(game.Id)
//                     .AsNoTracking()
//                     .Select(x => new PendingFaction(x.Id, x.Name, x.Password))
//                     .ToListAsync(cancellationToken);

//                 var playersIn = AppendNewFactions(newFactions, turn.PlayerData);

//                 try {
//                     nextTurn = await RunLocalTurnAsync(turnsRepo, playersRepo, game, turn, allOrders, newFactions, engine.Contents, turn.GameData, playersIn, cancellationToken);
//                 }
//                 catch (GameTurnRunException ex) {
//                     await unit.RollbackTransaction(cancellationToken);
//                     return new TurnRunResult(false, ex.Message);
//                 }

//                 break;

//             case Persistence.GameType.REMOTE:
//                 var remote = new NewOriginsClient(game.Options.ServerAddress, httpFactory);
//                 var remoteTurnNumber = await remote.GetCurrentTurnNumberAsync(cancellationToken);
//                 if (game.NextTurnNumber > remoteTurnNumber) {
//                     return new TurnRunResult(false, "No new turn found on remote server.");
//                 }

//                 turnNumber = remoteTurnNumber;
//                 if (game.NextTurnNumber < turnNumber) {
//                     turn = await turnsRepo.AddTurnAsync(remoteTurnNumber, cancellationToken);
//                 }
//                 else {
//                     turn = await (game.NextTurnNumber == null
//                         ? turnsRepo.AddTurnAsync(turnNumber, cancellationToken)
//                         : turnsRepo.GetOneAsync(turnNumber, cancellationToken));
//                 }

//                 // import remote turn
//                 if (turn.State != TurnState.PENDING) {
//                     return new TurnRunResult(false, "Turn was already imported.");
//                 }

//                 nextTurn = await ImportRemoteTurnAsync(turnsRepo, remote, game, turn, factions, cancellationToken);

//                 break;

//             default:
//                 await unit.RollbackTransaction(cancellationToken);
//                 return new TurnRunResult(false, "Game is of unknown type, just LOCAL or REMOTE games are supported.");
//         }

//         turn.State = TurnState.READY;

//         game.LastTurnNumber = turn.Number;
//         game.NextTurnNumber = nextTurn.Number;

//         await unit.SaveChanges(cancellationToken);

//         await foreach (var player in playersRepo.ActivePlayers.AsAsyncEnumerable().WithCancellation(cancellationToken)) {
//             if (player.LastTurnNumber == game.LastTurnNumber) {
//                 continue;
//             }

//             var playerRepo = unit.Player(player);
//             if (player.LastTurnNumber == null) {
//                 await playerRepo.AddTurnAsync(game.LastTurnNumber.Value, player.Name, player.Number);
//             }

//             await playerRepo.AddTurnAsync(game.NextTurnNumber.Value, player.Name, player.Number);

//             player.LastTurnNumber = game.LastTurnNumber;
//             player.NextTurnNumber = game.NextTurnNumber;
//         }

//         await unit.SaveChanges(cancellationToken);

//         if (game.Type == Persistence.GameType.REMOTE) {
//             var result = await mediator.Send(new GameSyncFactions(request.GameId), cancellationToken);
//             if (!result) {
//                 await unit.RollbackTransaction(cancellationToken);
//                 return new TurnRunResult(result, result.Error);
//             }
//         }

//         game.Status = GameStatus.RUNNING;

//         await unit.SaveChanges(cancellationToken);
//         await unit.CommitTransaction(cancellationToken);

//         return new TurnRunResult(true, Turn: turn);
//     }

//     private async Task<DbTurn> RunLocalTurnAsync(
//         ITurnRepository turnsRepo,
//         IPlayerRepository playersRepo,
//         DbGame game,
//         DbTurn turn,
//         Dictionary<FactionProjection, IEnumerable<UnitOrdersRec>> allOrders,
//         List<PendingFaction> newFactions,
//         byte[] gameEngine,
//         byte[] gameData,
//         string playersIn,
//         CancellationToken cancellation
//     ) {
//         List<FactionRecord> factionsOut;
//         DbTurn nextTurn;

//         // 1. Start the local turn runner in random temp directory
//         var runner = new TurnRunner(TurnRunnerOptions.UseTempDirectory());
//         try {
//             // 2. Put there engine and game state files
//             await runner.WriteEngineAsync(gameEngine);
//             await runner.WriteGameAsync(gameData);
//             await runner.WritePlayersAsync(playersIn);

//             // 3. Write faction orders
//             foreach (var (faction, orders) in allOrders) {
//                 await runner.WriteFactionOrdersAsync(faction.Number, faction.Password, orders);
//             }

//             // 4. Run the simulation
//             var run = await runner.RunAsync(TimeSpan.FromMinutes(10), cancellation);
//             if (!run.Success) {
//                 throw new GameTurnRunException(run.StdErr);
//             }

//             /////

//             // 5. Read new game state
//             var gameOut = runner.GetGameOut();
//             var playersOut = runner.GetPlayersOut();

//             // 6. Store game state into the upcoming turn
//             nextTurn = await turnsRepo.AddTurnAsync(turn.Number + 1, cancellation);
//             nextTurn.GameData = await gameOut.ReadAllBytesAsync(cancellation);
//             nextTurn.PlayerData = await playersOut.ReadAllBytesAsync(cancellation);

//             // 7. Read players
//             using var playersOutStream = playersOut.OpenRead();
//             factionsOut = ReadFactions(playersOutStream).ToList();
//         }
//         finally {
//             runner.Clean();
//         }

//         Dictionary<int, long> factionToPlayer = allOrders.Keys.ToDictionary(x => x.Number, x => x.PlayerId);
//         foreach (var (number, faction) in MatchWithNewFactions(factionsOut, newFactions)) {
//             var player = await playersRepo.AddLocalAsync(faction.RegistrationId, number, cancellation);
//             factionToPlayer.Add(player.Number, player.Id);
//         }

//         foreach (var faction in MatchWithQuitFactions(factionsOut, allOrders.Keys)) {
//             await playersRepo.QuitAsync(faction.PlayerId, cancellation);
//         }

//         foreach (var article in runner.ListArticles()) {
//             using var reader = article.OpenText();

//             var text = await reader.ReadToEndAsync();
//             turn.Articles.Add(new DbArticle { Text = text, Type = "Global" });
//         }

//         var tempaltes = runner.ListTemplates().ToDictionary(x => x.Number);
//         foreach (var reportFile in runner.ListReports()) {
//             var factionNumber = reportFile.Number;
//             using var ms = new MemoryStream();

//             using var reportStream = reportFile.Contents.OpenRead();
//             await reportStream.CopyToAsync(ms);

//             if (tempaltes.ContainsKey(factionNumber)) {
//                 using var templateStream = tempaltes[factionNumber].Contents.OpenRead();
//                 await templateStream.CopyToAsync(ms);
//             }

//             var playerId = factionToPlayer[factionNumber];
//             var report = await turnsRepo.AddReportAsync(playerId, factionNumber, turn.Number, ms.ToArray(), cancellation);
//         }

//         return nextTurn;
//     }

//     private async Task<DbTurn> ImportRemoteTurnAsync(
//         ITurnRepository turnsRepo,
//         NewOrigins client,
//         DbGame game,
//         DbTurn turn,
//         List<FactionProjection> factions,
//         CancellationToken cancellation
//     ) {
//         foreach (var faction in factions.Where(x => x.IsClaimed)) {
//             string reportText = await client.DownloadReportAsync(faction.Number, faction.Password, cancellation);
//             var report = await turnsRepo.AddReportAsync(faction.PlayerId, faction.Number, turn.Number, Encoding.UTF8.GetBytes(reportText), cancellation);
//         }

//         var nextTurn = await turnsRepo.AddTurnAsync(turn.Number + 1, cancellation);
//         return nextTurn;
//     }

//     private async Task<bool> TryParseReportAsync(TextReader reader, DbReport report, CancellationToken cancellation) {
//         try {
//             using var atlantisReader = new AtlantisReportJsonConverter(reader);
//             var json = await atlantisReader.ReadAsJsonAsync();

//             var jsonString = json.ToString(Newtonsoft.Json.Formatting.None);
//             report.Json = Encoding.UTF8.GetBytes(jsonString);

//             return true;
//         }
//         catch (Exception ex) {
//             report.Error = ex.ToString();

//             return false;
//         }
//     }

//     private string AppendNewFactions(IEnumerable<PendingFaction> newPlayers, byte[] playerData) {
//         using var ms = new MemoryStream(playerData);
//         using var reader = new StreamReader(ms);

//         using var writer = new StringWriter();
//         writer.Write(reader.ReadToEnd());

//         var playersWriter = new PlayersFileWriter(writer);

//         foreach (var player in newPlayers) {
//             var faction = new FactionRecord {
//                 Name = player.Name,
//                 Password = player.Password,
//                 SendTimes = true,
//                 Template = "long"
//             };

//             playersWriter.Write(faction);
//         }

//         writer.Flush();

//         return writer.ToString();
//     }

//     private IEnumerable<(int number, PendingFaction faction)> MatchWithNewFactions(List<FactionRecord> factions, List<PendingFaction> newFactions) {
//         int i = 0;
//         foreach (var faction in factions.TakeLast(newFactions.Count)) {
//             yield return (faction.Number.Value, newFactions[i++]);
//         }
//     }

//     private IEnumerable<FactionProjection> MatchWithQuitFactions(List<FactionRecord> nextFactions, IEnumerable<FactionProjection> oldFactions) {
//         var knownFactions = nextFactions
//             .Select(x => x.Number.Value)
//             .ToHashSet();

//         var quitFactions = oldFactions
//             .Where(x => !knownFactions.Contains(x.Number));

//         return quitFactions;
//     }

//     private IEnumerable<FactionRecord> ReadFactions(Stream playersFileStream) => new PlayersFileReader(playersFileStream);
// }

[System.Serializable]
public class GameTurnRunException : System.Exception
{
    public GameTurnRunException() { }
    public GameTurnRunException(string message) : base(message) { }
    public GameTurnRunException(string message, System.Exception inner) : base(message, inner) { }
    protected GameTurnRunException(
        System.Runtime.Serialization.SerializationInfo info,
        System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
}
