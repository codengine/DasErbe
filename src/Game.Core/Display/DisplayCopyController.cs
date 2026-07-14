using Game.Shared.RE;
using Game.Shared.Rendering;
using Game.State;

namespace Game.Display;

/// <summary>
///     Session-owned display copy helpers for carried-over display wrapper symbols.
/// </summary>
/// <param name="state">Live runtime state.</param>
internal sealed class DisplayCopyController(RuntimeState state)
{
    private const int SourceStridePixels = RuntimeState.FrameWidth;
    private const int DisplayBufferWidthPixels = RuntimeState.FrameWidth;
    private const int DisplayBufferHeightRows = 0x00C8;

    /// <summary>
    ///     Copies the published software framebuffer into the current work buffer.
    /// </summary>
    [FunctionSymbol("sub_10416", 0x10416, FunctionFlags.AdaptedForMonoGame)]
    internal void CopyPublishedBufferToWorkBuffer()
    {
        var display = state.Presentation.Display;

        // IDA 0x1041E..0x1042B: enable the VGA planar read/write configuration through the preserved register-call seams.
        // Ignored in game definition

        // IDA 0x1042C..0x1044F: clone the current published 320x200 image into the work buffer without publishing it.
        // Presentation is not invalidated because the published framebuffer does not change here.
        var publishedBuffer = display.GetPublishedBuffer();
        var workBuffer = display.GetWorkBuffer();
        Blitter.Copy(publishedBuffer,
            new IntRect(0, 0, DisplayBufferWidthPixels, DisplayBufferHeightRows),
            workBuffer,
            0,
            0);

        // IDA 0x10450..0x1045C: restore the graphics-mode latch through the explicit compatibility seam.
        // Ignored in game definition
    }

    /// <summary>
    ///     Copies the current work buffer into the retained snapshot buffer.
    /// </summary>
    [FunctionSymbol("sub_1045D", 0x1045D, FunctionFlags.AdaptedForMonoGame)]
    internal void CopyWorkBufferToSnapshotBuffer()
    {
        var display = state.Presentation.Display;

        // IDA 0x10465..0x10472: enable the VGA planar read/write configuration through the preserved register-call seams.
        // Ignored in game definition

        // IDA 0x10473..0x10496: clone the current 320x200 work buffer into the fixed snapshot buffer without
        // invalidating presentation, because neither the work copy nor the snapshot copy is published here.
        var workBuffer = display.GetWorkBuffer();
        var snapshotBuffer = display.GetSnapshotBuffer();
        Blitter.Copy(workBuffer,
            new IntRect(0, 0, DisplayBufferWidthPixels, DisplayBufferHeightRows),
            snapshotBuffer,
            0,
            0);

        // IDA 0x10497..0x104A3: restore the graphics-mode latch through the explicit compatibility seam.
        // Ignored in game definition
    }

    /// <summary>
    ///     Restores one planar-aligned region from the retained snapshot buffer into the current work buffer.
    /// </summary>
    /// <param name="column">Left column of the retained snapshot region.</param>
    /// <param name="row">Top row of the retained snapshot region.</param>
    /// <param name="widthPixels">Width of the region in pixels.</param>
    /// <param name="heightRows">Height of the region in rows.</param>
    [FunctionSymbol("sub_104A4", 0x104A4, FunctionFlags.AdaptedForMonoGame)]
    internal void CopySnapshotRegionToWorkBuffer(ushort column, ushort row, ushort widthPixels, ushort heightRows)
    {
        var display = state.Presentation.Display;

        // IDA 0x104A9..0x104B6: enable the VGA planar read/write configuration through the preserved register-call seams.
        // Ignored in game definition

        // IDA 0x104B7..0x10508: compute the same-coordinate planar byte-window copy from the fixed snapshot buffer
        // into the current work buffer. The original rounds the left edge down to the containing 4-pixel group and
        // rounds the right edge up through the copied plane bytes; the managed runtime preserves that helper contract by
        // blitting the aligned indexed superset rectangle without invalidating presentation.
        var snapshotBuffer = display.GetSnapshotBuffer();
        var workBuffer = display.GetWorkBuffer();
        var sourceRect = GetPlanarAlignedRegion(column, row, widthPixels, heightRows);
        EnsureSupportedDisplayRegion(snapshotBuffer, workBuffer, sourceRect);
        Blitter.Copy(snapshotBuffer, sourceRect, workBuffer, sourceRect.X, sourceRect.Y);

        // IDA 0x1050B..0x10515: restore the graphics-mode latch through the explicit compatibility seam.
        // Ignored in game definition
    }

