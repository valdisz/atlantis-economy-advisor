namespace advisor;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using advisor.Features;
using advisor.Persistence;
using AngleSharp;
using Hangfire;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// FIXME
public class RemoteGameServerJobs {
    public RemoteGameServerJobs(Database db, IMediator mediator, IHttpClientFactory httpClientFactory,
        IBackgroundJobClient backgroundJobs,
        ILogger<RemoteGameServerJobs> logger) {
        this.db = db;
        this.mediator = mediator;
        this.httpClientFactory = httpClientFactory;
        this.backgroundJobs = backgroundJobs;
        this.logger = logger;
    }

    private readonly Database db;
    private readonly IMediator mediator;
    private readonly IHttpClientFactory httpClientFactory;
    private readonly IBackgroundJobClient backgroundJobs;
    private readonly ILogger logger;

    // // [RecurringJob("0 12 * * 2,5", TimeZone = "America/Los_Angeles")]
    // public async Task NewOrigins(long gameId) {
    //     var players = await db.Players
    //         .AsNoTracking()
    //         .Where(x => x.GameId == gameId && x.Number != null && x.Password != null && !x.IsQuit)
    //         .ToListAsync();

    //     if (players.Count == 0) {
    //         logger.LogInformation($"No factions with known password, report downloading completed.");
    //     }

    //     var missingTurns = new List<DbPlayer>();

    //     var started = DateTime.UtcNow;
    //     int remoteTurnNumber;
    //     do {
    //         remoteTurnNumber = await GetRemoteTurnNumberAsync();
    //         logger.LogInformation($"Remote turn number is {remoteTurnNumber}");

    //         foreach (var player in players) {
    //             if (player.LastTurnNumber < remoteTurnNumber) {
    //                 missingTurns.Add(player);
    //             }
    //         }

    //         if (missingTurns.Count == 0) {
    //             logger.LogInformation($"Sleep 1 minute");
    //             await Task.Delay(TimeSpan.FromMinutes(1));
    //         }
    //     }
    //     while (missingTurns.Count == 0 && (TimeSpan.FromMinutes(30) > (DateTime.UtcNow - started)));

    //     if (missingTurns.Count > 0) {
    //         await DownloadReportsAsync(missingTurns);
    //         await ProcessTurns(missingTurns, remoteTurnNumber);

    //         logger.LogInformation($"All player reports were processed");
    //     }
    //     else {
    //         logger.LogError($"New turn was not generated or was not possible to reach server");
    //     }
    // }

    // private async Task<int> GetRemoteTurnNumberAsync() {
    //     logger.LogInformation("Scraping remote game state");

    //     var config = Configuration.Default.WithDefaultLoader();

    //     var browsingContext = BrowsingContext.New(config);
    //     var document = await browsingContext.OpenAsync("http://atlantis-pbem.com/");

    //     var allHeadings = document.QuerySelectorAll("h3");
    //     foreach (var h in allHeadings) {
    //         if (h.TextContent.StartsWith("Turn Number:")) {
    //             var turnNumber = int.Parse(h.QuerySelector("span").TextContent);
    //             return turnNumber;
    //         }
    //     }

    //     return -1;
    // }

    // public async Task DownloadReportsAsync(List<DbPlayer> players) {
    //     foreach (var player in players) {
    //         logger.LogInformation($"{player.Name} ({player.Number}): Downloading new turn from the server");
    //         var report = await DownloadReportForFactionAsync(player.Number.Value, player.Password);

    //         logger.LogInformation($"{player.Name} ({player.Number}): Saving report to database");
    //         var turn = await mediator.Send(new PlayerReportUpload(player.Id, new[] { report }));
    //     }
    // }

    // private async Task<string> DownloadReportForFactionAsync(int factionNumber, string password) {
    //     using var http = httpClientFactory.CreateClient();

    //     var fields = new Dictionary<string, string>();
    //     fields.Add("factionId", factionNumber.ToString());
    //     fields.Add("password", password);

    //     var request = new HttpRequestMessage(HttpMethod.Post, "http://atlantis-pbem.com/game/download-report") {
    //         Content = new FormUrlEncodedContent(fields)
    //     };

    //     var response = await http.SendAsync(request);
    //     response.EnsureSuccessStatusCode();

    //     var report = await response.Content.ReadAsStringAsync();
    //     return report;
    // }

    // public async Task ProcessTurns(List<DbPlayer> players, int turnNumber) {
    //     foreach (var player in players) {
    //         try {
    //             logger.LogInformation($"{player.Name} ({player.Number}): Processing turn");
    //             await mediator.Send(new TurnProcess(player.Id, turnNumber));
    //         }
    //         catch (Exception ex) {
    //             logger.LogError(ex, $"{player.Name} ({player.Number}): {ex.Message}");
    //             throw;
    //         }
    //     }
    // }
}
