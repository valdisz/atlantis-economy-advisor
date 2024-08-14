namespace advisor.facts;

using advisor;
using advisor.Model;
using LanguageExt.Pipes;
using static LanguageExt.Prelude;

public class GameInterpreterSpec {
    [Fact]
    public void CanInterpret() {
        var value = GameId.New(1).Match(
            Succ: gameId => {
                return GameInterpreter<Runtime>.Interpret(Mystcraft.ReadOneGame(gameId));
            },
            Fail: _ => throw new Exception("Should not fail")
        );

        value.Should().NotBeNull();
    }

    [Fact]
    public void CanWriteOneGameEngine() {
        var value = GameEngineId.New(1)
            .Match(
                Succ: gameEngineId =>
                    GameInterpreter<Runtime>.Interpret(
                        from engine in Mystcraft.WriteOneGameEngine(gameEngineId)
                        select engine
                    ),
                Fail: _ => throw new Exception("Should not fail")
            );

        value.Should().NotBeNull();
    }

    [Fact]
    public void CanDeleteGameEngine() {
        var value = GameInterpreter<Runtime>.Interpret(
            from _ in Mystcraft.DeleteGameEngine(new Persistence.DbGameEngine { Id = 1 })
            select _
        );

        value.Should().NotBeNull();
    }
}