    /// <summary>
    ///     Copies one indexed source region into the current work buffer.
    /// </summary>
    /// <param name="sourceOffsetBytes">Byte offset into the source buffer.</param>
    /// <param name="sourceBuffer">Decoded 320-byte-stride source image buffer.</param>
    /// <param name="region">Source and destination copy region descriptor.</param>
    [FunctionSymbol("sub_10578", 0x10578, FunctionFlags.AdaptedForMonoGame)]
    internal void CopyIndexedRegionToWorkBuffer(ushort sourceOffsetBytes, byte[] sourceBuffer, DisplayCopyRegion region)
    {
        // IDA 0x1057E..0x10594: return immediately when the requested copy is empty.
        if (region.WidthPixels == 0 || region.HeightRows == 0)
        {
            return;
        }

        var display = state.Presentation.Display;
        var destinationSurface = display.GetWorkBuffer();
        var destinationX = region.DestinationColumn;
        var destinationY = region.DestinationRow;
        var sourceStartIndex = checked(sourceOffsetBytes + region.SourceColumn + region.SourceRow * SourceStridePixels);
        var copyWidth = region.WidthPixels;
        var copyHeight = region.HeightRows;

        // IDA 0x10597..0x105F9: compute the source origin and resolve the work-buffer destination.
        // The managed runtime preserves the selected retained framebuffer by using the work-buffer role.
        EnsureSupportedCopyBounds(sourceBuffer,
            sourceStartIndex,
            copyWidth,
            copyHeight,
            destinationSurface,
            destinationX,
            destinationY);

        // IDA 0x105FA..0x10630: copy the indexed rectangle into the current work buffer and mark the affected
        // destination region dirty for presentation.
        Blitter.CopyIndexed8(sourceBuffer.AsSpan(sourceOffsetBytes),
            SourceStridePixels,
            new IntRect(region.SourceColumn, region.SourceRow, copyWidth, copyHeight),
            destinationSurface,
            destinationX,
            destinationY);

        state.Presentation.Screen.Invalidate(new IntRect(destinationX, destinationY, copyWidth, copyHeight));
    }

    /// <summary>
    ///     Copies a clipped transparent indexed source region into the current work buffer.
    /// </summary>
    /// <param name="sourceOffsetBytes">Byte offset into the 320-byte-stride source buffer.</param>
    /// <param name="sourceBuffer">Decoded source image buffer backing the original source region.</param>
    /// <param name="region">Signed source/destination copy descriptor with transparent-zero semantics.</param>
    [FunctionSymbol("sub_10637", 0x10637, FunctionFlags.AdaptedForMonoGame)]
    internal void CopyClippedTransparentIndexedRegionToWorkBuffer(ushort sourceOffsetBytes,
        byte[] sourceBuffer,
        DisplayCopyRegion region)
    {
        // IDA 0x1063F..0x106B8: clip the signed destination rectangle against the 320x200 work buffer while translating
        // any off-screen negative destination delta back into the source origin. The original mutates the caller-owned
        // six-word descriptor in place; the managed runtime consumes the descriptor by value and applies the same
        // clipping to the local working copy used for the blit.
        if (region.DestinationColumn + region.WidthPixels > DisplayBufferWidthPixels)
        {
            region.WidthPixels = (short)(DisplayBufferWidthPixels - region.DestinationColumn);
        }
        else if (region.DestinationColumn < 0)
        {
            region.SourceColumn = (short)(region.SourceColumn - region.DestinationColumn);
            region.WidthPixels = (short)(region.WidthPixels + region.DestinationColumn);
            region.DestinationColumn = 0;
        }

        if (region.DestinationRow + region.HeightRows > DisplayBufferHeightRows)
        {
            region.HeightRows = (short)(DisplayBufferHeightRows - region.DestinationRow);
        }
        else if (region.DestinationRow < 0)
        {
            region.SourceRow = (short)(region.SourceRow - region.DestinationRow);
            region.HeightRows = (short)(region.HeightRows + region.DestinationRow);
            region.DestinationRow = 0;
        }

        // IDA 0x106BB..0x106CC: return immediately once clipping empties the copy region.
        if (region.WidthPixels <= 0 || region.HeightRows <= 0)
        {
            return;
        }

        var display = state.Presentation.Display;
        var destinationSurface = display.GetWorkBuffer();
        var destinationX = region.DestinationColumn;
        var destinationY = region.DestinationRow;
        var copyWidth = region.WidthPixels;
        var copyHeight = region.HeightRows;
        var sourceStartIndex = checked(sourceOffsetBytes + region.SourceColumn + region.SourceRow * SourceStridePixels);

        // IDA 0x106CF..0x1072F: resolve the clipped source and work-buffer destination cursors. The managed runtime
        // preserves the same visible contract by resolving directly into the retained indexed work buffer.
        EnsureSupportedTransparentCopyBounds(sourceBuffer,
            sourceStartIndex,
            copyWidth,
            copyHeight,
            destinationSurface,
            destinationX,
            destinationY);

        // IDA 0x10732..0x1076F: copy the clipped rectangle while treating palette index 0 as transparent. The original
        // cycles the VGA sequencer plane mask one column at a time; the managed runtime writes the resolved indexed pixels
        // directly into the retained work buffer and invalidates the final clipped region for presentation.
        Blitter.KeyedCopyIndexed8(sourceBuffer.AsSpan(sourceOffsetBytes),
            SourceStridePixels,
            new IntRect(region.SourceColumn, region.SourceRow, copyWidth, copyHeight),
            destinationSurface,
            destinationX,
            destinationY,
            0);

        state.Presentation.Screen.Invalidate(new IntRect(destinationX, destinationY, copyWidth, copyHeight));
    }

