namespace advisor.facts;

using System.IO;
using System.Linq;
using advisor.TurnProcessing;

public class FactionRecordSpec {
    [Fact]
    public void FactionRecord_InitializesCorrectly()
    {
        var faction = new FactionRecord(1);
        faction.Number.Should().Be(1);
        faction.Props.Should().BeEmpty();
    }

    [Fact]
    public void FactionRecord_AddsPropsCorrectly()
    {
        var faction = new FactionRecord();
        faction.Add("TestProp", "TestValue");
        faction.Props.Should().Contain(("TestProp", "TestValue"));
    }

    [Fact]
    public void FactionRecord_HasPropReturnsCorrectly()
    {
        var faction = new FactionRecord();
        faction.Add("TestProp", "TestValue");
        faction.HasProp("TestProp").Should().BeTrue();
        faction.HasProp("NonExistentProp").Should().BeFalse();
    }

    [Fact]
    public void FactionRecord_SetFlagWorksCorrectly()
    {
        var faction = new FactionRecord();
        faction.SetFlag("TestFlag");
        faction.HasProp("TestFlag").Should().BeTrue();
    }

    [Fact]
    public void FactionRecord_ClearFlagWorksCorrectly()
    {
        var faction = new FactionRecord();
        faction.SetFlag("TestFlag");
        faction.ClearFlag("TestFlag");
        faction.HasProp("TestFlag").Should().BeFalse();
    }

    [Fact]
    public void FactionRecord_ToggleFlagWorksCorrectly()
    {
        var faction = new FactionRecord();
        faction.TogglFlag("TestFlag", true);
        faction.HasProp("TestFlag").Should().BeTrue();
        faction.TogglFlag("TestFlag", false);
        faction.HasProp("TestFlag").Should().BeFalse();
    }

    [Fact]
    public void FactionRecord_GetStrReturnsCorrectly()
    {
        var faction = new FactionRecord();
        faction.Add("TestProp", "TestValue");
        faction.GetStr("TestProp").Should().Be("TestValue");
        faction.GetStr("NonExistentProp").Should().BeNull();
    }

    [Fact]
    public void FactionRecord_GetAllStrReturnsCorrectly()
    {
        var faction = new FactionRecord();
        faction.Add("TestProp", "TestValue1");
        faction.Add("TestProp", "TestValue2");
        var result = faction.GetAllStr("TestProp");
        result.Should().BeEquivalentTo(new List<string> { "TestValue1", "TestValue2" });
    }

    [Fact]
    public void FactionRecord_SetStrWorksCorrectly()
    {
        var faction = new FactionRecord();
        faction.SetStr("TestProp", "TestValue");
        faction.GetStr("TestProp").Should().Be("TestValue");
    }

    [Fact]
    public void FactionRecord_GetIntReturnsCorrectly()
    {
        var faction = new FactionRecord();
        faction.SetStr("TestProp", "123");
        faction.GetInt("TestProp").Should().Be(123);
        faction.GetInt("NonExistentProp").Should().BeNull();
    }

    [Fact]
    public void FactionRecord_GetAllIntReturnsCorrectly()
    {
        var faction = new FactionRecord();
        faction.SetStr("TestProp", "123", true);
        faction.SetStr("TestProp", "456", true);
        var result = faction.GetAllInt("TestProp");
        result.Should().BeEquivalentTo(new List<int> { 123, 456 });
    }

    [Fact]
    public void FactionRecord_SetIntWorksCorrectly()
    {
        var faction = new FactionRecord();
        faction.SetInt("TestProp", 123);
        faction.GetInt("TestProp").Should().Be(123);
    }
}
