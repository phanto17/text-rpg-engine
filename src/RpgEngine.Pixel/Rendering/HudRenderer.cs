using Raylib_cs;
using RpgEngine.Core.Characters;
using RpgEngine.Core.Combat;
using RpgEngine.Core.Content.Defs;
using RpgEngine.Core.Engine;
using RpgEngine.Core.Views;
using RpgEngine.Core.World;
using RpgEngine.Pixel.Game;
using RpgEngine.Pixel.Layout;
using RpgEngine.Pixel.Presentation;
using RpgEngine.Pixel.Sequencing;

namespace RpgEngine.Pixel.Rendering;

/// <summary>
/// The character panel: name, calling, level and XP, the eased HP and MP bars, AC and gold, the
/// Barrowlight's resonance, statuses, and the Inventory, Quests, Stats, and Exits tabs.
/// </summary>
internal sealed class HudRenderer(PixelRect area)
{
    private static readonly (HudTab Tab, string Label)[] Tabs = [(HudTab.Inventory, "Inv"), (HudTab.Quests, "Quests"), (HudTab.Stats, "Stats"), (HudTab.Exits, "Exits")];

    private readonly List<(PixelRect Rect, HudTab Tab)> _tabHits = [];
    private readonly List<(PixelRect Rect, string Command)> _commandHits = [];
    private double _shownHp = -1;
    private double _shownMana = -1;
    private string _exitsKey = string.Empty;
    private IReadOnlyList<MenuAction> _exits = [];

    /// <summary>A click: switches a tab, or returns a command to type (a compass direction, an item to inspect).</summary>
    public string? Click(PixelGame game, int x, int y)
    {
        foreach (var (rect, tab) in _tabHits)
        {
            if (rect.Contains(x, y))
            {
                game.Tab = tab;
                return null;
            }
        }

        foreach (var (rect, command) in _commandHits)
        {
            if (rect.Contains(x, y))
            {
                return command;
            }
        }

        return null;
    }