    /// <summary>
    ///     Draws the scaled backdrop region for the current scene composition pass.
    /// </summary>
    /// <param name="region">Program-scene backdrop resample descriptor built by <c>RenderBackdrop</c>.</param>
    [FunctionSymbol("sub_10776", 0x10776, FunctionFlags.AdaptedForMonoGame)]
    internal void DrawScaledBackdrop(ProgramSceneBackdropCopyRegion region)
    {
        // IDA 0x1077E..0x107A6: return immediately unless both source and destination dimensions are non-zero.
        if (region.SourceWidthPixels == 0 || region.SourceHeightRows == 0 || region.DestinationWidthPixels == 0 ||
            region.DestinationHeightRows == 0)
        {
            return;
        }

        var sourceWidthPixels = region.SourceWidthPixels;
        var sourceHeightRows = region.SourceHeightRows;
        var horizontalStep = region.HorizontalStep == -1 ? -1 : 1;

        // IDA 0x10824..0x10887: resolve the backdrop source surface origin and current work-buffer destination origin.
        var sourceBuffer = state.Program.ScratchBuffers.Backdrop;
        ValidateBackdropSourceBounds(sourceBuffer, region);
        var display = state.Presentation.Display;
        var buffers = display.RetainedDisplayBuffers;
        var workBufferIndex = display.WorkBufferIndex;
        var fullyInsideCurrentWorkBuffer = region is { DestinationColumn: >= 0, DestinationRow: >= 0 } &&
                                           region.DestinationColumn + region.DestinationWidthPixels <=
                                           DisplayBufferWidthPixels &&
                                           region.DestinationRow + region.DestinationHeightRows <=
                                           DisplayBufferHeightRows;

        var firstSourceColumnCursor = checked(region.SourceColumn + region.SourceRow * SourceStridePixels);
        if (horizontalStep < 0)
        {
            firstSourceColumnCursor += sourceWidthPixels - 1;
        }

        // IDA 0x1088A..0x10907: scale the source backdrop rectangle onto the work buffer with repeat/skip thresholds in
        // both axes, preserving transparent-zero semantics and the signed horizontal mirror step.
        //
        // Intent note, IDA 0x10867..0x10887 and 0x108D1..0x10907: the original computes a raw destination pointer
        // from the signed destination origin and writes without viewport clipping. Slightly off-screen backdrop frames
        // therefore alias through contiguous retained display memory rather than faulting. The managed runtime preserves
        // that behavior within the retained display-buffer set by mapping writes through absolute retained display-memory
        // pixel offsets instead of forcing Cartesian clipping on the current work buffer.
        if (fullyInsideCurrentWorkBuffer)
        {
            Span<AxisRun> columnRuns = stackalloc AxisRun[sourceWidthPixels];
            Span<AxisRun> rowRuns = stackalloc AxisRun[sourceHeightRows];
            BuildAxisRuns(sourceWidthPixels, region.DestinationColumn, region.DestinationWidthPixels, columnRuns);
            BuildAxisRuns(sourceHeightRows, region.DestinationRow, region.DestinationHeightRows, rowRuns);

            DrawScaledBackdropInsideWorkBuffer(buffers[workBufferIndex],
                sourceBuffer,
                firstSourceColumnCursor,
                sourceWidthPixels,
                sourceHeightRows,
                horizontalStep,
                columnRuns,
                rowRuns);
            return;
        }

        DrawScaledBackdropWithRawAliasFallback(display,
            sourceBuffer,
            firstSourceColumnCursor,
            sourceWidthPixels,
            sourceHeightRows,
            horizontalStep,
            region,
            workBufferIndex);
    }

