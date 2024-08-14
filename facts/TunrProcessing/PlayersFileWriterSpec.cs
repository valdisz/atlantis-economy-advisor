namespace advisor.facts;

using System.IO;
using advisor.TurnProcessing;

public class PlayersFileWriterSpec {
    [Fact]
    public void CanWritePlayersFile()
    {
        using var writer = new StringWriter();

        var rec = new FactionRecord(1);
        rec.Add("Name", "The Guardsmen");

        var playersWriter = new PlayersFileWriter(writer);
        playersWriter.Write(rec);

        var result = writer.ToString();
        result.Should().Be("Faction: 1\nName: The Guardsmen\n");
    }
}