    public void Draw(PixelGame game, double dt, double time, int resonance)
    {
        Canvas.Panel(area, Palette.HudBack, Palette.PanelEdge);
        _tabHits.Clear();
        _commandHits.Clear();

        if (game.Session is not { } session)
        {
            DrawTitleOrCreation(game, time);
            return;
        }

        var player = session.State.Player;
        var content = session.Content;
        var x = area.X + 5;
        var right = area.Right - 5;
        var y = area.Y + 4;

        Canvas.BoldText(x, y, Canvas.Ellipsize(player.Name, 110), Palette.Title);
        var level = $"Lv {player.Level}";
        Canvas.Text(right - Canvas.Width(level), y, level, Palette.Accent);
        y += 11;
        var calling = content.Classes[player.ClassId].Name;
        var burden = content.Backgrounds.TryGetValue(player.BackgroundId, out var background) ? $", {background.Name}" : string.Empty;
        Canvas.Text(x, y, Canvas.Ellipsize(calling + burden, area.Width - 10), Palette.Muted);
        y += 12;

        _shownHp = Ease(_shownHp, player.CurrentHp, dt);
        Canvas.Text(x, y, "HP", Palette.Failure);
        Canvas.Bar(new PixelRect(x + 16, y + 1, 108, 8), _shownHp / Math.Max(1, player.MaxHp), Palette.HpFill, Palette.HpBack);
        var hpText = $"{player.CurrentHp}/{player.MaxHp}";
        Canvas.Text(right - Canvas.Width(hpText), y, hpText, Palette.Narration);
        y += 11;

        if (player.MaxMana > 0)
        {
            _shownMana = Ease(_shownMana, player.CurrentMana, dt);
            Canvas.Text(x, y, "MP", Palette.Mana);
            Canvas.Bar(new PixelRect(x + 16, y + 1, 108, 8), _shownMana / Math.Max(1, player.MaxMana), Palette.ManaFill, Palette.ManaBack);
            var mpText = $"{player.CurrentMana}/{player.MaxMana}";
            Canvas.Text(right - Canvas.Width(mpText), y, mpText, Palette.Narration);
            y += 11;
        }

        var next = Progression.NextLevelXp(player, content);
        var previous = content.Rules.XpThresholdsByLevel[Math.Min(player.Level - 1, content.Rules.XpThresholdsByLevel.Count - 1)];
        var xpRatio = next is { } needed && needed > previous ? (player.Xp - previous) / (double)(needed - previous) : 1;
        Canvas.Text(x, y, "XP", Palette.Gold);
        Canvas.Bar(new PixelRect(x + 16, y + 2, 108, 5), xpRatio, Palette.XpFill, Palette.XpBack);
        var xpText = next is { } n ? $"{player.Xp}/{n}" : $"{player.Xp}";
        Canvas.Text(right - Canvas.Width(xpText), y, xpText, Palette.Muted);
        y += 11;

        Canvas.Text(x, y, $"AC {CombatRules.ArmorClass(player, content)}", Palette.System);
        var gold = $"{player.Gold} gold";
        Canvas.Text(right - Canvas.Width(gold), y, gold, Palette.Gold);
        y += 11;

        var glow = Palette.Of(LanternLight.ColorFor(resonance));
        var flicker = (int)(Math.Sin(time * 6) * 1);
        Raylib.DrawRectangle(x + 2, y + 1, 6, 8, Emblems.Shade(glow, 0.5));
        Raylib.DrawRectangle(x + 3, y + 2, 4, 6, glow);
        Raylib.DrawRectangle(x + 3, y - 1 + flicker, 4, 1, Palette.PanelEdgeLight);
        Raylib.BeginBlendMode(BlendMode.Additive);
        Raylib.DrawCircleGradient(new System.Numerics.Vector2(x + 5, y + 5), 9 + flicker, Palette.Alpha(glow, 90), Palette.Alpha(glow, 0));
        Raylib.EndBlendMode();
        Canvas.Text(x + 14, y, $"Resonance {resonance}", glow);
        y += 12;

        if (session.Combat?.Player.Statuses is { Count: > 0 } statuses)
        {
            var sx = x;
            foreach (var status in statuses)
            {
                var label = Canvas.Ellipsize(status.Definition.Name, 60);
                var w = Canvas.Width(label) + 6;
                if (sx + w > right)
                {
                    break;
                }

                var chip = new PixelRect(sx, y, w, 10);
                Canvas.Fill(chip, Palette.Alpha(Palette.Warning, 60));
                Canvas.Frame(chip, Palette.Warning);
                Canvas.Text(sx + 3, y, label, Palette.Warning);
                sx += w + 3;
            }
        }
        else
        {
            Canvas.Text(x, y, session.Mode == GameMode.Combat ? "No conditions" : ModeLabel(session), Palette.Alpha(Palette.Muted, 160));
        }

        y += 14;
        DrawTabs(game, y);
        y += 13;
        var body = new PixelRect(area.X + 3, y, area.Width - 6, area.Bottom - y - 3);
        Canvas.Fill(body, Palette.PanelBack);
        Canvas.Frame(body, Palette.PanelEdge);
        Canvas.Clip(body.Inset(1));
        switch (game.Tab)
        {
            case HudTab.Inventory:
                DrawInventory(session, body);
                break;
            case HudTab.Quests:
                DrawQuests(session, body);
                break;
            case HudTab.Stats:
                DrawStats(game, session, body);
                break;
            default:
                DrawExits(session, body);
                break;
        }

        Canvas.Unclip();
    }

    private void DrawTitleOrCreation(PixelGame game, double time)
    {
        var x = area.X + 6;
        var y = area.Y + 6;
        Canvas.BoldText(x, y, game.Content.Manifest.Title, Palette.Title);
        Canvas.Text(x, y + 12, $"content {game.Content.Manifest.ContentVersion}", Palette.Muted);
        Raylib.DrawRectangle(x, y + 24, area.Width - 12, 1, Palette.PanelEdgeLight);
        y += 30;

        if (game.Creation is { } creation)
        {
            Canvas.Text(x, y, "Your character so far", Palette.Accent);
            y += 12;
            Row("Name", creation.Name);
            Row("Calling", creation.Class?.Name ?? string.Empty);
            Row("Burden", creation.Background?.Name ?? string.Empty);
            if (creation.Scores is { } scores)
            {
                foreach (var ability in AbilityExtensions.All)
                {
                    Row(ability.Abbreviation(), $"{scores[ability]} ({EventNarrator.Signed(scores.Modifier(ability))})");
                }
            }

            if (creation.Skills.Count > 0)
            {
                Row("Skills", string.Join(", ", creation.Skills.Select(s => game.Content.Skills[s].Name)));
            }

            return;
        }

        var lines = new[]
        {
            ("Click", "a button or press 1-9"),
            ("Type", "classic commands: look, go north"),
            ("Enter", "skips the typewriter"),
            ("Tab", "cycles the HUD tabs"),
            ("Esc", "goes back"),
            ("F2", "CRT scanlines"),
            ("F3", "whole-pixel scaling"),
            ("F11", "borderless fullscreen"),
        };
        foreach (var (key, what) in lines)
        {
            Row(key, what);
        }

        var glow = Palette.Of(LanternLight.ColorFor((int)(time * 1.2) % (LanternLight.FullResonance + 1)));
        Raylib.BeginBlendMode(BlendMode.Additive);
        Raylib.DrawCircleGradient(new System.Numerics.Vector2(area.CenterX, area.Bottom - 30), 26, Palette.Alpha(glow, 70), Palette.Alpha(glow, 0));
        Raylib.EndBlendMode();
        Canvas.Text(area.CenterX - (Canvas.Width("Barrowlight burns.") / 2), area.Bottom - 20, "Barrowlight burns.", glow);

        void Row(string key, string value)
        {
            Canvas.Text(x, y, key, Palette.Muted);
            Canvas.Text(x + 46, y, Canvas.Ellipsize(value, area.Width - 58), Palette.Narration);
            y += 11;
        }
    }

