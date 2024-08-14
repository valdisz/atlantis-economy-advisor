namespace advisor.facts;

using advisor;
using advisor.facts.Fixtures;
using advisor.Model;
using LanguageExt.Pipes;
using static LanguageExt.Prelude;

public class GameInterpreterIntegrationSpec: WithDatabaseSpec {
    [Fact]
    public async Task ShouldFailIfNoGameEnginePresent() {
        var result = await GameInterpreter<Runtime>.Interpret(
            from engine in Mystcraft.WriteOneGameEngine(new GameEngineId(1))
            select engine
        )
        .Run(Runtime.New(Db));

        result.IfSucc(_ => throw new Exception("Should not succeed"));
        result.IfFail(e => e.Should().Be(Errors.E_GAME_ENGINE_DOES_NOT_EXIST));
    }
}
