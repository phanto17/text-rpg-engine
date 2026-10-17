using RpgEngine.Core.Commands;
using RpgEngine.Core.Content.Defs;

namespace RpgEngine.Tests.Commands;

public sealed class CommandParserTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("the a an")]
    public void Parse_Empty(string? input)
    {
        Assert.Equal(CommandVerb.Empty, CommandParser.Parse(input).Verb);
    }

    [Theory]
    [InlineData("n", Direction.North)]
    [InlineData("north", Direction.North)]
    [InlineData("S", Direction.South)]
    [InlineData("e", Direction.East)]
    [InlineData("w", Direction.West)]
    [InlineData("u", Direction.Up)]
    [InlineData("d", Direction.Down)]
    [InlineData("in", Direction.In)]
    [InlineData("outside", Direction.Out)]
    [InlineData("go north", Direction.North)]
    [InlineData("walk to the east", Direction.East)]
    [InlineData("climb up", Direction.Up)]
    [InlineData("head down!", Direction.Down)]
    public void Parse_Directions(string input, Direction expected)
    {
        var command = CommandParser.Parse(input);

        Assert.Equal(CommandVerb.Go, command.Verb);
        Assert.Equal(expected, command.Direction);
    }

    [Fact]
    public void Parse_GoWithoutDirection_KeepsTarget()
    {
        var command = CommandParser.Parse("go home");

        Assert.Equal(CommandVerb.Go, command.Verb);
        Assert.Null(command.Direction);
        Assert.Equal("home", command.Target);
        Assert.Null(CommandParser.Parse("go").Target);
    }

    [Theory]
    [InlineData("look", CommandVerb.Look, null)]
    [InlineData("l", CommandVerb.Look, null)]
    [InlineData("look at the bronze door", CommandVerb.Inspect, "bronze door")]
    [InlineData("look door", CommandVerb.Inspect, "door")]
    [InlineData("examine statue", CommandVerb.Inspect, "statue")]
    [InlineData("x statue", CommandVerb.Inspect, "statue")]
    [InlineData("inspect", CommandVerb.Inspect, null)]
    [InlineData("take the potion", CommandVerb.Take, "potion")]
    [InlineData("get potion", CommandVerb.Take, "potion")]
    [InlineData("grab potion", CommandVerb.Take, "potion")]
    [InlineData("pick up a potion", CommandVerb.Take, "potion")]
    [InlineData("drop sword", CommandVerb.Drop, "sword")]
    [InlineData("discard sword", CommandVerb.Drop, "sword")]
    [InlineData("equip sword", CommandVerb.Equip, "sword")]
    [InlineData("wield sword", CommandVerb.Equip, "sword")]
    [InlineData("wear chain mail", CommandVerb.Equip, "chain mail")]
    [InlineData("talk to the old sage", CommandVerb.Talk, "old sage")]
    [InlineData("speak with sage", CommandVerb.Talk, "sage")]
    [InlineData("talk", CommandVerb.Talk, null)]
    [InlineData("save slot1", CommandVerb.Save, "slot1")]
    [InlineData("save", CommandVerb.Save, null)]
    [InlineData("load slot1", CommandVerb.Load, "slot1")]
    [InlineData("restore", CommandVerb.Load, null)]
    [InlineData("press sun", CommandVerb.Interact, "sun")]
    [InlineData("Press the SUN sigil.", CommandVerb.Interact, "sun sigil")]
    [InlineData("dance", CommandVerb.Interact, null)]
    public void Parse_VerbsAndTargets(string input, CommandVerb verb, string? target)
    {
        var command = CommandParser.Parse(input);

        Assert.Equal(verb, command.Verb);
        Assert.Equal(target, command.Target);
    }

    [Theory]
    [InlineData("i", CommandVerb.Inventory)]
    [InlineData("inventory", CommandVerb.Inventory)]
    [InlineData("inv", CommandVerb.Inventory)]
    [InlineData("stats", CommandVerb.Stats)]
    [InlineData("c", CommandVerb.Stats)]
    [InlineData("sheet", CommandVerb.Stats)]
    [InlineData("journal", CommandVerb.Journal)]
    [InlineData("j", CommandVerb.Journal)]
    [InlineData("help", CommandVerb.Help)]
    [InlineData("?", CommandVerb.Help)]
    [InlineData(" ? ", CommandVerb.Help)]
    [InlineData("quit", CommandVerb.Quit)]
    [InlineData("exit", CommandVerb.Quit)]
    public void Parse_MetaCommands(string input, CommandVerb expected)
    {
        Assert.Equal(expected, CommandParser.Parse(input).Verb);
    }

    [Fact]
    public void Parse_UseOn_SplitsTargets()
    {
        var command = CommandParser.Parse("use the iron key on the bronze door");

        Assert.Equal(CommandVerb.Use, command.Verb);
        Assert.Equal("iron key", command.Target);
        Assert.Equal("bronze door", command.Secondary);
    }

    [Fact]
    public void Parse_UseWith_SplitsTargets()
    {
        var command = CommandParser.Parse("use key with lock");

        Assert.Equal(("key", "lock"), (command.Target, command.Secondary));
        Assert.Null(CommandParser.Parse("use potion").Secondary);
    }

    [Fact]
    public void Parse_KeepsTypedVerbWord()
    {
        Assert.Equal("press", CommandParser.Parse("PRESS sun").Word);
    }
}
