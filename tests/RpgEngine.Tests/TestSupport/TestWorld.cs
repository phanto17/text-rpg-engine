using RpgEngine.Core.Characters;
using RpgEngine.Core.Checks;
using RpgEngine.Core.Content;
using RpgEngine.Core.Dice;
using RpgEngine.Core.World;

namespace RpgEngine.Tests.TestSupport;

/// <summary>Wires content, scripted dice, and the world services together for unit tests.</summary>
internal sealed class TestWorld
{
    public TestWorld(GameContent? content = null, PlayerCharacter? player = null, params int[] dice)
    {
        Content = content ?? new TestContent().Load();
        Random = new FixedRandomSource(dice);
        Dice = new DiceRoller(Random);
        Checks = new CheckRunner(Content, new CheckResolver(Dice, Content.Rules.NaturalCritsOnChecks));
        Text = new TextFormatter(Content);
        Effects = new EffectApplier(Content, Dice, Checks, Text);
        State = new GameState(player ?? TestCharacters.Create(), WorldState.CreateNew(Content), Content.Manifest.StartRoom);
    }

    public GameContent Content { get; }

    public FixedRandomSource Random { get; }

    public DiceRoller Dice { get; }

    public CheckRunner Checks { get; }

    public TextFormatter Text { get; }

    public EffectApplier Effects { get; }

    public GameState State { get; }

    public PlayerCharacter Player => State.Player;

    public WorldState World => State.World;
}
