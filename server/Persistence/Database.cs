namespace advisor.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public enum DatabaseProvider {
    SQLite,
    PgSQL,
    MsSQL
}

public class DatabaseOptions {
    public string ConnectionString { get; set; }
    public DatabaseProvider Provider { get; set; }
    public bool IsProduction { get; set; }
}

public class PgSqlDatabase : Database {
    public PgSqlDatabase(IOptionsSnapshot<DatabaseOptions> options, ILoggerFactory loggerFactor)
        : base(options, loggerFactor) {
            this.options.Provider = DatabaseProvider.PgSQL;
    }
}

public class MsSqlDatabase : Database {
    public MsSqlDatabase(IOptionsSnapshot<DatabaseOptions> options, ILoggerFactory loggerFactor)
        : base(options, loggerFactor) {
            this.options.Provider = DatabaseProvider.MsSQL;
    }
}

public class SQLiteDatabase : Database {
    public SQLiteDatabase(IOptionsSnapshot<DatabaseOptions> options, ILoggerFactory loggerFactor)
        : base(options, loggerFactor) {
            this.options.Provider = DatabaseProvider.SQLite;
    }
}

public abstract class Database : DbContext {
    protected Database(IOptionsSnapshot<DatabaseOptions> options, ILoggerFactory loggerFactory) {
        this.options = options.Value;
        this.loggerFactory = loggerFactory;

        this.Provider = this.options.Provider;
    }

    public DatabaseProvider Provider { get; }

    protected DatabaseOptions options;
    private readonly ILoggerFactory loggerFactory;

    // Registered users
    public DbSet<DbUser> Users { get; set; }

    // Game engines
    public DbSet<DbGameEngine> GameEngines { get; set; }

    // Local and remote games
    public DbSet<DbGame> Games { get; set; }

    //
    public DbSet<DbTurn> Turns { get; set; }
    public DbSet<DbReport> Reports { get; set; }
    public DbSet<DbArticle> Articles{ get; set; }

    public DbSet<DbPlayer> Players { get; set; }

    public DbSet<DbPlayerTurn> PlayerTurns { get; set; }
    public DbSet<DbAditionalReport> AditionalReports { get; set; }
    public DbSet<DbFaction> Factions { get; set; }
    public DbSet<DbAttitude> Attitudes { get; set; }
    public DbSet<DbStat> Stats { get; set; }
    public DbSet<DbStatItem> StatItems { get; set; }
    public DbSet<DbEvent> Events { get; set; }
    public DbSet<DbRegion> Regions { get; set; }
    public DbSet<DbProductionItem> Production { get; set; }
    public DbSet<DbMarketItem> Markets { get; set; }
    public DbSet<DbExit> Exits { get; set; }
    public DbSet<DbStructure> Structures { get; set; }
    public DbSet<DbUnit> Units { get; set; }
    public DbSet<DbUnitItem> Items { get; set; }

    public DbSet<DbAlliance> Alliances { get; set; }
    public DbSet<DbAllianceMember> AllianceMembers { get; set; }
    public DbSet<DbStudyPlan> StudyPlans { get; set; }
    public DbSet<DbBattle> Battles { get; set; }


    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
        switch (options.Provider) {
            case DatabaseProvider.SQLite:
                optionsBuilder.UseSqlite(options.ConnectionString);
                break;

            case DatabaseProvider.PgSQL:
                optionsBuilder.UseNpgsql(options.ConnectionString);
                break;

            case DatabaseProvider.MsSQL:
                optionsBuilder.UseSqlServer(options.ConnectionString);
                break;
        }

        optionsBuilder.UseLoggerFactory(loggerFactory);

        if (!options.IsProduction) {
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.EnableSensitiveDataLogging();
        }

