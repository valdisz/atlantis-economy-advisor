namespace advisor.facts;

using System.IO;
using System.Threading.Tasks;
using Xunit;

public class BattlesSectionSpec {
    [Fact]
    public async Task CanParseBattles() {
        using var source = new StringReader(File.ReadAllText(@"data/battle"));
        using var converter = new AtlantisReportJsonConverter(
            source,
            new BattlesSection()
        );

        var json = await converter.ReadAsJsonAsync();


        File.WriteAllText("battle.json", json.ToString());
    }
}
