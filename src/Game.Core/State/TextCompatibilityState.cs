using Game.Shared.RE;

namespace Game.State;

internal sealed class TextCompatibilityState
{
    /// <summary>
    ///     Column seed restored on line breaks while the FONT_VGA renderer walks multiline strings.
    /// </summary>
    [GlobalSymbol("word_1D3EE", 0x1D3EE)] internal ushort TextBlockStartColumn;

    /// <summary>
    ///     Current text cursor column consumed by the FONT_VGA text renderer slice.
    /// </summary>
    [GlobalSymbol("word_1D0DC", 0x1D0DC)] internal ushort TextCursorColumn;

    /// <summary>
    ///     Current text cursor row consumed by the FONT_VGA text renderer slice.
    /// </summary>
    [GlobalSymbol("word_1D0DE", 0x1D0DE)] internal ushort TextCursorRow;
}
