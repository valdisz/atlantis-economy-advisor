namespace advisor.facts; 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pidgin;
using Xunit;
using Xunit.Abstractions;

public class GeneralParserSpec {
    public GeneralParserSpec(ITestOutputHelper output) {
        this.output = output;
    }

    private readonly ITestOutputHelper output;

    [Theory]
    [InlineData("35 skeletons [SKEL] (Combat 2/2, Attacks 2, Hits 2, Tactics 0)", 35, "skeletons", "SKEL", "Combat 2/2, Attacks 2, Hits 2, Tactics 0")]
    [InlineData("35 skeletons [SKEL] (Combat 2/2, Attacks 2, Hits 2, Tactics 0), 24 horses [HORS], 6 undead [UNDE] (Combat 3/3, Attacks 12, Hits 12, Tactics 0)", 35, "skeletons", "SKEL", "Combat 2/2, Attacks 2, Hits 2, Tactics 0")]
    public async Task ParseItem(string s, int amount, string name, string code, string props) {
        var parser = new ItemParser();

        TextParser tp = new(0, s);
        var result = parser.Parse(tp).Value;

        using StringWriter sw = new();
        using (JsonWriter jw = new JsonTextWriter(sw)) {
            await result.WriteJson(jw);
        }

        dynamic o = JObject.Parse(sw.ToString());
        int pAmount = o.amount;
        string pCode = o.code;
        string pName = o.name;
        string pProps = o.props;
        int? pPrice = o.price;

        pAmount.Should().Be(amount);
        pCode.Should().Be(code);
        pName.Should().Be(name);
        pProps.Should().Be(props);
    }

//         [Theory]
//         [InlineData("War 1", "War", 1)]
//         [InlineData("Trade 1", "Trade", 1)]
//         [InlineData("Magic 1", "Magic", 1)]
//         [InlineData("Ocean Fleets 5", "Ocean Fleets", 5)]
//         public void canParseFactionAttribute(string input, string key, int value) {
//             var result = AtlantisParser.FactionAttribute.Parse(input);
//             output.AssertParsed(result);

//             (result.Value.Children[0] as ValueReportNode<string>).Value.Should().Be(key);
//             (result.Value.Children[1] as ValueReportNode<int>).Value.Should().Be(value);
//         }

//         [Theory]
//         [InlineData("(War 1, Trade 2, Magic 2)", 3)]
//         [InlineData("(War 1,\n  Trade 2, Magic 2)", 3)]
//         [InlineData("(War 1, Trade 2,\n  Magic 2)", 3)]
//         public void canParseFactionAttributes(string input, int count) {
//             var result = AtlantisParser.FactionAttributes.Parse(input);
//             output.AssertParsed(result);

//             result.Value.Children.Count.Should().Be(count);
//         }

//         [Fact]
//         public void canParseFactionNumber() {
//             var result = AtlantisParser.FactionNumber.Parse("(15)");
//             output.AssertParsed(result);

//             (result.Value as ValueReportNode<int>).Value.Should().Be(15);
//         }

//         [Theory]
//         [InlineData("Avalon Empire (15)", "Avalon Empire", 15)]
//         [InlineData("Avalon Empire  (15)", "Avalon Empire", 15)]
//         [InlineData("Avalon Empire   (15)", "Avalon Empire", 15)]
//         [InlineData("Avalon\n  Empire (15)", "Avalon Empire", 15)]
//         [InlineData("Avalon Empire\n  (15)", "Avalon Empire", 15)]
//         public void canParseFactionNameAndNumber(string input, string name, int number) {
//             var result = AtlantisParser.Faction.Parse(input);
//             output.AssertParsed(result);

//             var faction = result.Value;
//             faction.StrValueOf("name").Should().Be(name);
//             faction.IntValueOf("number").Should().Be(number);
//         }

//         [Theory]
//         [InlineData("Avalon Empire (15) (War 1, Trade 2, Magic 2)", "Avalon Empire", 15, 3)]
//         [InlineData("Avalon Empire (15) (War 1, Trade 2, Magic\n  2)", "Avalon Empire", 15, 3)]
//         [InlineData("Avalon Empire (15) (War 1, Trade 2,\n  Magic 2)", "Avalon Empire", 15, 3)]
//         [InlineData("Avalon Empire (15) (War 1, Trade\n  2, Magic 2)", "Avalon Empire", 15, 3)]
//         [InlineData("Avalon Empire (15) (War 1,\n  Trade 2, Magic 2)", "Avalon Empire", 15, 3)]
//         [InlineData("Avalon Empire (15) (War\n  1, Trade 2, Magic 2)", "Avalon Empire", 15, 3)]
//         [InlineData("Avalon Empire (15)\n  (War 1, Trade 2, Magic 2)", "Avalon Empire", 15, 3)]
//         [InlineData("Avalon Empire\n  (15) (War 1, Trade 2, Magic 2)", "Avalon Empire", 15, 3)]
//         [InlineData("Avalon\n  Empire (15) (War 1, Trade 2, Magic 2)", "Avalon Empire", 15, 3)]
//         public void canParseReportFaction(string input, string name, int number, int count) {
//             var result = AtlantisParser.ReportFaction.Parse(input);
//             output.AssertParsed(result);

//             var faction = result.Value;

//             faction.FirstByType("faction")?.StrValueOf("name").Should().Be(name);
//             faction.FirstByType("faction")?.IntValueOf("number").Should().Be(number);
//             faction.FirstByType("attributes")?.Children.Count.Should().Be(count);
//         }

//         [Theory]
//         [InlineData("May, Year 3", "May", 3)]
//         [InlineData("February, Year 1", "February", 1)]
//         [InlineData("February, Year\n  1", "February", 1)]
//         [InlineData("February,\n  Year 1", "February", 1)]
//         public void canParseReportDate(string input, string month, int year) {
//             var result = AtlantisParser.ReportDate.Parse(input);
//             output.AssertParsed(result);

//             var node = result.Value;
//             (node.Children[0] as ValueReportNode<string>).Value.Should().Be(month);
//             (node.Children[1] as ValueReportNode<int>).Value.Should().Be(year);
//         }

//         [Theory]
//         [InlineData("(0,0,2 <underworld>)", 0, 0, 2, "underworld")]
//         [InlineData("(0,0,2\n  <underworld>)", 0, 0, 2, "underworld")]
//         [InlineData("(1,1)", 1, 1, null, null)]
//         public void canParseCoords(string input, int x, int y, int? z, string label) {
//             var result = AtlantisParser.Coords.Parse(input);
//             output.AssertParsed(result);

//             var v = result.Value;
//             v.IntValueOf("x").Should().Be(x);
//             v.IntValueOf("y").Should().Be(y);
//             v.IntValueOf("z").Should().Be(z);
//             v.StrValueOf("label").Should().Be(label);
//         }

//         [Theory]
//         [InlineData("contains Sinsto [town]", "Sinsto", "town")]
//         public void canParseSettlement(string input, string name, string size) {
//             var result = AtlantisParser.Settlement.Parse(input);
//             output.AssertParsed(result);

//             var v = result.Value;
//             v.StrValueOf("name").Should().Be(name);
//             v.StrValueOf("size").Should().Be(size);
//         }

//         [Theory]
//         [InlineData("1195 peasants (gnomes)", 1195, "gnomes")]
//         [InlineData("10237 peasants (drow elves)", 10237, "drow elves")]
//         [InlineData("10237 peasants (drow\n  elves)", 10237, "drow elves")]
//         [InlineData("10237 peasants\n  (drow elves)", 10237, "drow elves")]
//         [InlineData("10237\n  peasants (drow elves)", 10237, "drow elves")]
//         public void canParsePopulation(string input, int amount, string race) {
//             var result = AtlantisParser.Population.Parse(input);
//             output.AssertParsed(result);

//             var v = result.Value;
//             v.IntValueOf("amount").Should().Be(amount);
//             v.StrValueOf("race").Should().Be(race);
//         }

//         [Theory]
//         [InlineData("underforest (50,0,2 <underworld>) in Ryway, 100 peasants (humans).\n")]
//         [InlineData("underforest (50,0,2 <underworld>) in Ryway, 100 peasants\n  (humans).\n")]
//         [InlineData("underforest (50,0,2 <underworld>) in Ryway, 100\n  peasants (humans).\n")]
//         [InlineData("underforest (50,0,2 <underworld>) in Ryway,\n  100 peasants (humans).\n")]
//         [InlineData("underforest (50,0,2 <underworld>) in\n  Ryway, 100 peasants (humans).\n")]
//         [InlineData("underforest (50,0,2 <underworld>)\n  in Ryway, 100 peasants (humans).\n")]
//         [InlineData("underforest (50,0,2\n  <underworld>) in Ryway, 100 peasants (humans).\n")]
//         [InlineData("underforest\n  (50,0,2 <underworld>) in Ryway, 100 peasants (humans).\n")]
//         [InlineData("underforest (50,0,2 <underworld>) in Ryway, contains Sinsto [town], 10237 peasants (drow elves), $5937.\n")]
//         [InlineData("underforest (50,0,2 <underworld>) in Ryway, contains Sinsto [town],\n  10237 peasants (drow elves), $5937.\n")]
//         [InlineData("underforest (50,0,2 <underworld>) in Ryway, contains Sinsto\n  [town], 10237 peasants (drow elves), $5937.\n")]
//         [InlineData("underforest (50,0,2 <underworld>) in Ryway, contains\n  Sinsto [town], 10237 peasants (drow elves), $5937.\n")]
//         [InlineData("underforest (50,0,2 <underworld>) in Ryway,\n  contains Sinsto [town], 10237 peasants (drow elves), $5937.\n")]
//         [InlineData("underforest (52,0,2 <underworld>) in Ryway, 1872 peasants (drow elves), $1385.\n")]
//         public void canParseRegionHeader(string input) {
//             var result = AtlantisParser.RegionHeader.Parse(input);
//             output.AssertParsed(result);

//             var v = result.Value;
//         }

//         [Theory]
//         [InlineData("The weather was clear last month; it will be clear next month.", null)]
//         [InlineData("Wages: $16.9 (Max: $5331).", "Wages")]
//         [InlineData("Wanted: none.", "Wanted")]
//         [InlineData("For Sale: 65 orcs [ORC] at $42, 13 leaders [LEAD] at $744.", "For Sale")]
//         [InlineData("Entertainment available: $54.", "Entertainment available")]
//         [InlineData("Products: 15 grain [GRAI].", "Products")]
//         public void canParseRegionParam(string input, string capture) {
//             var result = AtlantisParser.RegionAttribute.Parse(input);

//             if (capture == null) {
//                 result.Success.Should().BeFalse();
//                 return;
//             }

//             output.AssertParsed(result);
//             result.Value.Should().Be(capture);
//         }

//         [Fact]
//         public void canParseRegionParams() {
//             var input = @"------------------------------------------------------------
//   The weather was clear last month; it will be clear next month.
//   Wages: $13.3 (Max: $466).
//   Wanted: none.
//   For Sale: 65 orcs [ORC] at $42, 13 leaders [LEAD] at $744.
//   Entertainment available: $54.
//   Products: 15 grain [GRAI].

// ";
//             var result = AtlantisParser.RegionAttributes.Parse(input);

//             var items = output.AssertParsed(result).Value.ToArray();


//             items.FirstOrDefault(x => x.Type == "unknown")?.StrValueOf("value").Should().Be("The weather was clear last month; it will be clear next month.");
//             items.FirstOrDefault(x => x.Type == "wages")?.RealValue().Should().Be(13.3);
//             items.FirstOrDefault(x => x.Type == "wages")?.IntValue().Should().Be(466);
//             items.FirstOrDefault(x => x.Type == "wanted")?.Children.Count.Should().Be(0);
//             items.FirstOrDefault(x => x.Type == "for-sale")?.Children?.Count.Should().Be(2);
//             items.FirstOrDefault(x => x.Type == "entertainment-available").Should().Be(54);
//             items.FirstOrDefault(x => x.Type == "products")?.Children?.Count.Should().Be(1);
//         }

//         [Theory]
//         [InlineData("orc [ORC] at $42", 1, "orc", "ORC", 42)]
//         [InlineData("orc [ORC] at\n  $42", 1, "orc", "ORC", 42)]
//         [InlineData("orc [ORC]\n  at $42", 1, "orc", "ORC", 42)]
//         [InlineData("orc\n  [ORC] at $42", 1, "orc", "ORC", 42)]
//         [InlineData("65 orcs [ORC] at $42", 65, "orcs", "ORC", 42)]
//         [InlineData("65\n  orcs [ORC] at $42", 65, "orcs", "ORC", 42)]
//         [InlineData("65 orcs [ORC]", 65, "orcs", "ORC", null)]
//         [InlineData("orc [ORC]", 1, "orc", "ORC", null)]
//         public void canParseItem(string input, int amount, string name, string code, int? price) {
//             var result = AtlantisParser.Item.Parse(input);
//             output.AssertParsed(result);

//             var value = result.Value;
//             value.IntValueOf("amount").Should().Be(amount);
//             value.StrValueOf("name").Should().Be(name);
//             value.StrValueOf("code").Should().Be(code);
//             value.IntValueOf("price").Should().Be(price);
//         }

//         [Theory]
//         [InlineData(@"underforest (50,0,2 <underworld>) in Ryway, contains Sinsto [town],
//   10237 peasants (drow elves), $5937.
// ------------------------------------------------------------
//   The weather was clear last month; it will be clear next month.
//   Wages: $12.9 (Max: $1187).
//   Wanted: 167 grain [GRAI] at $20, 115 livestock [LIVE] at $20, 123
//     fish [FISH] at $27, 7 leather armor [LARM] at $69.
//   For Sale: 409 drow elves [DRLF] at $41, 81 leaders [LEAD] at $722.
//   Entertainment available: $399.
//   Products: 17 livestock [LIVE], 12 wood [WOOD], 15 stone [STON], 12
//     iron [IRON].

// Exits:
//   Southeast : underforest (51,1,2 <underworld>) in Ryway.
//   South : underforest (50,2,2 <underworld>) in Ryway.
//   Southwest : underforest (49,1,2 <underworld>) in Hawheci.

// - Unit (2465), Semigallians (18), avoiding, behind, under dwarf
//   [UDWA].
// - Scout (2070), Disasters Inc (43), avoiding, behind, revealing
//   faction, receiving no aid, high elf [HELF], horse [HORS], 40 silver
//   [SILV]. Weight: 60. Capacity: 0/70/85/0. Skills: riding [RIDI] 1
//   (60).
// - tactician (5246), Anuii (44), revealing faction, holding, weightless
//   battle spoils, drow elf [DRLF], 6 silver [SILV]. Weight: 10.
//   Capacity: 0/0/15/0. Skills: tactics [TACT] 2 (90).
// - taxmen (5600), Anuii (44), revealing faction, holding, sharing, 2
//   under dwarves [UDWA], 118 drow elves [DRLF], horse [HORS], 14241
//   silver [SILV]. Weight: 1250. Capacity: 0/70/1870/0. Skills: combat
//   [COMB] 1 (35).
// - Ambassador (5279), Noizy Tribe (49), avoiding, behind, orc [ORC],
//   horse [HORS].
// - Unit (9349), Anuii (44), avoiding, behind, revealing faction,
//   holding, weightless battle spoils, under dwarf [UDWA], 2 scrying
//   orbs [SORB], 9734 silver [SILV]. Weight: 10. Capacity: 0/0/15/0.
//   Skills: none.
// - b gemcutting (6366), Disasters Inc (43), avoiding, behind, revealing
//   faction, 10 under dwarves [UDWA], 5 horses [HORS], 300 silver
//   [SILV]. Weight: 350. Capacity: 0/350/500/0. Skills: gemcutting
//   [GCUT] 3 (180).

// + Shaft [1] : Shaft, contains an inner location.
//   - scout (4637), Anuii (44), avoiding, behind, revealing faction,
//     receiving no aid, sharing, weightless battle spoils, under dwarf
//     [UDWA], horse [HORS], 52 silver [SILV]. Weight: 60. Capacity:
//     0/70/85/0. Skills: none.
// ", 7)]
//         [InlineData(@"forest (50,22) in Mapa, contains Sembury [village], 5866 peasants
//   (high elves), $2698.
// ------------------------------------------------------------
//   Wages: $12.3 (Max: $539).
//   Wanted: 96 grain [GRAI] at $21, 112 livestock [LIVE] at $19, 86 fish
//     [FISH] at $23.
//   For Sale: 234 high elves [HELF] at $39, 46 leaders [LEAD] at $688.
//   Entertainment available: $193.
//   Products: 32 grain [GRAI], 24 wood [WOOD], 11 furs [FUR], 16 herbs
//     [HERB].

// Exits:
//   North : forest (50,20) in Mapa.
//   Northeast : plain (51,21) in Inthon.
//   Southeast : plain (51,23) in Inthon.
//   South : ocean (50,24) in Atlantis Ocean.
//   Southwest : ocean (49,23) in Atlantis Ocean.
//   Northwest : forest (49,21) in Mapa.

// - City Guard (35), on guard, The Guardsmen (1), 40 leaders [LEAD], 40
//   swords [SWOR].
// * Emperor (456), Avalon Empire (15), avoiding, behind, holding, won't
//   cross water, leader [LEAD]. Weight: 10. Capacity: 0/0/15/0. Skills:
//   force [FORC] 1 (30), pattern [PATT] 1 (30), spirit [SPIR] 1 (30),
//   gate lore [GATE] 1 (30), combat [COMB] 3 (180), endurance [ENDU] 3
//   (180). Can Study: fire [FIRE], earthquake [EQUA], force shield
//   [FSHI], energy shield [ESHI], spirit shield [SSHI], magical healing
//   [MHEA], farsight [FARS], mind reading [MIND], weather lore [WEAT],
//   earth lore [EART], necromancy [NECR], demon lore [DEMO], illusion
//   [ILLU], artifact lore [ARTI].
// ", 2)]
//         [InlineData(@"mountain (10,2) in Metfle, 2021 peasants (orcs), $889.
// ------------------------------------------------------------
//   The weather was clear last month; it will be clear next month.
//   Wages: $12.2 (Max: $370).
//   Wanted: none.
//   For Sale: 80 orcs [ORC] at $39, 16 leaders [LEAD] at $683.
//   Entertainment available: $53.
//   Products: 28 livestock [LIVE], 38 iron [IRON], 11 stone [STON], 6
//     mithril [MITH].

// Exits:
//   North : ocean (10,0) in Atlantis Ocean.
//   Northeast : ocean (11,1) in Atlantis Ocean.
//   Southeast : mountain (11,3) in Metfle.
//   South : mountain (10,4) in Metfle.
//   Southwest : ocean (9,3) in Atlantis Ocean.
//   Northwest : ocean (9,1) in Atlantis Ocean.

// - p iron (5220), Anuii (44), avoiding, behind, revealing faction,
//   sharing, weightless battle spoils, 6 humans [MAN], 6 picks [PICK],
//   52 iron [IRON]. Weight: 326. Capacity: 0/0/90/0. Skills: mining
//   [MINI] 3 (260), riding [RIDI] 1 (30).
// - p mith (5561), Anuii (44), avoiding, behind, revealing faction,
//   weightless battle spoils, 2 humans [MAN], 12 mithril [MITH]. Weight:
//   140. Capacity: 0/0/30/0. Skills: mining [MINI] 3 (260), riding
//   [RIDI] 1 (30).
// - p weap-quar (7637), Anuii (44), avoiding, behind, revealing faction,
//   weightless battle spoils, 12 orcs [ORC], 16 hammers [HAMM], 11 stone
//   [STON], 1028 silver [SILV]. Weight: 686. Capacity: 0/0/180/0.
//   Skills: weaponsmith [WEAP] 2 (90), quarrying [QUAR] 1 (40).
// - miner (2898), Anuii (44), avoiding, behind, revealing faction,
//   weightless battle spoils, 2 humans [MAN], 2 picks [PICK], 20 iron
//   [IRON]. Weight: 122. Capacity: 0/0/30/0. Skills: mining [MINI] 4
//   (300), riding [RIDI] 1 (30).
// - Jester (5626), The Lizards (20), avoiding, behind, orc [ORC].
// - Lizardmen (6656), The Lizards (20), avoiding, behind, lizardman
//   [LIZA].
// - taxmen (5923), Anuii (44), revealing faction, holding, 8 wood elves
//   [WELF], 378 silver [SILV]. Weight: 80. Capacity: 0/0/120/0. Skills:
//   combat [COMB] 1 (30).
// - scout (2907), on guard, Anuii (44), behind, revealing faction,
//   holding, weightless battle spoils, centaur [CTAU], 48 silver [SILV].
//   Weight: 50. Capacity: 0/70/70/0. Skills: riding [RIDI] 1 (55).
// - scout (4611), Anuii (44), avoiding, behind, revealing faction,
//   weightless battle spoils, orc [ORC], 2 silver [SILV]. Weight: 10.
//   Capacity: 0/0/15/0. Skills: none.

// + Shaft [1] : Shaft, contains an inner location.

// + Fleet [177] : Cog; Load: 20/500; Sailors: 6/6; MaxSpeed: 4.
//   - captain cog 177 (6792), Anuii (44), avoiding, behind, revealing
//     faction, weightless battle spoils, 2 lizardmen [LIZA], 980 silver
//     [SILV]. Weight: 20. Capacity: 0/0/30/30. Skills: sailing [SAIL] 3
//     (205).
// ", 9)]
//         [InlineData(@"forest (52,10) in Hiewmouckdoi, 2334 peasants (wood elves), $700.
// ------------------------------------------------------------
//   The weather was clear last month; it will be clear next month.
//   Wages: $11.5 (Max: $284).
//   Wanted: none.
//   For Sale: 93 wood elves [WELF] at $36, 18 leaders [LEAD] at $644.
//   Entertainment available: $51.
//   Products: 28 livestock [LIVE], 26 wood [WOOD], 17 furs [FUR], 16
//     herbs [HERB].

// Exits:
//   North : ocean (52,8) in Atlantis Ocean.
//   Northeast : forest (53,9) in Hiewmouckdoi.
//   Southeast : ocean (53,11) in Atlantis Ocean.
//   South : ocean (52,12) in Atlantis Ocean.
//   Southwest : ocean (51,11) in Atlantis Ocean.
//   Northwest : ocean (51,9) in Atlantis Ocean.

// - forest guard (649), on guard, Disasters Inc (43), revealing faction,
//   sharing, 11 wood elves [WELF], horse [HORS]. Weight: 160. Capacity:
//   0/70/235/0. Skills: combat [COMB] 1 (35).
// - p lumberjacks (1487), Disasters Inc (43), avoiding, behind,
//   revealing faction, sharing, 13 wood elves [WELF], 26 wood [WOOD].
//   Weight: 260. Capacity: 0/0/195/0. Skills: lumberjack [LUMB] 2 (158).
// - p hunt-armo (2823), Disasters Inc (43), avoiding, behind, revealing
//   faction, sharing, 5 wood elves [WELF], 35 leather armor [LARM].
//   Weight: 85. Capacity: 0/0/75/0. Skills: hunting [HUNT] 1 (60),
//   armorer [ARMO] 1 (75).
// - skauts (3329), Semigallians (18), avoiding, behind, wood elf [WELF].
// - p weapon (6348), Disasters Inc (43), avoiding, behind, revealing
//   faction, sharing, 10 wood elves [WELF], 10 axes [AXE], 60 crossbows
//   [XBOW]. Weight: 170. Capacity: 0/0/150/0. Skills: weaponsmith [WEAP]
//   1 (65).
// - p ranchers (6349), Disasters Inc (43), avoiding, behind, revealing
//   faction, 13 wood elves [WELF], 78 livestock [LIVE]. Weight: 4030.
//   Capacity: 0/0/4095/0. Skills: ranching [RANC] 1 (62).
// - p hunters (6350), Disasters Inc (43), avoiding, behind, revealing
//   faction, sharing, 17 wood elves [WELF], 49 furs [FUR]. Weight: 219.
//   Capacity: 0/0/255/0. Skills: hunting [HUNT] 1 (63).
// - p herbers (6756), Disasters Inc (43), avoiding, behind, revealing
//   faction, 10 wood elves [WELF], 54 herbs [HERB]. Weight: 100.
//   Capacity: 0/0/150/0. Skills: herb lore [HERB] 2 (110).
// - p armorers (7137), Disasters Inc (43), avoiding, behind, revealing
//   faction, sharing, 10 wood elves [WELF], 30 leather armor [LARM].
//   Weight: 130. Capacity: 0/0/150/0. Skills: armorer [ARMO] 1 (55).
// - p quick-xbow (7138), Disasters Inc (43), avoiding, behind, revealing
//   faction, sharing, 10 wood elves [WELF], 30 crossbows [XBOW]. Weight:
//   130. Capacity: 0/0/150/0. Skills: weaponsmith [WEAP] 1 (55).
// - p shipbuilders (2626), Disasters Inc (43), avoiding, behind,
//   revealing faction, consuming faction's food, 9 high elves [HELF], 4
//   horses [HORS]. Weight: 290. Capacity: 0/280/415/0. Skills:
//   shipbuilding [SHIP] 4 (368).
// - p shipbuilders (1702), Disasters Inc (43), avoiding, behind,
//   revealing faction, consuming faction's food, 9 high elves [HELF],
//   unfinished Galleon [GALL] (needs 54). Weight: 90. Capacity:
//   0/0/135/0. Skills: shipbuilding [SHIP] 4 (357).
// - skauts (1231), Semigallians (18), avoiding, behind, wood elf [WELF],
//   20 horses [HORS].
// - watcher (8025), Disasters Inc (43), avoiding, behind, sharing,
//   centaur [CTAU]. Weight: 50. Capacity: 0/70/70/0. Skills: riding
//   [RIDI] 3 (190), observation [OBSE] 2 (90), stealth [STEA] 1 (30),
//   entertainment [ENTE] 1 (30).
// - kurjers (7601), Semigallians (18), avoiding, behind, human [MAN],
//   horse [HORS].
// - trans Chawi-SWforest (5894), Disasters Inc (43), avoiding, behind,
//   revealing faction, sharing, human [MAN], 5 horses [HORS], 1050
//   silver [SILV]. Weight: 260. Capacity: 0/350/365/0. Skills: none.
// ", 16)]
//         public void canParseRegion(string input, int unitCount) {
//             var result = AtlantisParser.Region.Before(Parser<char>.End).Parse(input);
//             var region = output.AssertParsed(result).Value;

//             (region.FirstByType("units")?.Children?.Count ?? 0).Should().Be(unitCount);
//         }

//         [Theory]
//         [InlineData(@"mountain (10,2) in Metfle, 2021 peasants (orcs), $889.
// ------------------------------------------------------------
//   The weather was clear last month; it will be clear next month.
//   Wages: $12.2 (Max: $370).
//   Wanted: none.
//   For Sale: 80 orcs [ORC] at $39, 16 leaders [LEAD] at $683.
//   Entertainment available: $53.
//   Products: 28 livestock [LIVE], 38 iron [IRON], 11 stone [STON], 6
//     mithril [MITH].

// Exits:
//   North : ocean (10,0) in Atlantis Ocean.
//   Northeast : ocean (11,1) in Atlantis Ocean.
//   Southeast : mountain (11,3) in Metfle.
//   South : mountain (10,4) in Metfle.
//   Southwest : ocean (9,3) in Atlantis Ocean.
//   Northwest : ocean (9,1) in Atlantis Ocean.

// - p iron (5220), Anuii (44), avoiding, behind, revealing faction,
//   sharing, weightless battle spoils, 6 humans [MAN], 6 picks [PICK],
//   52 iron [IRON]. Weight: 326. Capacity: 0/0/90/0. Skills: mining
//   [MINI] 3 (260), riding [RIDI] 1 (30).
// - p mith (5561), Anuii (44), avoiding, behind, revealing faction,
//   weightless battle spoils, 2 humans [MAN], 12 mithril [MITH]. Weight:
//   140. Capacity: 0/0/30/0. Skills: mining [MINI] 3 (260), riding
//   [RIDI] 1 (30).
// - p weap-quar (7637), Anuii (44), avoiding, behind, revealing faction,
//   weightless battle spoils, 12 orcs [ORC], 16 hammers [HAMM], 11 stone
//   [STON], 1028 silver [SILV]. Weight: 686. Capacity: 0/0/180/0.
//   Skills: weaponsmith [WEAP] 2 (90), quarrying [QUAR] 1 (40).
// - miner (2898), Anuii (44), avoiding, behind, revealing faction,
//   weightless battle spoils, 2 humans [MAN], 2 picks [PICK], 20 iron
//   [IRON]. Weight: 122. Capacity: 0/0/30/0. Skills: mining [MINI] 4
//   (300), riding [RIDI] 1 (30).
// - Jester (5626), The Lizards (20), avoiding, behind, orc [ORC].
// - Lizardmen (6656), The Lizards (20), avoiding, behind, lizardman
//   [LIZA].
// - taxmen (5923), Anuii (44), revealing faction, holding, 8 wood elves
//   [WELF], 378 silver [SILV]. Weight: 80. Capacity: 0/0/120/0. Skills:
//   combat [COMB] 1 (30).
// - scout (2907), on guard, Anuii (44), behind, revealing faction,
//   holding, weightless battle spoils, centaur [CTAU], 48 silver [SILV].
//   Weight: 50. Capacity: 0/70/70/0. Skills: riding [RIDI] 1 (55).
// - scout (4611), Anuii (44), avoiding, behind, revealing faction,
//   weightless battle spoils, orc [ORC], 2 silver [SILV]. Weight: 10.
//   Capacity: 0/0/15/0. Skills: none.

// + Shaft [1] : Shaft, contains an inner location.

// + Fleet [177] : Cog; Load: 20/500; Sailors: 6/6; MaxSpeed: 4.
//   - captain cog 177 (6792), Anuii (44), avoiding, behind, revealing
//     faction, weightless battle spoils, 2 lizardmen [LIZA], 980 silver
//     [SILV]. Weight: 20. Capacity: 0/0/30/30. Skills: sailing [SAIL] 3
//     (205).


// swamp (40,2) in Lihes, contains Panedyl [village], 5835 peasants
//   (lizardmen), $1750.
// ------------------------------------------------------------
//   The weather was clear last month; it will be clear next month.
//   Wages: $11.5 (Max: $350).
//   Wanted: 122 grain [GRAI] at $18, 114 livestock [LIVE] at $23, 129
//     fish [FISH] at $21.
//   For Sale: 233 lizardmen [LIZA] at $36, 46 leaders [LEAD] at $644.
//   Entertainment available: $145.
//   Products: 17 livestock [LIVE], 16 wood [WOOD], 19 herbs [HERB].

// Exits:
//   North : swamp (40,0) in Lihes.
//   Northeast : swamp (41,1) in Lihes.
//   Southeast : swamp (41,3) in Lihes.
//   South : swamp (40,4) in Lihes.
//   Southwest : swamp (39,3) in Lihes.
//   Northwest : swamp (39,1) in Lihes.

// - Marines (1416), Noizy Tribe (49), 17 lizardmen [LIZA], 3 spears
//   [SPEA], 17 livestock [LIVE].
// - City Guard (2966), on guard, The Guardsmen (1), 40 leaders [LEAD],
//   40 swords [SWOR].
// - Weaponsmiths (2370), Noizy Tribe (49), avoiding, behind, 3 goblins
//   [GBLN], 2 lizardmen [LIZA], 2 axes [AXE], 62 crossbows [XBOW].
// - Scout (2258), Silver Hand (27), avoiding, behind, goblin [GBLN].
// - choppers (6852), Noizy Tribe (49), avoiding, behind, 7 lizardmen
//   [LIZA].
// - Ambassador Whereitallbegan (4290), Disasters Inc (43), avoiding,
//   behind, revealing faction, lizardman [LIZA], 6 silver [SILV].
//   Weight: 10. Capacity: 0/0/15/15. Skills: none.
// - ranger (8928), Noizy Tribe (49), leader [LEAD].
// - scout (9371), Noizy Tribe (49), behind, 2 gnomes [GNOM], winged
//   horse [WING].
// ", 2)]
//         [InlineData(@"underforest (50,0,2 <underworld>) in Ryway, contains Sinsto [town],
//   10237 peasants (drow elves), $5937.
// ------------------------------------------------------------
//   The weather was clear last month; it will be clear next month.
//   Wages: $12.9 (Max: $1187).
//   Wanted: 167 grain [GRAI] at $20, 115 livestock [LIVE] at $20, 123
//     fish [FISH] at $27, 7 leather armor [LARM] at $69.
//   For Sale: 409 drow elves [DRLF] at $41, 81 leaders [LEAD] at $722.
//   Entertainment available: $399.
//   Products: 17 livestock [LIVE], 12 wood [WOOD], 15 stone [STON], 12
//     iron [IRON].

// Exits:
//   Southeast : underforest (51,1,2 <underworld>) in Ryway.
//   South : underforest (50,2,2 <underworld>) in Ryway.
//   Southwest : underforest (49,1,2 <underworld>) in Hawheci.

// - Unit (2465), Semigallians (18), avoiding, behind, under dwarf
//   [UDWA].
// - Scout (2070), Disasters Inc (43), avoiding, behind, revealing
//   faction, receiving no aid, high elf [HELF], horse [HORS], 40 silver
//   [SILV]. Weight: 60. Capacity: 0/70/85/0. Skills: riding [RIDI] 1
//   (60).
// - tactician (5246), Anuii (44), revealing faction, holding, weightless
//   battle spoils, drow elf [DRLF], 6 silver [SILV]. Weight: 10.
//   Capacity: 0/0/15/0. Skills: tactics [TACT] 2 (90).
// - taxmen (5600), Anuii (44), revealing faction, holding, sharing, 2
//   under dwarves [UDWA], 118 drow elves [DRLF], horse [HORS], 14241
//   silver [SILV]. Weight: 1250. Capacity: 0/70/1870/0. Skills: combat
//   [COMB] 1 (35).
// - Ambassador (5279), Noizy Tribe (49), avoiding, behind, orc [ORC],
//   horse [HORS].
// - Unit (9349), Anuii (44), avoiding, behind, revealing faction,
//   holding, weightless battle spoils, under dwarf [UDWA], 2 scrying
//   orbs [SORB], 9734 silver [SILV]. Weight: 10. Capacity: 0/0/15/0.
//   Skills: none.
// - b gemcutting (6366), Disasters Inc (43), avoiding, behind, revealing
//   faction, 10 under dwarves [UDWA], 5 horses [HORS], 300 silver
//   [SILV]. Weight: 350. Capacity: 0/350/500/0. Skills: gemcutting
//   [GCUT] 3 (180).

// + Shaft [1] : Shaft, contains an inner location.
//   - scout (4637), Anuii (44), avoiding, behind, revealing faction,
//     receiving no aid, sharing, weightless battle spoils, under dwarf
//     [UDWA], horse [HORS], 52 silver [SILV]. Weight: 60. Capacity:
//     0/70/85/0. Skills: none.


// underforest (52,0,2 <underworld>) in Ryway, 1872 peasants (drow
//   elves), $1385.
// ------------------------------------------------------------
//   The weather was clear last month; it will be clear next month.
//   Wages: $13.7 (Max: $581).
//   Wanted: none.
//   For Sale: 74 drow elves [DRLF] at $43, 14 leaders [LEAD] at $767.
//   Entertainment available: $71.
//   Products: 14 grain [GRAI], 10 wood [WOOD], 14 stone [STON], 19 iron
//     [IRON].

// Exits:
//   Southeast : underforest (53,1,2 <underworld>) in Ryway.
//   South : underforest (52,2,2 <underworld>) in Ryway.

// - stow guard (7622), Disasters Inc (43), avoiding, behind, revealing
//   faction, holding, receiving no aid, weightless battle spoils, under
//   dwarf [UDWA], 19 silver [SILV]. Weight: 10. Capacity: 0/0/15/0.
//   Skills: none.
// - Unit (9282), Disasters Inc (43), avoiding, behind, revealing
//   faction, sharing, hill dwarf [HDWA], winged horse [WING], 90 silver
//   [SILV]. Weight: 60. Capacity: 70/70/85/0. Skills: mining [MINI] 5
//   (450).

// underforest (54,0,2 <underworld>) in Ryway, 1618 peasants (humans),
//   $970.
// ------------------------------------------------------------
//   The weather was clear last month; it will be clear next month.
//   Wages: $13.0 (Max: $421).
//   Wanted: none.
//   For Sale: 64 humans [MAN] at $41, 12 leaders [LEAD] at $728.
//   Entertainment available: $49.
//   Products: 10 grain [GRAI], 13 wood [WOOD], 13 stone [STON], 18 iron
//     [IRON].

// Exits:
//   Southeast : underforest (55,1,2 <underworld>) in Ryway.
//   South : underforest (54,2,2 <underworld>) in Ryway, contains
//     Stowotpest [town].

// - trigger (4327), Anuii (44), avoiding, behind, revealing faction,
//   receiving no aid, weightless battle spoils, under dwarf [UDWA], 39
//   silver [SILV]. Weight: 10. Capacity: 0/0/15/0. Skills: none.
// - stow guard (7627), Disasters Inc (43), behind, revealing faction,
//   holding, receiving no aid, weightless battle spoils, under dwarf
//   [UDWA], 32 silver [SILV]. Weight: 10. Capacity: 0/0/15/0. Skills:
//   none.

// ", 3)]
//         public void canParseRegions(string input, int regionCount) {
//             var result = AtlantisParser.Regions.Parse(input);
//             (output.AssertParsed(result).Value?.Children?.Count ?? 0).Should().Be(regionCount);
//         }

//         [Theory]
//         [InlineData("Atlantis Engine Version: 5.2.0 (beta)\n")]
//         [InlineData("NewOrigins, Version: 1.0.1 (beta)\n")]
//         [InlineData("Faction Status:\n")]
//         [InlineData("Tax Regions: 0 (15)\n")]
//         [InlineData("Trade Regions: 0 (15)\n")]
//         [InlineData("Mages: 1 (2)\n")]
//         [InlineData("Apprentices: 0 (3)\n")]
//         [InlineData("Events during turn:\n")]
//         [InlineData("Emperor (456): Claims $50.\n")]
//         [InlineData("Emperor (456): Enters Gateway to forest [2].\n")]
//         [InlineData(@"Emperor (456): Walks from nexus (0,0,0 <nexus>) in The Void to forest
//   (50,22) in Mapa.
// ")]
//         [InlineData("Item reports:\n")]
//         [InlineData("silver [SILV], weight 0. This is the currency of Atlantis.\n")]
//         [InlineData("Declared Attitudes (default Neutral):\n")]
//         [InlineData("Hostile : none.\n")]
//         [InlineData("Unfriendly : Creatures (2).\n")]
//         [InlineData("Neutral : none.\n")]
//         [InlineData("Friendly : none.\n")]
//         [InlineData("Ally : none.\n")]
//         [InlineData("Unclaimed silver: 10000.\n")]
//         [InlineData("\n")]
//         public void canParseUnknownReportLine(string input) {
//             var result = AtlantisParser.ReportLine.Parse(input);
//             output.AssertParsed(result);
//         }

//         [Theory]
//         [InlineData("+ Fleet [128] : Longship; Load: 93/100; Sailors: 4/4; MaxSpeed: 4.\n")]
//         [InlineData("+ Fleet [128] : Longship; Load: 93/100; Sailors: 4/4; MaxSpeed:\n  4.\n")]
//         [InlineData("+ Fleet [128] : Longship; Load: 93/100; Sailors: 4/4;\n  MaxSpeed: 4.\n")]
//         [InlineData("+ Fleet [128] : Longship; Load: 93/100; Sailors:\n  4/4; MaxSpeed: 4.\n")]
//         [InlineData("+ Fleet [128] : Longship; Load: 93/100;\n  Sailors: 4/4; MaxSpeed: 4.\n")]
//         [InlineData("+ Fleet [128] : Longship; Load:\n  93/100; Sailors: 4/4; MaxSpeed: 4.\n")]
//         [InlineData("+ Fleet [128] : Longship;\n  Load: 93/100; Sailors: 4/4; MaxSpeed: 4.\n")]
//         [InlineData("+ Fleet [128] :\n  Longship; Load: 93/100; Sailors: 4/4; MaxSpeed: 4.\n")]
//         [InlineData("+ Fleet [128]\n  : Longship; Load: 93/100; Sailors: 4/4; MaxSpeed: 4.\n")]
//         [InlineData("+ Fleet\n  [128] : Longship; Load: 93/100; Sailors: 4/4; MaxSpeed: 4.\n")]
//         [InlineData("+ Fleet [121] : Fleet, 1 Longship, 1 Cog; Load: 580/600; Sailors:\n  10/10; MaxSpeed: 4.\n")]
//         [InlineData("+ Shaft [1] : Shaft, contains an inner location.\n")]
//         [InlineData("+ AE Sembury [165] : Cog; Load: 500/500; Sailors: 6/6; MaxSpeed: 4; Imperial Trade Fleet.\n")]
//         public void canParseStructure(string input) {
//             var result = AtlantisParser.Structure.Parse(input);
//             output.AssertParsed(result);
//         }

//         [Theory]
//         [InlineData(@"+ Fleet [128] : Longship; Load: 93/100; Sailors: 4/4; MaxSpeed: 4.
//   - captain longship 128 (3941), Anuii (44), avoiding, behind,
//     revealing faction, weightless battle spoils, 2 gnomes [GNOM], 83
//     crossbows [XBOW], 36 herbs [HERB]. Weight: 93. Capacity: 0/0/18/0.
//     Skills: sailing [SAIL] 2 (90).
// ", 1)]
//         [InlineData(@"+ Fleet [121] : Fleet, 1 Longship, 1 Cog; Load: 580/600; Sailors:
//   10/10; MaxSpeed: 4.
//   - captain fleet 121 (3940), Anuii (44), avoiding, behind, revealing
//     faction, weightless battle spoils, 2 gnomes [GNOM], 11 stone
//     [STON]. Weight: 560. Capacity: 0/0/18/0. Skills: sailing [SAIL] 2
//     (90).
//   - Unit (6795), Anuii (44), avoiding, behind, revealing faction,
//     weightless battle spoils, 2 lizardmen [LIZA]. Weight: 20.
//     Capacity: 0/0/30/30. Skills: sailing [SAIL] 3 (195).
// ", 2)]
//         [InlineData(@"+ Building [1] : Fort.
//   - Unit (469), Demon's Duchy (28), behind, leader [LEAD].
// ", 1)]
//         public void canPraseStructureWithUnits(string input, int units) {
//             var result = AtlantisParser.StructureWithUnits.Parse(input);
//             output.AssertParsed(result);

//             var structure = result.Value;
//             output.WriteLine(structure.ToString());

//             (structure.FirstByType("units")?.Children?.Count ?? 0).Should().Be(units);
//         }

//         [Theory]
//         [InlineData(@"  - Unit (6795), Anuii (44), avoiding, behind, revealing faction,
//     weightless battle spoils, 2 lizardmen [LIZA]. Weight: 20.
//     Capacity: 0/0/30/30. Skills: sailing [SAIL] 3 (195).
// ")]
//         public void canParseUnitWithlargerIdent(string input) {
//             var result = AtlantisParser.Unit(Parser.Char(' ').Repeat(2).IgnoreResult()).Parse(input);
//             output.AssertParsed(result);
//         }

//         [Theory]
//         [InlineData(@"  - captain fleet 121 (3940), Anuii (44), avoiding, behind, revealing
//     faction, weightless battle spoils, 2 gnomes [GNOM], 11 stone
//     [STON]. Weight: 560. Capacity: 0/0/18/0. Skills: sailing [SAIL] 2
//     (90).
//   - Unit (6795), Anuii (44), avoiding, behind, revealing faction,
//     weightless battle spoils, 2 lizardmen [LIZA]. Weight: 20.
//     Capacity: 0/0/30/30. Skills: sailing [SAIL] 3 (195).
// ", 2)]
//         public void canParseMultipleUnitsWithlargerIdent(string input, int unitCount) {
//             var result = AtlantisParser.Units(Parser.Char(' ').Repeat(2).IgnoreResult()).Parse(input);
//             output.AssertParsed(result).Value?.Children?.Count.Should().Be(unitCount);
//         }

//         [Theory]
//         [InlineData("avoiding, behind, revealing faction, holding, receiving no aid, won't cross water, wood elf [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing faction, holding, receiving no aid, won't cross water, wood elf [WELF], horse\n  [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing faction, holding, receiving no aid, won't cross water, wood elf [WELF],\n  horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing faction, holding, receiving no aid, won't cross water, wood elf\n  [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing faction, holding, receiving no aid, won't cross water, wood\n  elf [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing faction, holding, receiving no aid, won't cross water,\n  wood elf [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing faction, holding, receiving no aid, won't cross\n  water, wood elf [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing faction, holding, receiving no aid, won't\n  cross water, wood elf [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing faction, holding, receiving no aid,\n  won't cross water, wood elf [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing faction, holding, receiving no\n  aid, won't cross water, wood elf [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing faction, holding, receiving\n  no aid, won't cross water, wood elf [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing faction, holding,\n  receiving no aid, won't cross water, wood elf [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing faction,\n  holding, receiving no aid, won't cross water, wood elf [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind, revealing\n  faction, holding, receiving no aid, won't cross water, wood elf [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding, behind,\n  revealing faction, holding, receiving no aid, won't cross water, wood elf [WELF], horse [HORS].", 6, 2)]
//         [InlineData("avoiding,\n  behind, revealing faction, holding, receiving no aid, won't cross water, wood elf [WELF], horse [HORS].", 6, 2)]
//         public void canParseUnitFlagsAndSkills(string input, int falgsCount, int itemCount) {
//             var result = AtlantisParser.UnitFlagsAndItems.Parse(input);
//             output.AssertParsed(result);

//             var (flags, items) = result.Value;
//             (flags?.Children?.Count ?? 0).Should().Be(falgsCount);
//             (items?.Children?.Count ?? 0).Should().Be(itemCount);
//         }

//         public static IEnumerable<string> AddLineBreaks(string input, string lineBreak = "\n  ") {
//             var words = input.Split(" ", StringSplitOptions.RemoveEmptyEntries);

//             yield return input;

//             for (var i = 1; i < words.Length; i++) {
// ;                yield return string.Join(" ", words.Take(i)) + lineBreak + string.Join(" ", words.Skip(i));
//             }
//         }

//         public static IEnumerable<object[]> CanParaseUnitAttributesCases {
//             get {
//                 var cases = new[] {
//                     (". Weight: 60.\n", new[] { "weight" }),
//                     (". Capacity: 0/70/85/0.\n", new[] { "capacity" }),
//                     (". Capacity: 0/70/85/0;", new[] { "capacity" }),
//                     (". Weight: 60. Capacity: 0/70/85/0.\n", new[] { "weight", "capacity" }),
//                     (". Weight: 60. Capacity: 0/70/85/0;", new[] { "weight", "capacity" }),
//                     (". Skills: combat [COMB] 1 (30), stealth [STEA] 1 (30), riding [RIDI] 1 (65).\n", new[] { "skills" }),
//                     (". Skills: combat [COMB] 1 (30), stealth [STEA] 1 (30), riding [RIDI] 1 (65);", new[] { "skills" }),
//                     (". Weight: 60. Capacity: 0/70/85/0. Skills: combat [COMB] 1 (30), stealth [STEA] 1 (30), riding [RIDI] 1 (65).\n", new[] { "weight", "capacity", "skills" }),
//                     (". Weight: 60. Capacity: 0/70/85/0. Skills: combat [COMB] 1 (30), stealth [STEA] 1 (30), riding [RIDI] 1 (65);", new[] { "weight", "capacity", "skills" }),
//                     (". Can Study: fire [FIRE], earthquake [EQUA], force shield [FSHI], energy shield [ESHI], spirit shield [SSHI], magical healing [MHEA], farsight [FARS], mind reading [MIND], weather lore [WEAT], earth lore [EART], necromancy [NECR], demon lore [DEMO], illusion [ILLU], artifact lore [ARTI].\n", new[] { "can-study" }),
//                     (". Can Study: fire [FIRE], earthquake [EQUA], force shield [FSHI], energy shield [ESHI], spirit shield [SSHI], magical healing [MHEA], farsight [FARS], mind reading [MIND], weather lore [WEAT], earth lore [EART], necromancy [NECR], demon lore [DEMO], illusion [ILLU], artifact lore [ARTI];", new[] { "can-study" })
//                 };

//                 foreach (var (s, p1) in cases) {
//                     foreach (var input in AddLineBreaks(s)) {
//                         yield return new object[] { input, p1 };
//                     }
//                 }
//             }
//         }

//         [Fact]
//         public void canParseCanStudyUnitAttribute() {
//             string input = ". Can Study: fire [FIRE], earthquake [EQUA], force shield [FSHI], energy shield [ESHI], spirit shield [SSHI], magical healing [MHEA], farsight [FARS], mind reading [MIND], weather lore [WEAT], earth lore [EART], necromancy [NECR], demon lore [DEMO], illusion [ILLU], artifact lore [ARTI].\n";
//             var attribute = output.AssertParsed(AtlantisParser.UnitAttribute.Parse(input)).Value;

//             attribute.StrValueOf("key").Should().Be("can-study");
//             (attribute.FirstByType("value")?.Children?.Count ?? 0).Should().Be(14);
//         }

//         [Theory]
//         [MemberData(nameof(CanParaseUnitAttributesCases))]
//         public void canParseUnitAttributes(string input, string[] attributeNames) {
//             var result = AtlantisParser.UnitAttributes().Parse(input);
//             var attributes = output.AssertParsed(result).Value.ToArray();

//             attributes.Length.Should().Be(attributeNames.Length);

//             attributes.Select(x => x.StrValueOf("key")).Should().BeEquivalentTo(attributeNames);
//         }

//         [Theory]
//         [InlineData(@"* Emperor (456), Avalon Empire (15), avoiding, behind, holding, won't
//   cross water, leader [LEAD]. Weight: 10. Capacity: 0/0/15/0. Skills:
//   force [FORC] 1 (30), pattern [PATT] 1 (30), spirit [SPIR] 1 (30),
//   gate lore [GATE] 1 (30), combat [COMB] 3 (180), endurance [ENDU] 3
//   (180). Can Study: fire [FIRE], earthquake [EQUA], force shield
//   [FSHI], energy shield [ESHI], spirit shield [SSHI], magical healing
//   [MHEA], farsight [FARS], mind reading [MIND], weather lore [WEAT],
//   earth lore [EART], necromancy [NECR], demon lore [DEMO], illusion
//   [ILLU], artifact lore [ARTI].
// ")]
//         [InlineData(@"* Unit m2 (2530), Avalon Empire (15), avoiding, behind, revealing
//   faction, holding, receiving no aid, won't cross water, wood elf
//   [WELF], horse [HORS]. Weight: 60. Capacity: 0/70/85/0. Skills:
//   combat [COMB] 1 (30), stealth [STEA] 1 (30), riding [RIDI] 1 (65).
// ")]
//         [InlineData(@"- p shipbuilders (2626), Disasters Inc (43), avoiding, behind,
//   revealing faction, consuming faction's food, 9 high elves [HELF], 4
//   horses [HORS]. Weight: 290. Capacity: 0/280/415/0. Skills:
//   shipbuilding [SHIP] 4 (368).
// ")]
//         public void canParserUnit(string input) {
//             var result = AtlantisParser.Unit().Before(Parser<char>.End).Parse(input);
//             output.AssertParsed(result);
//         }

//         [Fact]
//         public void canParseSpecificUnit1() {
//             string input = @"- p shipbuilders (2626), Disasters Inc (43), avoiding, behind,
//   revealing faction, consuming faction's food, 9 high elves [HELF], 4
//   horses [HORS]. Weight: 290. Capacity: 0/280/415/0. Skills:
//   shipbuilding [SHIP] 4 (368).
// ";
//             var unit = output.AssertParsed(AtlantisParser.Unit().Before(Parser<char>.End).Parse(input)).Value;

//             output.WriteLine(unit.ToString());

//             unit.StrValueOf("name").Should().Be("p shipbuilders");
//             (unit.IntValueOf("number") ?? 0).Should().Be(2626);
//             unit.FirstByType("faction").StrValueOf("name").Should().Be("Disasters Inc");
//             (unit.FirstByType("faction").IntValueOf("number") ?? 0).Should().Be(43);

//             var flags = unit.FirstByType("flags").Children;
//             flags.Count.Should().Be(4);
//             flags.Select(x => (x as ValueReportNode<string>).Value).Should().BeEquivalentTo(
//                 "avoiding", "behind", "revealing faction", "consuming faction's food"
//             );

//             var items = unit.FirstByType("items").Children;
//             items.Count.Should().Be(2);

//             (items[0].IntValueOf("amount") ?? 0).Should().Be(9);
//             items[0].StrValueOf("name").Should().Be("high elves");
//             items[0].StrValueOf("code").Should().Be("HELF");

//             (items[1].IntValueOf("amount") ?? 0).Should().Be(4);
//             items[1].StrValueOf("name").Should().Be("horses");
//             items[1].StrValueOf("code").Should().Be("HORS");

//             unit.FirstByType("weight").Should().NotBeNull();
//             unit.FirstByType("capacity").Should().NotBeNull();
//             unit.FirstByType("skills").Should().NotBeNull();
//         }

//         [Fact]
//         public void canParseSpecificUnit2() {
//             string input = @"- p shipbuilders (1702), Disasters Inc (43), avoiding, behind,
//   revealing faction, consuming faction's food, 9 high elves [HELF],
//   unfinished Galleon [GALL] (needs 54). Weight: 90. Capacity:
//   0/0/135/0. Skills: shipbuilding [SHIP] 4 (357).
// ";
//             var unit = output.AssertParsed(AtlantisParser.Unit().Before(Parser<char>.End).Parse(input)).Value;

//             output.WriteLine(unit.ToString());

//             unit.StrValueOf("name").Should().Be("p shipbuilders");
//             (unit.IntValueOf("number") ?? 0).Should().Be(1702);
//             unit.FirstByType("faction").StrValueOf("name").Should().Be("Disasters Inc");
//             (unit.FirstByType("faction").IntValueOf("number") ?? 0).Should().Be(43);

//             var flags = unit.FirstByType("flags").Children;
//             flags.Count.Should().Be(4);
//             flags.Select(x => (x as ValueReportNode<string>).Value).Should().BeEquivalentTo(
//                 "avoiding", "behind", "revealing faction", "consuming faction's food"
//             );

//             var items = unit.FirstByType("items").Children;
//             items.Count.Should().Be(2);

//             (items[0].IntValueOf("amount") ?? 0).Should().Be(9);
//             items[0].StrValueOf("name").Should().Be("high elves");
//             items[0].StrValueOf("code").Should().Be("HELF");

//             (items[1].IntValueOf("amount") ?? 0).Should().Be(1);
//             items[1].StrValueOf("name").Should().Be("unfinished Galleon");
//             items[1].StrValueOf("code").Should().Be("GALL");
//             items[1].IntValueOf("needs").Should().Be(54);

//             unit.FirstByType("weight").Should().NotBeNull();
//             unit.FirstByType("capacity").Should().NotBeNull();
//             unit.FirstByType("skills").Should().NotBeNull();
//         }

//         [Fact]
//         public void canParseSpecificUnit3() {
//             string input = @"- trans Chawi-SWforest (5894), Disasters Inc (43), avoiding, behind,
//   revealing faction, sharing, human [MAN], 5 horses [HORS], 1050
//   silver [SILV]. Weight: 260. Capacity: 0/350/365/0. Skills: none.
// ";
//             var unit = output.AssertParsed(AtlantisParser.Unit().Before(Parser<char>.End).Parse(input)).Value;

//             output.WriteLine(unit.ToString());

//             unit.StrValueOf("name").Should().Be("trans Chawi-SWforest");
//             (unit.IntValueOf("number") ?? 0).Should().Be(5894);
//             unit.FirstByType("faction").StrValueOf("name").Should().Be("Disasters Inc");
//             (unit.FirstByType("faction").IntValueOf("number") ?? 0).Should().Be(43);

//             var flags = unit.FirstByType("flags").Children;
//             flags.Count.Should().Be(4);
//             flags.Select(x => (x as ValueReportNode<string>).Value).Should().BeEquivalentTo(
//                 "avoiding", "behind", "revealing faction", "sharing"
//             );

//             var items = unit.FirstByType("items").Children;
//             items.Count.Should().Be(3);

//             (items[0].IntValueOf("amount") ?? 0).Should().Be(1);
//             items[0].StrValueOf("name").Should().Be("human");
//             items[0].StrValueOf("code").Should().Be("MAN");

//             (items[1].IntValueOf("amount") ?? 0).Should().Be(5);
//             items[1].StrValueOf("name").Should().Be("horses");
//             items[1].StrValueOf("code").Should().Be("HORS");

//             (items[2].IntValueOf("amount") ?? 0).Should().Be(1050);
//             items[2].StrValueOf("name").Should().Be("silver");
//             items[2].StrValueOf("code").Should().Be("SILV");

//             unit.FirstByType("weight").Should().NotBeNull();
//             unit.FirstByType("capacity").Should().NotBeNull();
//             unit.FirstByType("skills").Should().NotBeNull();
//         }

//         [Fact]
//         public void canParseSpecificUnit4() {
//             string input = @"- kurjers (7601), Semigallians (18), avoiding, behind, human [MAN],
//   horse [HORS].
// ";
//             var unit = output.AssertParsed(AtlantisParser.Unit().Before(Parser<char>.End).Parse(input)).Value;

//             output.WriteLine(unit.ToString());

//             unit.StrValueOf("name").Should().Be("kurjers");
//             (unit.IntValueOf("number") ?? 0).Should().Be(7601);
//             unit.FirstByType("faction").StrValueOf("name").Should().Be("Semigallians");
//             (unit.FirstByType("faction").IntValueOf("number") ?? 0).Should().Be(18);

//             var flags = unit.FirstByType("flags").Children;
//             flags.Count.Should().Be(2);
//             flags.Select(x => (x as ValueReportNode<string>).Value).Should().BeEquivalentTo(
//                 "avoiding", "behind"
//             );

//             var items = unit.FirstByType("items").Children;
//             items.Count.Should().Be(2);

//             (items[0].IntValueOf("amount") ?? 0).Should().Be(1);
//             items[0].StrValueOf("name").Should().Be("human");
//             items[0].StrValueOf("code").Should().Be("MAN");

//             (items[1].IntValueOf("amount") ?? 0).Should().Be(1);
//             items[1].StrValueOf("name").Should().Be("horse");
//             items[1].StrValueOf("code").Should().Be("HORS");
//         }

//         [Theory]
//         [InlineData(@"- Dwarven masons (3860), 3 hill dwarves [HDWA], 3 picks [PICK], 41
//   stone [STON]; They are dusty and brawny..
// ", "They are dusty and brawny.")]
//         [InlineData(@"- Herdsmen of the Ungre Guild (1767), 3 humans [MAN], 3 spices [SPIC],
//   2 gems [GEM], net [NET], 2 mink [MINK], 3 livestock [LIVE]; Content
//   looking shepherds and herdsmen.
// ", "Content looking shepherds and herdsmen")]
//         [InlineData(@"- Burvis al Pirmais ibn Beigtais (551), leader [LEAD], skeleton
//   [SKEL]; Rise my minions....
// ", "Rise my minions...")]
//         public void canParseUnitWithDescription(string input, string description) {
//             var result = AtlantisParser.Unit().Parse(input);
//             var unit = output.AssertParsed(result).Value;

//             unit.StrValueOf("description").Should().Be(description);
//         }

//         [Theory]
//         [InlineData(".\n", true)]
//         [InlineData(".\n  foo", false)]
//         [InlineData(".\nfoo", true)]
//         public void canParseUnitTerminator(string input, bool success) {
//             var result = AtlantisParser.UnitTerminator().Parse(input);
//             result.Success.Should().Be(success);
//         }

//         [Fact]
//         public void canParserUnits() {
//             var result = AtlantisParser.Units().Parse(@"* Unit m2 (2530), Avalon Empire (15), avoiding, behind, revealing
//   faction, holding, receiving no aid, won't cross water, wood elf
//   [WELF], horse [HORS]. Weight: 60. Capacity: 0/70/85/0. Skills:
//   combat [COMB] 1 (30), stealth [STEA] 1 (30), riding [RIDI] 1 (65).
// - Aquatic Scout (3427), Disasters Inc (43), avoiding, behind,
//   revealing faction, lizardman [LIZA]. Weight: 10. Capacity:
//   0/0/15/15. Skills: none.
// - Scout (2070), Disasters Inc (43), avoiding, behind, revealing
//   faction, receiving no aid, high elf [HELF], horse [HORS], 40 silver
//   [SILV]. Weight: 60. Capacity: 0/70/85/0. Skills: riding [RIDI] 1
//   (60).
// ");
//             output.AssertParsed(result);

//             output.WriteLine(result.Value.ToString());
//         }

//         [Theory]
//         [InlineData("foo, bar, wood elf [WELF]", 2)]
//         [InlineData("foo, bar, 10 orcs [ORC]", 2)]
//         [InlineData("avoiding, behind, revealing faction, holding, receiving no aid, won't cross water, 3 swords [SWOR]", 6)]
//         public void canParseUnitFlags(string input, int count) {
//             var result = AtlantisParser.UnitFlags.Parse(input);
//             output.AssertParsed(result);

//             result.Value.Children.Count.Should().Be(count);
//         }
}
