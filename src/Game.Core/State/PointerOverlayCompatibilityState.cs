using Game.Shared.RE;

namespace Game.State;

/// <summary>
///     Holds the carried-over pointer-overlay state block rooted at dseg:5A46 so the original overlay frame-advance
///     helpers can be represented without leaking this slice into generic input state.
/// </summary>
internal sealed class PointerOverlayCompatibilityState
{
    /// <summary>
    ///     Current 0x80-byte overlay background slot offset used by the pointer-overlay helpers.
    /// </summary>
    [GlobalSymbol("word_1C1D4", 0x1C1D4)] internal ushort CurrentOverlayBackgroundOffset;

    /// <summary>
    ///     Current overlay-background validity byte consumed by the pointer-overlay restore helpers.
    /// </summary>
    [GlobalSymbol("byte_1C1B6", 0x1C1B6)] internal byte CurrentOverlayBackgroundSaved;

    /// <summary>
    ///     Enables the pointer-overlay draw and restore path after the subsystem is initialized.
    /// </summary>
    [GlobalSymbol("byte_1C1B8", 0x1C1B8)] internal byte PointerOverlayEnabled;

    /// <summary>
    ///     Previous 0x80-byte overlay background slot offset swapped by <c>rollPointerHistoryAndPollInput</c>.
    /// </summary>
    [GlobalSymbol("word_1C1D6", 0x1C1D6)] internal ushort PreviousOverlayBackgroundOffset;

    /// <summary>
    ///     Previous overlay-background validity byte consumed by the pointer-overlay restore helpers.
    /// </summary>
    [GlobalSymbol("byte_1C1B7", 0x1C1B7)] internal byte PreviousOverlayBackgroundSaved;

    /// <summary>
    ///     Previous pointer column captured before the next input-poll pass.
    /// </summary>
    [GlobalSymbol("word_1C1D0", 0x1C1D0)] internal ushort PreviousPointerColumn;

    /// <summary>
    ///     Previous pointer row captured before the next input-poll pass.
    /// </summary>
    [GlobalSymbol("word_1C1D2", 0x1C1D2)] internal ushort PreviousPointerRow;

    /// <summary>
    ///     Managed adaptation of the original two 0x80-byte overlay background slots. Each slot stores the visible
    ///     16x8 saved background block as row-major indexed pixels for the current and previous pointer-overlay helpers.
    /// </summary>
    internal byte[] OverlayBackgroundScratchBuffer { get; } = new byte[0x100];
}
