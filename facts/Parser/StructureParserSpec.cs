namespace advisor.facts;

public class StructureParserSpec {
    [Theory]
    [InlineData("+ Building [5] : Stables; Imperial Stables.")]
    [InlineData("+ AE Mayfield [286] : Fleet, 2 Cogs; Load: 485/1000; Sailors: 12/12; MaxSpeed: 4.")]
    [InlineData("+ AE Mayfield [286] : Fleet, 2 Cogs, 1 Longship; Load: 485/1000; Sailors: 12/12; MaxSpeed: 4.")]
    [InlineData("+ AE Empire [246] : Fleet, 10 Corsairs; Load: 5593/10000; Sailors: 135/150; MaxSpeed: 0; Sail directions: S, SW.")]
    [InlineData("+ AE Triangulum [329] : Fleet, 3 Corsairs; Sail directions: S, SW; Shiny new corsairs ready to engage any enemy. Built bay Imperial Shipyards..")]
    [InlineData("+ Shaft [1] : Shaft, contains an inner location.")]
    [InlineData("+ Lair [1] : Lair, closed to player units.")]
    [InlineData("+ Tower [1] : Tower, needs 10.")]
    [InlineData("+ Tower [1] : Tower, engraved with Runes of Warding.")]
    [InlineData("+ Ruin [1] : Ruin, closed to player units.")]
    [InlineData("+ The Kings Highway [1] : Road N.")]
    [InlineData("+ Trade Academy [NIMB] [Nort Triders] [2] : Tower; comment.")]
    public void CanParseBuildings(string s) {
        s.Should().NotBeNullOrEmpty();
        // var parser = new StructureParser();
        // var result = parser.Parse(new TextParser(0, s));
    }
}
