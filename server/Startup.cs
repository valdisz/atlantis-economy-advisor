namespace advisor;

using System;
using System.Linq;
using System.Net;
using advisor.Features;
using advisor.Model;
using advisor.Persistence;
using advisor.Schema;
using advisor.Hanfgire;
using Hangfire;
using Hangfire.Console;
using Hangfire.Console.Extensions;
using Hangfire.Heartbeat;
using Hangfire.Heartbeat.Server;
using Hangfire.PostgreSql;
using Hangfire.RecurringJobAdmin;
using Hangfire.Server;
using Hangfire.Storage.SQLite;
using HotChocolate;
using HotChocolate.Execution;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;
using HotChocolate.Types.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using advisor.TurnProcessing;
using System.IO;
using advisor.Authorization;
using System.Net.Http;

public class Startup {
    public Startup(IWebHostEnvironment env, IConfiguration configuration) {
        Env = env;
        Configuration = configuration;
    }

    public IWebHostEnvironment Env { get; }
    public IConfiguration Configuration { get; }

    public DatabaseProvider DbProvider => Configuration.GetValue<DatabaseProvider>("Provider");
    public string DbConnectionString => Configuration.GetConnectionString("database");
    public string HangfireConnectionString => Configuration.GetConnectionString("hangfire");

    public IConfigurationSection DiscordConfiguration => Configuration.GetSection("Discord");
    public IConfigurationSection DiscordOAuthConfiguration => DiscordConfiguration.GetSection("OAuth");
    public IConfigurationSection ProxyConfiguration => Configuration.GetSection("Proxy");

    public void ConfigureServices(IServiceCollection services) {
        services.AddFunctionalRuntime();

        services.AddOptions();

        services.Configure<DiscordOptions>(DiscordConfiguration);
        services.Configure<ForwardedHeadersOptions>(opt => {
            opt.ForwardedHeaders = ProxyConfiguration.GetValue<ForwardedHeaders>("ForwardedHeaders");

            opt.AllowedHosts.Clear();
            var allowedHosts = ProxyConfiguration.GetSection("AllowedHosts").Get<string[]>();
            foreach (var h in (allowedHosts ?? Enumerable.Empty<string>())) {
                opt.AllowedHosts.Add(h);
            }

            IPAddress resolveProxy(string ipOrDomain) {
                if (IPAddress.TryParse(ipOrDomain, out var ip)) {
                    return ip;
                }

                var rec = Dns.GetHostEntry(ipOrDomain);
                return rec.AddressList[0];
            }

            opt.KnownProxies.Clear();
            var knownProxies = ProxyConfiguration.GetSection("KnownProxies").Get<string[]>();
            foreach (var ip in (knownProxies ?? Enumerable.Empty<string>()).Select(resolveProxy)) {
                opt.KnownProxies.Add(ip);
            }

            IPNetwork parseNetwork(string s) {
                var parts = s.Split("/");

                var ip = IPAddress.Parse(parts[0]);
                var len = int.Parse(parts[1]);

                return new IPNetwork(ip, len);
            }

            opt.KnownNetworks.Clear();
            var knownNetworks = ProxyConfiguration.GetSection("KnownNetworks").Get<string[]>();
            foreach (var network in (knownNetworks ?? Enumerable.Empty<string>()).Select(parseNetwork)) {
                opt.KnownNetworks.Add(network);
            }
        });

        services.AddResponseCompression(opt => {
            opt.EnableForHttps = true;
        });

        services
            .ConfigureApiKeys(Configuration)
            .ConfigureApplicationCookie(options => Configuration.Bind("CookieSettings", options));

        services
            .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opt => {
                // store the cookie no longer than 30 days
                opt.Cookie.Name = "mystcraft";
                opt.Cookie.MaxAge = TimeSpan.FromDays(30);

                // the authentication ticket will be valid for 7 days from the last time the user was active
                opt.SlidingExpiration = true;
                opt.ExpireTimeSpan = TimeSpan.FromDays(7);

                opt.Events.OnValidatePrincipal = AccountController.ValidatePrinciaplAsync;
            })
            .AddCookie(ExternalAuthentication.AuthenticationScheme, opt => {
                opt.Cookie.MaxAge = null;
                opt.SlidingExpiration = false;
            })
            .AddApiKeys()
            .AddOAuth(DiscordDefaults.AuthenticationScheme, opt => {
                DiscordOAuthConfiguration.Bind(opt);

                opt.SignInScheme = ExternalAuthentication.AuthenticationScheme;
                opt.SaveTokens = true;
            });

