using Game.Shared.RE;
using Game.State;

namespace Game.Text;

/// <summary>
///     Session-owned text cursor compatibility writer for carried-over text-layout symbols.
/// </summary>
/// <param name="state">Live runtime state.</param>
internal sealed class TextCursorController(RuntimeState state)
{
    /// <summary>
    ///     Seeds the multiline text-block cursor position.
    /// </summary>
    /// <param name="column">Seed column for the multiline text block.</param>
    /// <param name="row">Seed row for the multiline text block.</param>
    [FunctionSymbol("sub_115F9", 0x115F9)]
    internal void SeedTextBlockCursor(ushort column, ushort row)
    {
        var text = state.Presentation.Text;

        // IDA 0x115FC..0x11608: publish the multiline text-block start column, current text cursor column, and current
        // text cursor row for later FONT_VGA text rendering helpers.
        text.TextBlockStartColumn = column;
        text.TextCursorColumn = column;
        text.TextCursorRow = row;
    }

    /// <summary>
    ///     Advances the text-block cursor to the next 8-pixel row.
    /// </summary>
    [FunctionSymbol("sub_1160D", 0x1160D)]
    internal void AdvanceTextBlockCursorToNextRow()
    {
        var text = state.Presentation.Text;

        // IDA 0x11610..0x11616: reset the current text cursor column to the block start, then advance the current text
        // cursor row by one FONT_VGA line (8 pixels).
        text.TextCursorColumn = text.TextBlockStartColumn;
        text.TextCursorRow += 8;
    }
}