    private void DrawScaledBackdropInsideWorkBuffer(Surface workBuffer,
        byte[] sourceBuffer,
        int firstSourceColumnCursor,
        int sourceWidthPixels,
        int sourceHeightRows,
        int horizontalStep,
        ReadOnlySpan<AxisRun> columnRuns,
        ReadOnlySpan<AxisRun> rowRuns)
    {
        var wroteAnyPixel = false;
        var visibleMinColumn = int.MaxValue;
        var visibleMinRow = int.MaxValue;
        var visibleMaxColumn = int.MinValue;
        var visibleMaxRow = int.MinValue;

        var sourceColumnCursor = firstSourceColumnCursor;
        for (var sourceX = 0; sourceX < sourceWidthPixels; sourceX++, sourceColumnCursor += horizontalStep)
        {
            var columnRun = columnRuns[sourceX];
            if (columnRun.Count == 0)
            {
                continue;
            }

            var sourceRowCursor = sourceColumnCursor;
            for (var sourceY = 0; sourceY < sourceHeightRows; sourceY++, sourceRowCursor += SourceStridePixels)
            {
                var rowRun = rowRuns[sourceY];
                if (rowRun.Count == 0)
                {
                    continue;
                }

                var value = sourceBuffer[sourceRowCursor];
                if (value == 0)
                {
                    continue;
                }

                wroteAnyPixel = true;

                var startColumn = columnRun.Start;
                var startRow = rowRun.Start;
                var width = columnRun.Count;
                var height = rowRun.Count;
                var endColumn = startColumn + width - 1;
                var endRow = startRow + height - 1;

                if (startColumn < visibleMinColumn)
                {
                    visibleMinColumn = startColumn;
                }

                if (startRow < visibleMinRow)
                {
                    visibleMinRow = startRow;
                }

                if (endColumn > visibleMaxColumn)
                {
                    visibleMaxColumn = endColumn;
                }

                if (endRow > visibleMaxRow)
                {
                    visibleMaxRow = endRow;
                }

                if (width == 1)
                {
                    for (var destinationRow = startRow; destinationRow <= endRow; destinationRow++)
                    {
                        workBuffer.GetRowSpan(destinationRow)[startColumn] = value;
                    }
                }
                else
                {
                    for (var destinationRow = startRow; destinationRow <= endRow; destinationRow++)
                    {
                        workBuffer.GetRowSpan(destinationRow).Slice(startColumn, width).Fill(value);
                    }
                }
            }
        }

        if (!wroteAnyPixel)
        {
            return;
        }

        state.Presentation.Screen.Invalidate(new IntRect(visibleMinColumn,
            visibleMinRow,
            visibleMaxColumn - visibleMinColumn + 1,
            visibleMaxRow - visibleMinRow + 1));
    }