    private void DrawTabs(PixelGame game, int y)
    {
        var width = (area.Width - 6) / Tabs.Length;
        for (var i = 0; i < Tabs.Length; i++)
        {
            var (tab, label) = Tabs[i];
            var rect = new PixelRect(area.X + 3 + (i * width), y, width - 1, 12);
            var active = game.Tab == tab;
            Canvas.Fill(rect, active ? Palette.ButtonHover : Palette.ButtonBack);
            Canvas.Frame(rect, active ? Palette.Accent : Palette.ButtonEdge);
            Canvas.Text(rect.X + ((rect.Width - Canvas.Width(label)) / 2), rect.Y + 1, label, active ? Palette.Title : Palette.Muted);
            _tabHits.Add((rect, tab));
        }
    }

    private void DrawInventory(GameSession session, PixelRect body)
    {
        var player = session.State.Player;
        var y = body.Y + 3;
        if (player.Inventory.IsEmpty)
        {
            Canvas.Text(body.X + 4, y, "You are carrying nothing.", Palette.Muted);
            return;
        }

        foreach (var entry in player.Inventory.Entries)
        {
            if (y > body.Bottom - 10)
            {
                Canvas.Text(body.X + 4, y, "...", Palette.Muted);
                break;
            }

            var item = session.Content.Items[entry.ItemId];
            var equipped = item.Id == player.EquippedWeaponId || item.Id == player.EquippedArmorId;
            var count = entry.Count > 1 ? $" x{entry.Count}" : string.Empty;
            var row = new PixelRect(body.X + 2, y - 1, body.Width - 4, 11);
            _commandHits.Add((row, $"inspect {item.Name}"));
            Canvas.Text(body.X + 4, y, Canvas.Ellipsize(item.Name + count, body.Width - (equipped ? 34 : 10)), item.Type == ItemType.Quest ? Palette.Accent : Palette.Gold);
            if (equipped)
            {
                Canvas.Text(body.Right - 26, y, "worn", Palette.System);
            }

            y += 11;
        }
    }

    private void DrawQuests(GameSession session, PixelRect body)
    {
        var journal = session.Journal();
        var y = body.Y + 3;
        if (journal.Quests.Count == 0 && journal.Reputation.Count == 0)
        {
            Canvas.Text(body.X + 4, y, "Your journal is empty.", Palette.Muted);
            return;
        }

        foreach (var quest in journal.Quests)
        {
            Canvas.Text(body.X + 4, y, Canvas.Ellipsize(quest.Name, body.Width - 10), quest.Completed ? Palette.Success : Palette.Accent);
            y += 11;
            foreach (var row in TextWrapper.Wrap(LogLine.Of(quest.Journal), body.Width - 12, Canvas.Width))
            {
                if (y > body.Bottom - 10)
                {
                    return;
                }

                Canvas.Text(body.X + 8, y, string.Concat(row.Select(s => s.Text)), Palette.Muted);
                y += 10;
            }

            y += 3;
        }

        foreach (var reputation in journal.Reputation)
        {
            if (y > body.Bottom - 10)
            {
                return;
            }

            Canvas.Text(body.X + 4, y, Canvas.Ellipsize(reputation.Faction, body.Width - 40), Palette.Narration);
            var value = EventNarrator.Signed(reputation.Value);
            Canvas.Text(body.Right - Canvas.Width(value) - 4, y, value, reputation.Value >= 0 ? Palette.Success : Palette.Failure);
            y += 11;
        }
    }

