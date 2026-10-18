using RpgEngine.Tests.TestSupport;

namespace RpgEngine.Tests.Playthrough;

/// <summary>
/// Drives the real CLI end to end through scripted keyboard input with "lucky" dice (every roll is
/// a natural 20 or maximum damage), so story paths succeed regardless of later number tuning.
/// </summary>
public sealed class GoldenPathTests
{
    private static readonly string[] CreateRogue =
    [
        "1",            // title: new game
        "Tamsin",       // name
        "2",            // class: rogue
        "1",            // standard array
        "1",            // keep suggested scores
        "8 4 7 10",     // persuasion, insight, perception, stealth
        "3 4",          // expertise: persuasion, stealth
        "y",            // begin
    ];

    private static readonly string[] CreateFighter = ["1", "Bram", "1", "1", "1", "3 6", "y"];

    private static readonly string[] OpenTheDoor = ["down", "press sun", "press moon", "press skull", "east"];

    [Fact]
    public void PersuadeWren_SolvePuzzle_ParleyWithKnight_ReachesPeacefulEpilogue()
    {
        using var cli = new CliHarness();

        string[] inputs =
        [
            .. CreateRogue,
            "talk to wren",
            "4",                    // [Persuasion DC 12] I'm not here to rob the dead...
            "1",                    // I'll bring the light back.
            "search headstone",     // hidden potion (Perception DC 10)
            .. OpenTheDoor,
            "5",                    // combat: Parley
            "2",                    // [Religion DC 10] (advantage) Speak the Rite of Rest
            "take lantern",         // lights the lantern and opens the stair
            "descend stair",
            "2",                    // after the epilogue: quit
        ];

        var exitCode = cli.Run(inputs);
        var output = cli.Output;

        Assert.Equal(0, exitCode);
        Assert.Contains("Sister Wren", output);
        Assert.Contains("[Persuasion DC 12] I'm not here to rob the dead.", output);
        Assert.Contains("CRITICAL SUCCESS", output);
        Assert.Contains("Gods help me, I believe you.", output);
        Assert.Contains("+ Flask of Holy Water", output);
        Assert.Contains("+ Potion of Healing", output);
        Assert.Contains("the bronze door swings inward on hidden hinges", output);
        Assert.Contains("COMBAT", output);
        Assert.Contains("[Religion DC 10] (advantage) Speak the Rite of Rest", output);
        Assert.Contains("Words won where steel did not.", output);
        Assert.Contains("+ the Barrowlight", output);
        Assert.Contains("its waking flame has unsealed a hidden stair beneath the sarcophagus", output);
        Assert.Contains("Descend the spiral stair into the Under-Barrow", output);
        Assert.Contains("Its flame leans toward the depths like a compass needle pulling home", output);
        Assert.Contains("You carried the Barrowlight down through the unsealed threshold into the Under-Barrow.", output);
        Assert.Contains("Above you, Ser Halric rests at last on his own sarcophagus", output);
        Assert.Contains("THE END", output);
    }

    [Fact]
    public void WrensHolyWater_ExploitsTheKnightsRadiantVulnerability()
    {
        using var cli = new CliHarness();

        // Lucky dice: everyone rolls 20 for initiative, and the rogue's DEX acts first. The flask's
        // 2d4 rolls 8, doubled to 16 against Ser Halric's 15 HP. The thrall's critical claw leaves
        // the rogue on 2 HP, and the rogue's critical strike finishes it.
        string[] inputs =
        [
            .. CreateRogue,
            "talk to wren",
            "4",                    // [Persuasion DC 12] earns her trust and the flask
            "1",                    // I'll bring the light back.
            .. OpenTheDoor,
            "3",                    // combat: Use an item
            "2",                    // Flask of Holy Water (after the potions)
            "1",                    // target: Ser Halric
            "1",                    // round 2: attack the last thrall
            "take lantern",
            "descend",              // the only descend action here, so no target is needed
            "2",                    // after the epilogue: quit
        ];

        var exitCode = cli.Run(inputs);
        var output = cli.Output;

        Assert.Equal(0, exitCode);
        Assert.Contains("Potion of Healing x2", output);
        Assert.Contains("Flask of Holy Water x1", output);
        Assert.Contains("You use the Flask of Holy Water on Ser Halric.", output);
        Assert.Contains("Ser Halric takes 16 radiant damage (vulnerable!)", output);
        Assert.Contains("Ser Halric falls!", output);
        Assert.Contains("VICTORY", output);
        Assert.Contains("the Barrowlight's flame settles into a thin, ash-pale glow", output);
        Assert.Contains("Ser Halric's shattered armor lies scattered in the ash behind you.", output);
    }

