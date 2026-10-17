using RpgEngine.Core.Dice;

namespace RpgEngine.Core.Views;

public sealed record ParleyOptionView(int Index, string Label, string CheckLabel, int Dc, RollMode Mode);
