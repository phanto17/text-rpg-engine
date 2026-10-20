using RpgEngine.Core.Characters;
using RpgEngine.Core.Content;
using RpgEngine.Core.Engine;
using RpgEngine.Core.World;
using RpgEngine.Tests.Content;
using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Playthrough;

/// <summary>
/// Act III, the Cradle of the First Architect, ends at the Great Brazier: the player relights the
/// Sun, tunes the Moon, or completes the Triad, and the epilogue for that choice is shaped by the
/// whole journey.
/// </summary>
public sealed class ActThreeScenarioTests
{
    private static readonly GameContent Content = ContentLoader.LoadFromDirectory(RealDataTests.DataDirectory);

    private static PlayerCharacter Create(string classId, string backgroundId, string[] skills, string[] expertise)
    {
        var cls = Content.Classes[classId];
        var scores = CharacterFactory.AssignByPriority(Content.Rules.StandardArray, cls);
        return new CharacterFactory(Content).Create("Tamsin", classId, backgroundId, scores, skills, expertise);
    }

    [Theory]
    [InlineData("ending_triad_passage", true, true, true, true, true, 9, "The Great Vesper Bell rings once above the Cradle")]
    [InlineData("ending_triad_passage", true, false, true, true, true, 9, "\"Last the face all faces yield,\" you say")]
    [InlineData("ending_triad_passage", false, true, true, true, true, 9, "The Rite of True Passage was meant to be spoken by the one who broke it.")]
    [InlineData("ending_sun_stasis", true, false, false, false, false, -3, "White fire climbs the roots of the mountain")]
    [InlineData("ending_sun_stasis", false, false, false, true, false, -1, "White fire climbs the roots of the mountain")]
    [InlineData("ending_sun_stasis", false, false, false, false, true, 3, "White fire climbs the roots of the mountain")]
    [InlineData("ending_sun_stasis", false, false, false, false, true, 2, "You lock the Sun Seal into the grate")]
    [InlineData("ending_moon_vigil", false, false, true, false, false, -2, "Silver-blue light runs up through the roots of the mountain")]
    [InlineData("ending_moon_vigil", false, false, false, false, true, -2, "Silver-blue light runs up through the roots of the mountain")]
    [InlineData("ending_moon_vigil", true, true, false, true, false, 4, "You set the Moon Seal into the resonator")]
    [InlineData("", true, true, true, true, true, 9, "The Great Brazier falls quiet behind you")]
    public void Epilogue_FollowsTheChoiceAtTheBrazier_AndTheJourneyThere(
        string ending, bool architectSpared, bool wrenFollowed, bool vaelenSpared, bool silasSpared, bool beaconRelit, int resonance, string expectedStart)
    {
        var world = new TestWorld(Content, Create("oath_eater", "moor_bailiff", ["athletics", "intimidation"], []));
        string[] flags =
        [
            ending,
            architectSpared ? "architect_reconciled" : "architect_destroyed",
            wrenFollowed ? "wren_joins_descent" : "",
            vaelenSpared ? "vaelen_reconciled" : "vaelen_destroyed",
            silasSpared ? "silas_relieved" : "silas_destroyed",
            beaconRelit ? "beacon_restored" : "",
        ];
        foreach (var flag in flags.Where(f => f.Length > 0))
        {
            world.World.SetFlag(flag);
        }

        world.World.SetCounter("lantern_resonance", resonance);

        var epilogue = Content.Manifest.Epilogues.First(e => ConditionEvaluator.AllPass(e.Conditions, world.State));

        Assert.StartsWith(expectedStart, epilogue.Text, StringComparison.Ordinal);
    }
}
