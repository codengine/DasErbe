using Game.Shared.RE;
using Game.Shared.Rendering;
using Game.State;

namespace Game.Display;

/// <summary>
///     Session-owned indexed draw helpers for the carried-over primitive fill slice.
/// </summary>
/// <param name="state">Live runtime state.</param>
internal sealed class DisplayPrimitiveController(RuntimeState state)
{
    /// <summary>
    ///     Fills an inclusive rectangle with the current draw color.
    /// </summary>
    /// <param name="leftColumn">Inclusive left column.</param>
    /// <param name="topRow">Inclusive top row.</param>
    /// <param name="rightColumn">Inclusive right column.</param>
    /// <param name="bottomRow">Inclusive bottom row.</param>
    [FunctionSymbol("sub_11592", 0x11592, FunctionFlags.AdaptedForMonoGame)]
    internal void FillRectangleWithCurrentColor(ushort leftColumn, ushort topRow, ushort rightColumn, ushort bottomRow)
    {
        // IDA 0x11595..0x115AF: normalize inverted rows by tail-recursing with the top/bottom arguments swapped.
        if (topRow > bottomRow)
        {
            (topRow, bottomRow) = (bottomRow, topRow);
        }

        // IDA 0x115B1..0x115CC originally forwarded each row to sub_109E4. The managed renderer owns dirty-region
        // bookkeeping, so the same inclusive filled rectangle is emitted in one blitter call and one invalidation.
        var display = state.Presentation.Display;
        var workBuffer = display.GetWorkBuffer();
        var fillRect = GetInclusiveRectangle(workBuffer, leftColumn, topRow, rightColumn, bottomRow);
        Blitter.FillRect(workBuffer, fillRect, display.CurrentDrawColorIndex);
        state.Presentation.Screen.Invalidate(fillRect);
    }

    private static IntRect GetInclusiveRectangle(Surface workBuffer,
        ushort leftColumn,
        ushort topRow,
        ushort rightColumn,
        ushort bottomRow)
    {
        if (leftColumn > rightColumn)
        {
            (leftColumn, rightColumn) = (rightColumn, leftColumn);
        }

        // The original primitive helpers do not bound-check and can be invoked with rightColumn == 0x0140.
        // The managed runtime clamps to the retained work-buffer bounds instead of allowing memory overrun.
        var maxColumn = checked((ushort)Math.Max(0, workBuffer.Width - 1));
        var maxRow = checked((ushort)Math.Max(0, workBuffer.Height - 1));
        if (leftColumn > maxColumn || topRow > maxRow)
        {
            return default;
        }

        if (rightColumn > maxColumn)
        {
            rightColumn = maxColumn;
        }

        if (bottomRow > maxRow)
        {
            bottomRow = maxRow;
        }

        return new IntRect(leftColumn, topRow, checked(rightColumn - leftColumn + 1), checked(bottomRow - topRow + 1));
    }
}