        services
            .AddAuthorization(conf => {
                var builder = new AuthorizationPolicyBuilder(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    ApiKeyDefaults.AuthenticationScheme
                );
                builder.RequireAuthenticatedUser();

                conf.DefaultPolicy = builder.Build();
                // conf.FallbackPolicy = conf.DefaultPolicy;

                conf.AddPolicyAnyRole(Policies.Root, Roles.Root);
                conf.AddPolicyAnyRole(Policies.GameMasters, Roles.Root, Roles.GameMaster);
                conf.AddPolicyAnyRole(Policies.UserManagers, Roles.Root, Roles.UserManager);

                conf.AddOwnPlayerPolicy(Policies.OwnPlayer);
            });

        services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(Configuration.GetValue<string>("DataProtection:Path")));

        services.AddHttpsRedirection(options => {
            options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
            options.HttpsPort = 443;
        });

        services.AddHttpClient();
        services.AddHttpContextAccessor();

        services
            .AddCors(opt => {
                opt.AddDefaultPolicy(builder => builder
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowAnyOrigin());
            });

        services.AddDatabase(new DatabaseOptions {
            Provider = DbProvider,
            ConnectionString = DbConnectionString,
            IsProduction = Env.IsProduction()
        });

        // TODO: enable hangfire back later
        // services.AddHangfire(conf => {
        //     conf.SetDataCompatibilityLevel(CompatibilityLevel.Version_170);
        //     conf.UseSimpleAssemblyNameTypeSerializer();
        //     conf.UseRecommendedSerializerSettings();

        //     switch (DbProvider) {
        //         case DatabaseProvider.SQLite:
        //             conf.UseSQLiteStorage(HangfireConnectionString);
        //             break;

        //         case DatabaseProvider.PgSQL:
        //             conf.UsePostgreSqlStorage(HangfireConnectionString);
        //             break;

        //         case DatabaseProvider.MsSQL:
        //             conf.UseSqlServerStorage(HangfireConnectionString);
        //             break;
        //     }

        //     conf.UseConsole();
        //     conf.UseRecurringJobAdmin(typeof(Startup).Assembly);
        //     conf.UseHeartbeatPage(TimeSpan.FromSeconds(5));
        // });

        // GlobalJobFilters.Filters.Add(new JoiningSupportAttribute(new BackgroundJobStateChanger()));
        // GlobalStateHandlers.Handlers.Add(new JoiningState.Handler());

        // TODO: enable hangfire back later
        // services
        //     .AddHangfireServer((ctx, conf) => {
        //         conf.Activator = new HangfireContainerActivator(ctx);
        //     })
        //     .AddHangfireConsoleExtensions()
        //     .AddSingleton<IBackgroundProcess, ProcessMonitor>(_ => new ProcessMonitor(TimeSpan.FromSeconds(5)));

        services
            .AddAutoMapper(typeof(MappingProfile));

        services
            .AddGraphQLServer()
            .AddAuthorization()
            .AddApolloTracing()
            .AddProjections()
            .AddGlobalObjectIdentification()
            .AddConvention<INamingConventions>(sp => new DatabaseEntityNamingConventions())
            .AddHttpRequestInterceptor<GraphQLHttpRequestInterceptor>()
            .SetPagingOptions(new PagingOptions {
                DefaultPageSize = 100,
                MaxPageSize = 100,
                IncludeTotalCount = true
            })
            .ModifyRequestOptions(opt => {
                opt.IncludeExceptionDetails = !Env.IsProduction();
            })
            .ModifyOptions(opt => {
                opt.DefaultResolverStrategy = ExecutionStrategy.Serial;
            })
            .RegisterService<Database>()
            .RegisterService<IRuntimeAccessor>()
            .RegisterService<IMediator>()
            .RegisterService<IAuthorizationService>()
            .AddQueryType<QueryType>()
            .AddMutationType<Mutation>()
            .AddType<UploadType>()
            .AddType<UserType>()
                .AddType<UserResolvers>()
            .AddType<advisor.Schema.GameType>()
                .AddType<GameResolvers>()
            .AddType<PlayerType>()
                .AddType<PlayerResolvers>()
            .AddType<PlayerTurnType>()
                .AddType<PlayerTurnResolvers>()
            .AddType<TurnType>()
                .AddType<TurnResolvers>()
            .AddType<RegionType>()
                .AddType<RegionResolvers>()
            .AddType<UnitType>()
                .AddType<UnitResolvers>()
            .AddType<StructureType>()
                .AddType<StructureResolvers>()
            .AddType<FactionType>()
                .AddType<FactionResolvers>()
            .AddType<advisor.Schema.EventType>()
            .AddType<AllianceType>()
            .AddType<AllianceMemberType>()
                .AddType<AllianceMemberResolvers>()
            .AddType<GameEngineType>()
            .AddType<ItemInterfaceType>()
            .AddType<ItemType>()
            .AddType<UnitItemType>()
            .AddType<BattleItemType>()
            .AddType<TradableItemType>()
            .AddType<TreasuryItemType>()
            .AddType<TurnStatisticsItemType>()
            .AddType<RegionStatisticsItemType>()
            .AddType<MutationResultType>()
            .BindRuntimeType<DbProductionItem, ItemType>()
            .BindRuntimeType<DbSkill, SkillType>()
            .BindRuntimeType<JSkill, SkillType>()
            ;

        services.AddMvcCore()
            .AddDataAnnotations();

        // services.Scan(x => x
        //     .FromExecutingAssembly()
        //     .AddClasses(classes => classes.AssignableTo<IAuthorizationHandler>())
        //         .AsImplementedInterfaces()
        //         .WithSingletonLifetime()
        // );

        services
            .AddMediatR(typeof(Startup))
            .AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

        services
            .AddMemoryCache();

        services
            .AddRepositories()
            .AddSingleton<IAuthorizationHandler, OwnPlayerAuthorizationHandler>()
            .AddSingleton<IApiKeyStore, ConfigurationApiKeyStore>()
            .AddSingleton<IAccessControl, AccessControl>()
            .AddSingleton<IReportParser, ReportParser>()
            .AddTransient<AllJobs>()
            .AddScoped<IUserAccessor, UserAccessor>();
    }

    public void Configure(IApplicationBuilder app) {
        if (!Env.IsProduction()) {
            app.UseDeveloperExceptionPage();
        }

        app.UseForwardedHeaders();
        app.UseCors();
        app.UseResponseCompression();

        if (Env.IsProduction()) {
            app.UseHsts();
        }

        app
            .UseMiddleware<DefaultFilesMiddleware>()
            .UseRouting()
            .UseAuthentication()
            .UseAuthorization()
            .UseStaticFiles()
            .UseEndpoints(endpoints => {
                endpoints.MapControllers();
                endpoints.MapGraphQL();

                // TODO: enable hangfire back later
                // endpoints.MapHangfireDashboard(new DashboardOptions {
                //     Authorization = new[] {
                //         new RoleBasedDashboardAuthorizationFilter(Roles.Root)
                //     }
                // });
            });
    }
}
