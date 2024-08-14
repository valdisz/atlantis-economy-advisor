namespace advisor;

using System.Linq;
using System.Threading.Tasks;
using advisor.Persistence;
using Hangfire.RecurringJobExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public class MaintenanceJobs {
    public MaintenanceJobs(Database db, IMediator mediator, ILogger<MaintenanceJobs> logger) {
        this.db = db;
        this.mediator = mediator;
        this.logger = logger;
    }

    private readonly Database db;
    private readonly IMediator mediator;
    private readonly ILogger logger;

    // every hour
    [RecurringJob("0 * * * *", TimeZone = "America/Los_Angeles")]
    public async Task ReconcileRports() {
        logger.LogInformation($"Looking for unparsed reports");

        var unparsed = (await db.AditionalReports
            .Include(x => x.Turn)
            .Where(x => x.Json == null)
            .ToListAsync())
            .GroupBy(x => new { x.PlayerId, x.TurnNumber })
            .Select(kv => new { kv.Key.PlayerId, kv.Key.TurnNumber, Reports = kv.ToList() })
            .OrderBy(x => x.PlayerId)
                .ThenBy(x => x.TurnNumber)
            .ToList();

        logger.LogInformation($"Found {unparsed.Count} Turns with unparsed reports");

        foreach (var g in unparsed) {
            var playerId = g.PlayerId;
            var turnNumber = g.TurnNumber;
            var reports = g.Reports;


        }
    }
}
