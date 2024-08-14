namespace advisor.facts;

using advisor;
using advisor.Model;
using LanguageExt.Pipes;
using static LanguageExt.Prelude;

public class PipesTest {
    [Fact]
    public async Task BoundVariable() {
        var ret = (producer(10) | consumer()).RunEffect().Run(Runtime.New(null));
        var value = await ret.Unwrap();

        // ast bpund variable is returned
        value.Should().Be(-1);
    }

    private static Producer<Runtime, int, int> producer(int prop) =>
        from v in Proxy.lift<Runtime, int>(SuccessEff(0))
        from _ in Proxy.yield(v)
        select prop;

    private static Consumer<Runtime, int, int> consumer() =>
        from v in Proxy.awaiting<int>()
        select -1;
}