    private void DrawStats(PixelGame game, GameSession session, PixelRect body)
    {
        var player = session.State.Player;
        var content = session.Content;
        var y = body.Y + 3;
        foreach (var ability in AbilityExtensions.All)
        {
            var save = player.IsProficientInSave(ability) ? "*" : string.Empty;
            Canvas.Text(body.X + 4, y, ability.Abbreviation(), Palette.Accent);
            Canvas.Text(body.X + 28, y, $"{player.Abilities[ability]} ({EventNarrator.Signed(player.Abilities.Modifier(ability))}){save}", Palette.Narration);
            y += 11;
        }

        var sy = body.Y + 3;
        var skills = content.SkillList.Where(s => player.GetSkillProficiency(s.Id) != ProficiencyLevel.None).ToList();
        foreach (var skill in skills)
        {
            if (sy > body.Y + 66)
            {
                break;
            }

            var level = player.GetSkillProficiency(skill.Id);
            var bonus = player.Abilities.Modifier(skill.Ability) + ProficiencyRules.Bonus(level, player.ProficiencyBonus);
            Canvas.Text(body.X + 84, sy, Canvas.Ellipsize($"{EventNarrator.Signed(bonus)} {skill.Name}{(level == ProficiencyLevel.Expertise ? "**" : string.Empty)}", body.Width - 88), Palette.Narration);
            sy += 11;
        }

        y += 2;
        var weapon = CombatRules.Weapon(player, content);
        Canvas.Text(body.X + 4, y, Canvas.Ellipsize($"{weapon.Name} {EventNarrator.Signed(weapon.ToHitBonus)}, {weapon.Damage.Text} {weapon.DamageType.ToString().ToLowerInvariant()}", body.Width - 8), Palette.Muted);
        y += 11;
        Canvas.Text(body.X + 4, y, $"Prof {EventNarrator.Signed(player.ProficiencyBonus)}   * save   ** expertise", Palette.Muted);
        y += 11;
        Canvas.Text(body.X + 4, y, Canvas.Ellipsize($"Seed {game.Seed}", body.Width - 8), Palette.Alpha(Palette.Muted, 150));
    }

    private void DrawExits(GameSession session, PixelRect body)
    {
        if (session.Mode != GameMode.Exploration)
        {
            Canvas.Text(body.X + 4, body.Y + 3, ModeLabel(session), Palette.Muted);
            return;
        }

        var key = $"{session.State.CurrentRoomId}|{session.State.World.Flags.Count}|{session.State.World.Counters.Count}|{session.State.World.VisitedRooms.Count}";
        if (key != _exitsKey)
        {
            _exitsKey = key;
            _exits = session.ExplorationActions().Where(a => a.Kind == MenuActionKind.Move && a.Command.Direction is not null).ToList();
        }

        var cx = body.CenterX;
        var cy = body.Y + 44;
        Raylib.DrawCircleLines(cx, cy, 30, Palette.PanelEdgeLight);
        Raylib.DrawCircleLines(cx, cy, 3, Palette.PanelEdge);
        foreach (var exit in _exits)
        {
            var direction = exit.Command.Direction!.Value;
            var (dx, dy) = direction switch
            {
                Direction.North => (0, -36),
                Direction.South => (0, 36),
                Direction.East => (56, 0),
                Direction.West => (-56, 0),
                Direction.Up => (-52, -30),
                Direction.Down => (-52, 30),
                Direction.In => (52, -30),
                _ => (52, 30),
            };
            var label = direction.ToString();
            var w = Canvas.Width(label) + 8;
            var rect = new PixelRect(cx + dx - (w / 2), cy + dy - 6, w, 12);
            Canvas.Fill(rect, Palette.ButtonBack);
            Canvas.Frame(rect, Palette.Accent);
            Canvas.Text(rect.X + 4, rect.Y + 1, label, Palette.Accent);
            _commandHits.Add((rect, $"go {label.ToLowerInvariant()}"));
        }

        var y = body.Y + 88;
        foreach (var exit in _exits)
        {
            if (y > body.Bottom - 10)
            {
                break;
            }

            Canvas.Text(body.X + 4, y, Canvas.Ellipsize(exit.Label, body.Width - 8), Palette.Muted);
            y += 10;
        }

        if (_exits.Count == 0)
        {
            Canvas.Text(body.X + 4, body.Y + 88, "No open way out.", Palette.Muted);
        }
    }

    private static string ModeLabel(GameSession session) => session.Mode switch
    {
        GameMode.Dialogue => "In conversation",
        GameMode.Combat => "In combat",
        GameMode.Shop => "At a merchant's stall",
        GameMode.GameOver => "Fallen",
        GameMode.Ended => "The story is told",
        _ => "Exploring",
    };

    private static double Ease(double shown, double target, double dt)
    {
        if (shown < 0)
        {
            return target;
        }

        var k = 1 - Math.Exp(-dt * 8);
        var next = shown + ((target - shown) * k);
        return Math.Abs(next - target) < 0.05 ? target : next;
    }
}