    private void DrawScaledBackdropWithRawAliasFallback(DisplayCompatibilityState display,
        byte[] sourceBuffer,
        int firstSourceColumnCursor,
        int sourceWidthPixels,
        int sourceHeightRows,
        int horizontalStep,
        ProgramSceneBackdropCopyRegion region,
        int workBufferIndex)
    {
        var expandWidth = region.DestinationWidthPixels > sourceWidthPixels;
        var expandHeight = region.DestinationHeightRows > sourceHeightRows;
        var widthDeltaThreshold = Math.Abs(sourceWidthPixels - region.DestinationWidthPixels) + 1;
        var heightDeltaThreshold = Math.Abs(sourceHeightRows - region.DestinationHeightRows) + 1;
        var wroteAnyRetainedBufferPixel = false;

        var widthAccumulator = sourceWidthPixels;
        var sourceColumnCursor = firstSourceColumnCursor;
        for (int remainingSourceColumns = sourceWidthPixels, destinationColumn = region.DestinationColumn;
             remainingSourceColumns > 0;
             remainingSourceColumns--, sourceColumnCursor += horizontalStep)
        {
            widthAccumulator -= widthDeltaThreshold;
            if (widthAccumulator < 0 && !expandWidth)
            {
                widthAccumulator += sourceWidthPixels;
                continue;
            }

            widthAccumulator -= sourceWidthPixels;
            do
            {
                var heightAccumulator = sourceHeightRows;
                var sourceRowCursor = sourceColumnCursor;
                for (int remainingSourceRows = sourceHeightRows, destinationRow = region.DestinationRow;
                     remainingSourceRows > 0;
                     remainingSourceRows--, sourceRowCursor += SourceStridePixels)
                {
                    heightAccumulator -= heightDeltaThreshold;
                    if (heightAccumulator < 0 && !expandHeight)
                    {
                        heightAccumulator += sourceHeightRows;
                        continue;
                    }

                    heightAccumulator -= sourceHeightRows;
                    var value = sourceBuffer[sourceRowCursor];
                    do
                    {
                        if (value != 0)
                        {
                            if (TryWriteBackdropDestinationPixel(display,
                                    workBufferIndex,
                                    destinationColumn,
                                    destinationRow,
                                    value))
                            {
                                wroteAnyRetainedBufferPixel = true;
                            }
                        }

                        destinationRow++;
                        heightAccumulator += sourceHeightRows;
                    } while (heightAccumulator < 0);
                }

                destinationColumn++;
                widthAccumulator += sourceWidthPixels;
            } while (widthAccumulator < 0);
        }

        if (wroteAnyRetainedBufferPixel)
        {
            state.Presentation.Screen.Invalidate(new IntRect(0,
                0,
                DisplayBufferWidthPixels,
                DisplayBufferHeightRows));
        }
    }

    private static void BuildAxisRuns(int sourceLength, int destinationStart, int destinationLength, Span<AxisRun> runs)
    {
        var expand = destinationLength > sourceLength;
        var deltaThreshold = Math.Abs(sourceLength - destinationLength) + 1;

        var accumulator = sourceLength;
        var destination = destinationStart;

        for (var source = 0; source < sourceLength; source++)
        {
            accumulator -= deltaThreshold;
            if (accumulator < 0 && !expand)
            {
                accumulator += sourceLength;
                runs[source] = default;
                continue;
            }

            accumulator -= sourceLength;

            var start = destination;
            var count = 0;
            do
            {
                count++;
                destination++;
                accumulator += sourceLength;
            } while (accumulator < 0);

            runs[source] = new AxisRun(start, count);
        }
    }