    [Fact]
    public void FalteringFighter_DiesToTheBarrowHeart_AndCanQuitFromGameOver()
    {
        using var cli = new CliHarness();

        string[] inputs =
        [
            .. CreateFighter,
            .. OpenTheDoor,
            "4",    // Defend; lucky dice make the undead crit, and the thrall finishes the job
            "2",    // game over menu (no saves): quit
        ];

        var exitCode = cli.Run(inputs);

        Assert.Equal(0, exitCode);
        Assert.Contains("You have died", cli.Output);
        Assert.Contains("The cold of the barrow closes over you", cli.Output);
    }

    [Fact]
    public void WizardBacksOutOfSpellMenu_ThenFlees()
    {
        using var cli = new CliHarness();

        string[] inputs =
        [
            "1", "Ilse", "3", "1", "1", "6 1", "y",
            .. OpenTheDoor,
            "2",        // Spells and techniques
            "0",        // back
            "6",        // Flee [Acrobatics DC 10]
            "quit", "y",
            "2",        // title: quit
        ];

        var exitCode = cli.Run(inputs);

        Assert.Equal(0, exitCode);
        Assert.Contains("Magic Missile (2 MP)", cli.Output);
        Assert.Contains("You got away.", cli.Output);
        Assert.Contains("The Ossuary Hall", cli.Output);
    }

    [Fact]
    public void SaveQuitAndContinue_RestoresTheGame()
    {
        using var cli = new CliHarness();

        string[] inputs =
        [
            .. CreateFighter,
            "stats", "inventory", "journal", "help", "look", "examine wren", "99", "",
            "save test-slot",
            "quit", "n",
            "quit", "y",
            "2",            // title: continue
            "1",            // the only save
            "load", "0",    // open the load menu, then back out
            "quit", "y",
            "3",            // title: quit
        ];

        var exitCode = cli.Run(inputs);
        var output = cli.Output;

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(Path.Combine(cli.SavesDirectory, "test-slot.json")));
        Assert.Contains("Game saved to slot 'test-slot'.", output);
        Assert.Contains("Loaded 'test-slot'.", output);
        Assert.Contains("replay with --seed 7", output);
        Assert.Contains("Chain Mail", output);
        Assert.Contains("Commands", output);
        Assert.Contains("The Barrowlight lies somewhere inside the barrow", output);
        Assert.Contains("A woman of perhaps forty", output);
        Assert.Contains("There is no option 99.", output);
        Assert.Contains("Continue a saved game", output);
    }

    [Fact]
    public void RolledScores_ManualAssignment_AndInvalidInputsAreRejected()
    {
        using var cli = new CliHarness();

        string[] inputs =
        [
            "1",
            "",                         // empty name is rejected
            "Wren's Cousin",
            "bard",                     // unknown class name is ignored
            "Rogue",                    // class by name
            "2",                        // roll 4d6 drop lowest (lucky: all 18s)
            "2",                        // assign manually
            "1", "1", "1", "1", "1", "1",
            "1 1 2 3",                  // duplicate picks are rejected
            "1 2 3 4",
            "1 2",
            "n",                        // start over
            "Tamsin", "1", "1", "1", "1 2", "y",
            "quit", "y",
            "2",
        ];

        var exitCode = cli.Run(inputs);
        var output = cli.Output;

        Assert.Equal(0, exitCode);
        Assert.Contains("A name needs 1 to 24 characters.", output);
        Assert.Contains("4d6kh3: [6, 6, 6, (6)] = 18", output);
        Assert.Contains("Pick exactly 4 different numbers", output);
        Assert.Contains("Begin your adventure as Wren's Cousin?", output);
        Assert.Contains("Begin your adventure as Tamsin?", output);
    }
}