        if (options.IsProduction) {
            optionsBuilder.ConfigureWarnings(c => c.Log((RelationalEventId.CommandExecuting, LogLevel.Debug)));
        }
    }

    protected override void OnModelCreating(ModelBuilder model) {
        model.Entity<DbUser>(t => {
            t.Property(x => x.Algorithm).HasConversion<string>();

            t.Property(p => p.Roles)
                .HasJsonConversion(options.Provider);

            t.HasMany(p => p.Players)
                .WithOne(p => p.User)
                .HasForeignKey(x => x.UserId);

            t.HasIndex(x => new { x.Email })
                .IsUnique();
        });

        model.Entity<DbGameEngine>(t => {
            t.HasMany(x => x.Games)
                .WithOne(x => x.Engine)
                .HasForeignKey(x => x.EngineId)
                .IsRequired(false);

            t.HasIndex(x => x.Name)
                .IsUnique();
        });

        model.Entity<DbGame>(t => {
            t.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            t.Property(x => x.Type)
                .HasConversion<string>();

            t.Property(x => x.Options)
                .HasJsonConversion(options.Provider);

            t.HasMany(p => p.Players)
                .WithOne(p => p.Game)
                .HasForeignKey(x => x.GameId);

            t.HasMany(x => x.Alliances)
                .WithOne(x => x.Game)
                .HasForeignKey(x => x.GameId);

            t.HasMany(x => x.Turns)
                .WithOne(x => x.Game)
                .HasForeignKey(x => x.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            t.HasMany(x => x.Articles)
                .WithOne(x => x.Game)
                .HasForeignKey(x => x.GameId)
                .OnDelete(DeleteBehavior.Cascade);

            t.HasOne(x => x.LastTurn)
                .WithOne()
                .HasForeignKey<DbGame>(x => new { x.Id, x.LastTurnNumber })
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);

            t.HasOne(x => x.NextTurn)
                .WithOne()
                .HasForeignKey<DbGame>(x => new { x.Id, x.NextTurnNumber })
                .IsRequired(false)
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<DbTurn>(t => {
            t.HasKey(x => new { x.GameId, x.Number });

            t.HasMany(x => x.Articles)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.GameId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Cascade);

            t.HasMany(x => x.Reports)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.GameId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Cascade);
        });

        model.Entity<DbReport>(t => {
            t.HasKey(x => new {  x.GameId, x.TurnNumber, x.FactionNumber });
        });

        model.Entity<DbArticle>(t => {
            t.HasKey(x => x.Id);
        });

        model.Entity<DbPlayer>(t => {
            t.HasMany<DbAditionalReport>(x => x.Reports)
                .WithOne(x => x.Player)
                .HasForeignKey(x => x.PlayerId);

            t.HasMany<DbPlayerTurn>(x => x.Turns)
                .WithOne(x => x.Player)
                .HasForeignKey(x => x.PlayerId);

            t.HasMany(x => x.AllianceMembererships)
                .WithOne(x => x.Player)
                .HasForeignKey(x => x.PlayerId);
        });

        model.Entity<DbPlayerTurn>(t => {
            t.HasKey(x => new { x.PlayerId, x.Number });

            t.HasMany(x => x.Reports)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Regions)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Exits)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Markets)
                .WithOne()
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Production)
                .WithOne()
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Factions)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Attitudes)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Events)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Structures)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Units)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Plans)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Stats)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);

            t.HasMany(x => x.Battles)
                .WithOne(x => x.Turn)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber })
                .OnDelete(DeleteBehavior.Restrict);
        });

        model.Entity<DbAditionalReport>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.FactionNumber });
        });

        model.Entity<DbRegion>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.Id });

            t.Ignore(x => x.ForSale);
            t.Ignore(x => x.Wanted);

            t.HasMany(x => x.Units)
                .WithOne(x => x.Region)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.RegionId });

            t.HasMany(x => x.Structures)
                .WithOne(x => x.Region)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.RegionId });

            t.HasMany(x => x.Stats)
                .WithOne(x => x.Region)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.RegionId });

            t.HasMany(x => x.Events)
                .WithOne(x => x.Region)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.RegionId });

            t.OwnsOne(p => p.Settlement, a => {
                a.Property(x => x.Size).HasConversion<string>();
            });

            t.HasMany(p => p.Produces)
                .WithOne(p => p.Region)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.RegionId });

            t.HasMany(p => p.Markets)
                .WithOne(p => p.Region)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.RegionId });
        });

        model.Entity<DbMarketItem>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.RegionId, x.Market, x.Code });

            t.Property(x => x.Amount).IsRequired();
            t.Property(x => x.Price).IsRequired();
            t.Property(x => x.Market)
                .HasConversion<string>();
        });

        model.Entity<DbProductionItem>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.RegionId, x.Code });

            t.Property(x => x.Amount).IsRequired();
        });

        model.Entity<DbExit>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.OriginRegionId, x.TargetRegionId });

            t.Property(p => p.Direction).HasConversion<string>();

            t.OwnsOne(p => p.Settlement, a => {
                a.Property(x => x.Size).HasConversion<string>();
            });

            t.HasOne(p => p.Target)
                .WithMany()
                .HasForeignKey(p => new { p.PlayerId, p.TurnNumber, p.TargetRegionId });

            t.HasOne(p => p.Origin)
                .WithMany(p => p.Exits)
                .HasForeignKey(p => new { p.PlayerId, p.TurnNumber, p.OriginRegionId });
        });

        model.Entity<DbFaction>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.Number });

            t.Property(x => x.DefaultAttitude).HasConversion<string>();

            t.HasMany(x => x.Events)
                .WithOne(x => x.Faction)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.FactionNumber });

            t.HasMany(x => x.Units)
                .WithOne(x => x.Faction)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.FactionNumber });

            t.HasMany(x => x.Attitudes)
                .WithOne(x => x.Faction)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.FactionNumber });
        });

        model.Entity<DbAttitude>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.FactionNumber, x.TargetFactionNumber });

            t.Property(x => x.Stance).HasConversion<string>();
        });

        model.Entity<DbEvent>(t => {
            t.Property(x => x.Type).HasConversion<string>();
            t.Property(x => x.Category).HasConversion<string>();
        });

        model.Entity<DbStat>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.RegionId });

            t.OwnsOne(x => x.Income);

            t.HasMany(p => p.Production)
                .WithOne(x => x.Stat)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.RegionId });
        });

        model.Entity<DbStatItem>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.RegionId, x.Code });
            t.Property(x => x.Amount).IsRequired();

            t.HasOne(x => x.Region)
                .WithMany()
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.RegionId });
        });

        model.Entity<DbStructure>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.Id });

            t.Property(p => p.Flags)
                .HasJsonConversion(options.Provider);

            t.Property(p => p.SailDirections)
                .HasJsonConversion(options.Provider);

            t.Property(p => p.Contents)
                .HasJsonConversion(options.Provider);

            t.HasMany(x => x.Units)
                .WithOne(x => x.Structure)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.StrcutureId });

            t.OwnsOne(p => p.Load);
            t.OwnsOne(p => p.Sailors);
        });

        model.Entity<DbUnit>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.Number });

            t.Property(p => p.Flags)
                .HasJsonConversion(options.Provider);

            t.Property(p => p.CanStudy)
                .HasJsonConversion(options.Provider);

            t.Property(p => p.Skills)
                .HasJsonConversion(options.Provider);

            t.OwnsOne(p => p.Capacity);

            t.HasMany(x => x.Events)
                .WithOne(x => x.Unit)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.UnitNumber });

            t.HasMany(p => p.Items)
                .WithOne(p => p.Unit)
                .HasForeignKey(x => new { x.PlayerId, x.TurnNumber, x.UnitNumber });
        });

        model.Entity<DbUnitItem>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.UnitNumber, x.Code });
        });

        model.Entity<DbStudyPlan>(t => {
            t.HasKey(x => new { x.PlayerId, x.TurnNumber, x.UnitNumber });

            t.Property(x => x.Teach)
                .HasJsonConversion(options.Provider);

            t.OwnsOne(p => p.Target, owned => {
                owned.Ignore(x => x.Days);
            });

            t.HasOne(p => p.Unit)
                .WithOne(p => p.StudyPlan)
                .HasForeignKey<DbStudyPlan>(x => new { x.PlayerId, x.TurnNumber, x.UnitNumber });
        });

        model.Entity<DbAlliance>(t => {
            t.HasMany(p => p.Members)
                .WithOne(x => x.Alliance)
                .HasForeignKey(x => x.AllianceId);
        });

        model.Entity<DbAllianceMember>(t => {
            t.HasKey(x => new { x.PlayerId, x.AllianceId });
        });

        model.Entity<DbBattle>(t => {
            t.HasKey(x => x.Id);

            t.Property(x => x.Battle)
                .HasJsonConversion(options.Provider);

            t.OwnsOne(x => x.Attacker);
            t.OwnsOne(x => x.Defender);
        });
    }
}
