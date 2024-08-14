namespace advisor.facts;

using System.IO;
using System.Linq;
using advisor.TurnProcessing;

public class PlayersFileReaderSpec {
    [Fact]
    public void CanReadPlayersFile() {
        using var stream = File.OpenRead("data/players_playing-factions");

        var reader = new PlayersFileReader(stream);

        var faction = reader.First();
        faction.IsNew.Should().BeFalse();
        faction.Number.Should().Be(1);
        faction.Name.Should().Be("The Guardsmen");
        faction.Props.Count.Should().Be(8);
    }

    [Fact]
    public void CanReadPlayersFileWithNewFactions() {
        using var stream = File.OpenRead("data/players_with-new-factions");

        var reader = new PlayersFileReader(stream);

        var newFactions = reader.Where(x => x.IsNew).ToList();
        newFactions.Count.Should().Be(1);
        newFactions[0].Number.Should().BeNull();
        newFactions[0].Name.Should().Be("A New Faction");
    }
}
