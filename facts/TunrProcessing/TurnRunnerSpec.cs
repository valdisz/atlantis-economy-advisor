namespace advisor.facts;

using System.Text;
using System.Threading;
using advisor.IO;
using advisor.TurnProcessing;
using LanguageExt;
using LanguageExt.Effects.Traits;
using LanguageExt.Sys.Traits;

public sealed class TurnRunnerTestRuntimeEnv {
    public TurnRunnerTestRuntimeEnv(CancellationTokenSource source, CancellationToken token) {
        Source = source;
        Token  = token;
    }

    public TurnRunnerTestRuntimeEnv(CancellationTokenSource source) : this(source, source.Token) {
    }

    public readonly CancellationTokenSource Source;
    public readonly CancellationToken Token;
}

public readonly struct TurnRunnerTestRuntime :
    HasCancel<TurnRunnerTestRuntime>,
    HasUnix<TurnRunnerTestRuntime>,
    HasDirectory<TurnRunnerTestRuntime>,
    HasFile<TurnRunnerTestRuntime>
{
    TurnRunnerTestRuntime(TurnRunnerTestRuntimeEnv env) =>
        this.env = env;

    readonly TurnRunnerTestRuntimeEnv env;

    public static TurnRunnerTestRuntime New() =>
        new(new TurnRunnerTestRuntimeEnv(new CancellationTokenSource()));

    public static TurnRunnerTestRuntime New(CancellationToken linkedToken) =>
        new(new TurnRunnerTestRuntimeEnv(CancellationTokenSource.CreateLinkedTokenSource(linkedToken)));

    public TurnRunnerTestRuntimeEnv Env =>
        env ?? throw new InvalidOperationException("Runtime Env not set. Perhaps because of using default(Runtime) or new Runtime() rather than Runtime.New()");

    public Eff<TurnRunnerTestRuntime, FileIO> FileEff =>
        SuccessEff(LanguageExt.Sys.Live.FileIO.Default);

    public Encoding Encoding => throw new NotImplementedException();

    public TurnRunnerTestRuntime LocalCancel =>
        New();

    public CancellationToken CancellationToken =>
        Env.Token;

    public CancellationTokenSource CancellationTokenSource =>
        Env.Source;

    public Eff<TurnRunnerTestRuntime, IO.Traits.UnixIO> UnixEff =>
        SuccessEff(UnixIO.Default);

    public Eff<TurnRunnerTestRuntime, DirectoryIO> DirectoryEff =>
        SuccessEff(LanguageExt.Sys.Live.DirectoryIO.Default);
}

public class TurnRunnerSpec : IDisposable {
    public TurnRunnerSpec() {
        options = TurnRunnerOptions.UseTempDirectory();
        runtime = TurnRunnerTestRuntime.New();

    }

    readonly TurnRunnerOptions options;
    readonly TurnRunnerTestRuntime runtime;

    public void Dispose() {
        Directory.Delete(options.WorkingDirectory, true);
    }

    private Task WriteEngineAsync() => runner.WriteEngineAsync(File.ReadAllBytes("data/engine"));
    private Task WriteGameInAsync() => runner.WriteGameAsync(File.ReadAllBytes("data/game.in"));
    private Task WritePlayersInAsync() => runner.WritePlayersAsync(File.ReadAllBytes("data/players.in"));

    private async Task PrepareForTurnAsync() {
        var foo = from _ in TurnProcessing<TurnRunnerTestRuntime>.WriteEngine(new GameEngineId(1))
        select unit;

        await WriteEngineAsync();
        await WriteGameInAsync();
        await WritePlayersInAsync();
    }

    [Fact]
    public async Task EngineIsWrittenToWorkFolder() {
        await WriteEngineAsync();

        File.Exists(Path.Combine(options.WorkingDirectory, options.EngineFileName)).Should().BeTrue();
    }

    [Fact]
    public async Task GameInIsWrittenToWorkFolder() {
        await WriteGameInAsync();

        File.Exists(Path.Combine(options.WorkingDirectory, options.GameInFileName)).Should().BeTrue();
    }

    [Fact]
    public async Task PlayersInIsWrittenToWorkFolder() {
        await WritePlayersInAsync();

        File.Exists(Path.Combine(options.WorkingDirectory, options.PlayersInFileName)).Should().BeTrue();
    }

    [Fact]
    public async Task CanRunAsync() {
        await PrepareForTurnAsync();

        var result = await runner.RunAsync(TimeSpan.FromMinutes(1));

        result.ExitCode.Should().Be(0);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GameOutFileIsCreatedAfterRun() {
        await PrepareForTurnAsync();

        var result = await runner.RunAsync(TimeSpan.FromMinutes(1));

        File.Exists(Path.Combine(options.WorkingDirectory, options.GameOutFileName)).Should().BeTrue();
    }

    [Fact]
    public async Task PlayersOutFileIsCreatedAfterRun() {
        await PrepareForTurnAsync();

        var result = await runner.RunAsync(TimeSpan.FromMinutes(1));

        File.Exists(Path.Combine(options.WorkingDirectory, options.PlayersOutFileName)).Should().BeTrue();
    }

    [Fact]
    public async Task ReportFilesAreCreatedAfterRun() {
        await PrepareForTurnAsync();

        var result = await runner.RunAsync(TimeSpan.FromMinutes(1));

        var reports = runner.ListReports().ToList();

        reports.Count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task TemplateFilesAreCreatedAfterRun() {
        await PrepareForTurnAsync();

        var result = await runner.RunAsync(TimeSpan.FromMinutes(1));

        var templates = runner.ListTemplates().ToList();

        templates.Count.Should().BeGreaterThan(0);
    }
}
