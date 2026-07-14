using Game.Shared.RE;

namespace Game.State;

/// <summary>
///     Carries the game-owned pointer position consumed by runtime input seams.
/// </summary>
internal sealed class InputState
{
    internal const ushort MaxColumn = 319;
    internal const ushort MaxRow = 192;

    internal const ushort MinColumn = 0;
    internal const ushort MinRow = 0;

    /// <summary>
    ///     Current pointer column in display coordinates.
    /// </summary>
    [GlobalSymbol("word_1C1C2", 0x1C1C2)] internal ushort PointerColumn = 160;

    /// <summary>
    ///     Current pointer row in display coordinates.
    /// </summary>
    [GlobalSymbol("word_1C1C6", 0x1C1C6)] internal ushort PointerRow = 96;
}
