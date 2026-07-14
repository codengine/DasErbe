using Game.Display;
using Game.Hosting;
using Game.Shared.RE;
using Game.Shared.Rendering;
using Game.State;

namespace Game.Input;

/// <summary>
///     Session-owned pointer-overlay helpers for the carried-over cursor publication slice.
/// </summary>
/// <param name="displayBufferPublisher">Framebuffer publication seam.</param>
/// <param name="hostPacing">Host pacing seam.</param>
/// <param name="inputAdapter">Pointer/keyboard projection seam.</param>
/// <param name="state">Live runtime state.</param>
internal sealed class PointerOverlayController(DisplayBufferPublisher displayBufferPublisher,
    HostPacing hostPacing,
    InputAdapter inputAdapter,
    RuntimeState state)
{
    private const int PointerWidth = 0x10;
    private const int PointerHeight = 0x08;
    private const int PointerSlotSize = PointerWidth * PointerHeight;

    private static ReadOnlySpan<byte> PointerSpritePixels =>
    [
        0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0xFF, 0xFF, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00,
        0xFF, 0xFF, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00,
        0xFF, 0x00, 0xFF, 0x00, 0xFF, 0x00, 0x00, 0x00,
        0xFF, 0x00, 0xFF, 0x00, 0x00, 0xFF, 0x00, 0x00,
        0xFF, 0x00, 0x00, 0xFF, 0x00, 0x00, 0xFF, 0x00,
        0xFF, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0xFF,
        0xFF, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00,
        0xFF, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00,
        0xFF, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF
    ];

    /// <summary>
    ///     Enables the retained pointer-overlay bookkeeping and scratch slots.
    /// </summary>
    [FunctionSymbol("sub_11A29", 0x11A29)]
    internal void EnablePointerOverlay()
    {
        var overlay = state.PointerOverlay;

        // IDA 0x11A2C..0x11A4E: enable the pointer-overlay slice once and seed the current/previous saved-background
        // bookkeeping to the original two 0x80-byte overlay slots.
        if (overlay.PointerOverlayEnabled != 0)
        {
            return;
        }

        overlay.PointerOverlayEnabled = 1;
        overlay.CurrentOverlayBackgroundSaved = 0;
        overlay.PreviousOverlayBackgroundSaved = 0;
        overlay.CurrentOverlayBackgroundOffset = 0x0000;
        overlay.PreviousOverlayBackgroundOffset = 0x0080;
    }

    /// <summary>
    ///     Disables the pointer overlay during bootstrap shutdown.
    /// </summary>
    [FunctionSymbol("sub_11A50", 0x11A50)]
    internal void DisablePointerOverlay()
    {
        var overlay = state.PointerOverlay;
        if (overlay.PointerOverlayEnabled == 0)
        {
            return;
        }

        // IDA 0x11A5A..0x11A6A: publish the prepared framebuffer, restore the previously saved pointer background, then
        // roll the overlay/input history forward one last time before the overlay is disabled for shutdown. The original passes
        // dseg:5A46 explicitly to sub_112FE; the managed runtime keeps that same carried-over state in
        // PointerOverlayCompatibilityState.
        displayBufferPublisher.PublishPreparedBuffer();
        RestorePreviousPointerBackground();
        RollPointerHistoryAndPollInput();

        // IDA 0x11A6A..0x11A6F: clear the overlay-enabled flag after the terminal cleanup pass.
        overlay.PointerOverlayEnabled = 0;
    }

    /// <summary>
    ///     Advances the pointer overlay through capture, draw, publication, restore, and input refresh.
    /// </summary>
    [FunctionSymbol("sub_1155E", 0x1155E)]
    internal void AdvancePointerOverlayFrame()
    {
        // IDA 0x11561..0x11574: capture the current 16x8 background block under the pointer, then redraw the pointer
        // overlay into the current work buffer. The original explicit dseg:5A46 state block is split in the managed
        // runtime across PointerOverlayCompatibilityState and InputState.
        CaptureCurrentPointerBackground();
        DrawPointerOverlayToWorkBuffer();

        // IDA 0x11575: shared framebuffer publication seam between the retained display buffers (sub_1052D).
        displayBufferPublisher.PublishPreparedBuffer();
        hostPacing.WaitFrame();

        // IDA 0x11578..0x11581: restore the previous overlay background after the retrace-paced page flip.
        RestorePreviousPointerBackground();

        // IDA 0x11582: roll pointer history, then enter the shared input-poll seam via sub_11917.
        RollPointerHistoryAndPollInput();
    }

    /// <summary>
    ///     Captures the visible background block under the current pointer position.
    /// </summary>
    [FunctionSymbol("sub_113CB", 0x113CB, FunctionFlags.AdaptedForMonoGame)]
    private void CaptureCurrentPointerBackground()
    {
        var overlay = state.PointerOverlay;
        if (overlay.PointerOverlayEnabled == 0)
        {
            return;
        }

        var input = state.Input;
        if (!TryGetVisiblePointerRegion(unchecked((short)input.PointerColumn),
                input.PointerRow,
                out var destinationColumn,
                out var destinationRow,
                out _,
                out var visibleWidth))
        {
            return;
        }

        var workBuffer = state.Presentation.Display.GetWorkBuffer();
        var currentBackgroundSlot = GetOverlayBackgroundSlot(overlay.CurrentOverlayBackgroundOffset);

        // IDA 0x11425..0x1145A: resolve the current work-buffer source byte region and configure the original planar
        // read path through sub_1025E(1) and sub_1024E(0x0F). The retained-buffer runtime keeps those symbol boundaries
        // explicit even though their hardware effects are adapted away.
        // Ignored in game definition

        // IDA 0x1145E..0x11488: capture the visible 16x8 background block into the current overlay slot.
        for (var row = 0; row < PointerHeight; row++)
        {
            var sourcePixels = workBuffer.GetRowSpan(destinationRow + row).Slice(destinationColumn, visibleWidth);
            sourcePixels.CopyTo(currentBackgroundSlot.Slice(row * PointerWidth, visibleWidth));
        }

        // IDA 0x11481..0x11488: restore the original graphics-controller mode latch after the copy.
        // Ignored in game definition
    }

    /// <summary>
    ///     Draws the pointer sprite into the current retained work buffer.
    /// </summary>
    [FunctionSymbol("sub_1148E", 0x1148E, FunctionFlags.AdaptedForMonoGame)]
    private void DrawPointerOverlayToWorkBuffer()
    {
        var overlay = state.PointerOverlay;
        if (overlay.PointerOverlayEnabled == 0)
        {
            return;
        }

        var input = state.Input;
        if (!TryGetVisiblePointerRegion(unchecked((short)input.PointerColumn),
                input.PointerRow,
                out var destinationColumn,
                out var destinationRow,
                out var spriteColumnStart,
                out var visibleWidth))
        {
            return;
        }

        var workBuffer = state.Presentation.Display.GetWorkBuffer();

        // IDA 0x114EC..0x11518: mark the current slot valid, resolve the current work-buffer destination, and select
        // the first visible sprite column from the built-in 16x8 pointer pattern at dseg:0186.
        overlay.CurrentOverlayBackgroundSaved = 1;

        // IDA 0x1151B..0x11558: the original uses VGA sequencer map-mask cycling to draw one column at a time into
        // planar display memory. The managed runtime adapts that into direct indexed writes to the retained work buffer
        // while preserving the transparent-zero sprite semantics and clipping behavior.
        for (var column = 0; column < visibleWidth; column++)
        {
            var spriteColumn = spriteColumnStart + column;
            for (var row = 0; row < PointerHeight; row++)
            {
                var pixel = PointerSpritePixels[spriteColumn * PointerHeight + row];
                if (pixel != 0)
                {
                    workBuffer.GetRowSpan(destinationRow + row)[destinationColumn + column] = pixel;
                }
            }
        }

        state.Presentation.Screen.Invalidate(new IntRect(destinationColumn,
            destinationRow,
            visibleWidth,
            PointerHeight));
    }

    /// <summary>
    ///     Restores the previously saved pointer background block.
    /// </summary>
    [FunctionSymbol("sub_112FE", 0x112FE, FunctionFlags.AdaptedForMonoGame)]
    internal void RestorePreviousPointerBackground()
    {
        var overlay = state.PointerOverlay;
        if (overlay.PointerOverlayEnabled == 0 || overlay.PreviousOverlayBackgroundSaved == 0)
        {
            return;
        }

        if (!TryGetVisiblePointerRegion(unchecked((short)overlay.PreviousPointerColumn),
                overlay.PreviousPointerRow,
                out var destinationColumn,
                out var destinationRow,
                out _,
                out var visibleWidth))
        {
            return;
        }

        var workBuffer = state.Presentation.Display.GetWorkBuffer();
        var previousBackgroundSlot = GetOverlayBackgroundSlot(overlay.PreviousOverlayBackgroundOffset);

        // IDA 0x11362..0x11397: resolve the current work-buffer destination and configure the original planar write
        // path through sub_1025E(1) and sub_1024E(0x0F). The retained-buffer runtime keeps those symbol boundaries explicit
        // even though their hardware effects are adapted away.
        // Ignored in game definition

        // IDA 0x1139B..0x113C5: restore the visible 16x8 background block from the previous overlay slot.
        for (var row = 0; row < PointerHeight; row++)
        {
            var slotPixels = previousBackgroundSlot.Slice(row * PointerWidth, visibleWidth);
            slotPixels.CopyTo(workBuffer.GetRowSpan(destinationRow + row).Slice(destinationColumn, visibleWidth));
        }

        // IDA 0x113BE..0x113C5: restore the original graphics-controller mode latch after the copy.
        // Ignored in game definition

        state.Presentation.Screen.Invalidate(new IntRect(destinationColumn,
            destinationRow,
            visibleWidth,
            PointerHeight));
    }

    /// <summary>
    ///     Rolls pointer history forward and refreshes the shared input state.
    /// </summary>
    [FunctionSymbol("sub_119ED", 0x119ED)]
    private void RollPointerHistoryAndPollInput()
    {
        var input = state.Input;
        var overlay = state.PointerOverlay;

        // IDA 0x119F2..0x11A1F: snapshot the current pointer position into the carried-over "previous frame"
        // globals, then rotate the current/previous overlay background slot bookkeeping.
        overlay.PreviousPointerColumn = input.PointerColumn;
        overlay.PreviousPointerRow = input.PointerRow;
        overlay.PreviousOverlayBackgroundSaved = overlay.CurrentOverlayBackgroundSaved;

        (overlay.PreviousOverlayBackgroundOffset, overlay.CurrentOverlayBackgroundOffset) = (
            overlay.CurrentOverlayBackgroundOffset, overlay.PreviousOverlayBackgroundOffset);

        // IDA 0x11A22: enter the shared pointer-input refresh helper (sub_11917).
        inputAdapter.RefreshPointerInput();
    }

    private Span<byte> GetOverlayBackgroundSlot(ushort offset)
    {
        var overlay = state.PointerOverlay;
        if (offset + PointerSlotSize > overlay.OverlayBackgroundScratchBuffer.Length)
        {
            throw new InvalidOperationException(
                $"Overlay background slot offset 0x{offset:X4} falls outside the managed 0x100-byte scratch buffer.");
        }

        return overlay.OverlayBackgroundScratchBuffer.AsSpan(offset, PointerSlotSize);
    }

    private bool TryGetVisiblePointerRegion(short pointerColumn,
        ushort pointerRow,
        out int destinationColumn,
        out int destinationRow,
        out int spriteColumnStart,
        out int visibleWidth)
    {
        destinationColumn = pointerColumn;
        destinationRow = pointerRow;
        spriteColumnStart = 0;
        visibleWidth = PointerWidth;

        switch (pointerColumn)
        {
            case < 0:
                spriteColumnStart = -pointerColumn;
                visibleWidth = PointerWidth - spriteColumnStart;
                destinationColumn = 0;
                break;
            case > 0x130:
                visibleWidth = 0x140 - pointerColumn;
                break;
        }

        if (visibleWidth <= 0)
        {
            return false;
        }

        var workBuffer = state.Presentation.Display.GetWorkBuffer();
        if (destinationRow < 0 || destinationRow + PointerHeight > workBuffer.Height)
        {
            throw new InvalidOperationException(
                $"Pointer row {destinationRow} falls outside the 8-row overlay helper contract for the current work buffer.");
        }

        if (destinationColumn < 0 || destinationColumn + visibleWidth > workBuffer.Width)
        {
            throw new InvalidOperationException(
                $"Pointer column {pointerColumn} with visible width {visibleWidth} falls outside the current work buffer.");
        }

        return true;
    }
}