    private static void EnsureSupportedCopyBounds(byte[] sourceBuffer,
        int sourceStartIndex,
        int copyWidth,
        int copyHeight,
        Surface destinationSurface,
        int destinationX,
        int destinationY)
    {
        var sourceLimit = checked(sourceStartIndex + (copyHeight - 1) * SourceStridePixels + copyWidth);
        if (sourceStartIndex < 0 || sourceLimit > sourceBuffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceBuffer),
                "sub_10578 expects an in-bounds 320-byte-stride source region.");
        }

        var destinationRight = checked(destinationX + copyWidth);
        var destinationBottom = checked(destinationY + copyHeight);
        if (destinationX < 0 || destinationY < 0 || destinationRight > destinationSurface.Width ||
            destinationBottom > destinationSurface.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationSurface),
                "sub_10578 expects an in-bounds display destination region.");
        }
    }

    private static void EnsureSupportedTransparentCopyBounds(byte[] sourceBuffer,
        int sourceStartIndex,
        int copyWidth,
        int copyHeight,
        Surface destinationSurface,
        int destinationX,
        int destinationY)
    {
        if (sourceStartIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceBuffer),
                "sub_10637 expects a non-negative source origin after clipping.");
        }

        var sourceLimit = checked(sourceStartIndex + (copyHeight - 1) * SourceStridePixels + copyWidth);
        if (sourceLimit > sourceBuffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceBuffer),
                "sub_10637 expects an in-bounds 320-byte-stride source region after clipping.");
        }

        var destinationRight = checked(destinationX + copyWidth);
        var destinationBottom = checked(destinationY + copyHeight);
        if (destinationX < 0 || destinationY < 0 || destinationRight > destinationSurface.Width ||
            destinationBottom > destinationSurface.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationSurface),
                "sub_10637 expects an in-bounds work-buffer destination region after clipping.");
        }
    }

    private static void ValidateBackdropSourceBounds(byte[] sourceBuffer, ProgramSceneBackdropCopyRegion region)
    {
        if (region.SourceColumn < 0 || region.SourceRow < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(region),
                "sub_10776 expects a non-negative source origin inside the backdrop source surface.");
        }

        var sourceRight = checked(region.SourceColumn + region.SourceWidthPixels);
        var sourceBottom = checked(region.SourceRow + region.SourceHeightRows);
        if (sourceRight > SourceStridePixels || sourceBottom <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(region),
                "sub_10776 expects an in-bounds source rectangle inside the 320-byte-stride backdrop source surface.");
        }

        var sourceLimit = checked((sourceBottom - 1) * SourceStridePixels + sourceRight);
        if (sourceLimit > sourceBuffer.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceBuffer),
                "sub_10776 expects the source backdrop rectangle to fit within the backdrop source surface.");
        }
    }

    private static bool TryWriteBackdropDestinationPixel(DisplayCompatibilityState display,
        int workBufferIndex,
        int destinationColumn,
        int destinationRow,
        byte value)
    {
        const int bufferPixelCount = DisplayBufferWidthPixels * DisplayBufferHeightRows;
        var absolutePixelIndex = workBufferIndex * bufferPixelCount + destinationRow * DisplayBufferWidthPixels +
                                 destinationColumn;
        var totalPixelCount = display.RetainedDisplayBuffers.Length * bufferPixelCount;
        if ((uint)absolutePixelIndex >= (uint)totalPixelCount)
        {
            return false;
        }

        var resolvedBufferIndex = absolutePixelIndex / bufferPixelCount;
        var pixelIndexWithinBuffer = absolutePixelIndex % bufferPixelCount;
        var resolvedRow = pixelIndexWithinBuffer / DisplayBufferWidthPixels;
        var resolvedColumn = pixelIndexWithinBuffer % DisplayBufferWidthPixels;
        display.RetainedDisplayBuffers[resolvedBufferIndex].GetRowSpan(resolvedRow)[resolvedColumn] = value;
        return true;
    }

    private static void EnsureSupportedDisplayRegion(Surface sourceSurface,
        Surface destinationSurface,
        IntRect sourceRect)
    {
        if (sourceRect.X < 0 || sourceRect.Y < 0 || sourceRect.Right > sourceSurface.Width ||
            sourceRect.Bottom > sourceSurface.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceSurface),
                "sub_104A4 expects an in-bounds retained snapshot source region.");
        }

        if (sourceRect.X < 0 || sourceRect.Y < 0 || sourceRect.Right > destinationSurface.Width ||
            sourceRect.Bottom > destinationSurface.Height)
        {
            throw new ArgumentOutOfRangeException(nameof(destinationSurface),
                "sub_104A4 expects an in-bounds work-buffer destination region.");
        }
    }

    private static IntRect GetPlanarAlignedRegion(ushort column, ushort row, ushort widthPixels, ushort heightRows)
    {
        var alignedLeft = column & ~0x3;
        var planarByteWidth = (((column & 0x3) + widthPixels + 0x3) >> 2) << 2;
        return new IntRect(alignedLeft, row, planarByteWidth, heightRows);
    }

    private readonly struct AxisRun(int start, int count)
    {
        internal int Start { get; } = start;

        internal int Count { get; } = count;
    }
}
