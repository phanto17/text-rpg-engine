using RpgEngine.Core.Content;
using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Tests.Content;

/// <summary>Validates the shipped game content in <c>Data/</c> (copied next to the test assembly).</summary>
public sealed class RealDataTests
{
    public static string DataDirectory => Path.Combine(AppContext.BaseDirectory, "Data");

    private static readonly Lazy<GameContent> Content = new(() => ContentLoader.LoadFromDirectory(DataDirectory));

    [Fact]
    public void ShippedContent_LoadsWithoutErrors()
    {
        var content = Content.Value;

        Assert.Equal("Barrowlight", content.Manifest.Title);
        Assert.Equal("lichgate", content.Manifest.StartRoom);
    }

    [Fact]
    public void ShippedContent_HasTheMilestoneOneSlice()
    {
        var content = Content.Value;

        Assert.Equal(new[] { "fighter", "rogue", "wizard" }, content.ClassList.Select(c => c.Id));
        Assert.Equal(18, content.Skills.Count);
        Assert.Equal(new[] { "inner_crypt", "lichgate", "ossuary_hall" }, content.Rooms.Keys.Order(StringComparer.Ordinal));
        Assert.True(content.Dialogues.ContainsKey("wren"));
        Assert.Equal("inner_crypt", content.EncounterRooms["barrow_heart"]);
        Assert.Equal(2, content.Encounters["barrow_heart"].Parley!.Options.Count);
    }

    [Fact]
    public void WrenDialogue_HasSkillChecksWithPermanentLocks()
    {
        var hub = Content.Value.Dialogues["wren"].Nodes.Single(n => n.Id == "hub");

        var checks = hub.Choices.Where(c => c.Check is not null).ToList();

        Assert.Equal(new[] { "insight", "persuasion", "intimidation" }, checks.Select(c => c.Check!.Skill));
        Assert.All(checks, check => Assert.False(check.Retry));
        Assert.Equal(new[] { 10, 12, 12 }, checks.Select(c => c.Check!.Dc));
    }

    [Fact]
    public void WrensTrust_GivesHolyWater_ThatExploitsTheKnightsVulnerability()
    {
        var content = Content.Value;
        var trusts = content.Dialogues["wren"].Nodes.Single(n => n.Id == "trusts");

        Assert.Contains(trusts.Effects, e => e is GiveItemEffect { Item: "holy_water" });
        var use = content.Items["holy_water"].Use!;
        Assert.Equal(SpellTarget.Enemy, use.Target);
        var damageType = Assert.IsType<AutoHitResolutionDef>(use.Resolution).DamageType;
        Assert.Contains(damageType, content.Monsters["gravebound_knight"].Vulnerabilities);
    }

    [Fact]
    public void SigilDoor_IsAThreeStepSequencePuzzle()
    {
        var door = Content.Value.Rooms["ossuary_hall"].Features.Single(f => f.Id == "bronze_door");

        var presses = door.Interactions.Where(i => i.Verb == "press").Select(i => i.Target).Distinct();

        Assert.Equal(new[] { "sun", "moon", "skull" }, presses);
        Assert.Contains(door.Interactions, i => i.Effects.OfType<SetFlagEffect>().Any(e => e.Flag == "sigil_door_open"));
    }
}
