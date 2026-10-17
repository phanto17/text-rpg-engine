namespace RpgEngine.Core.Content.Defs;

/// <summary>Status applied on a hit; if <see cref="Save"/> is set, only when the target fails it.</summary>
public sealed record OnHitDef
{
    public SaveDef? Save { get; init; }

    public required string Status { get; init; }
}
