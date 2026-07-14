using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Game.Shared.Rendering;

/// <summary>
///     Low-level surface blit and fill helpers.
/// </summary>
/// <remarks>
///     Operations that take rectangles perform clipping against surface bounds. When the clipped region is empty,
///     the operation is a no-op.
/// </remarks>
public static class Blitter
{
    /// <summary>
    ///     Clears an indexed 8-bit surface with a palette index value.
    /// </summary>
    /// <param name="surface">The destination surface.</param>
    /// <param name="value">The palette index to fill with.</param>
    /// <exception cref="ArgumentException"><paramref name="surface" /> is not an indexed 8-bit surface.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Clear(Surface surface, byte value)
    {
        ValidateFormat(surface, PixelFormat.Indexed8);
        surface.GetPixelSpan().Fill(value);
    }

    /// <summary>
    ///     Clears an RGBA surface with a color.
    /// </summary>
    /// <param name="surface">The destination surface.</param>
    /// <param name="color">The color to fill with.</param>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Clear(Surface surface, Rgba32 color)
    {
        FillRect(surface, new IntRect(0, 0, surface.Width, surface.Height), color);
    }

    /// <summary>
    ///     Copies pixels from <paramref name="source" /> to <paramref name="destination" /> with clipping.
    /// </summary>
    /// <param name="source">The source surface.</param>
    /// <param name="sourceRect">The source rectangle.</param>
    /// <param name="destination">The destination surface.</param>
    /// <param name="destinationX">The X position in the destination where the source rectangle is placed.</param>
    /// <param name="destinationY">The Y position in the destination where the source rectangle is placed.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="destination" /> is
    ///     <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Source and destination formats do not match.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void Copy(Surface source, IntRect sourceRect, Surface destination, int destinationX, int destinationY)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);

        if (source.Format != destination.Format)
        {
            throw new ArgumentException("Copy requires source and destination surfaces to use the same format.",
                nameof(destination));
        }

        if (!TryClipBlit(source,
                sourceRect,
                destination,
                destinationX,
                destinationY,
                out var clippedSource,
                out var clippedDestination))
        {
            return;
        }

        CopyClipped(source, destination, clippedSource, clippedDestination);
    }

    /// <summary>
    ///     Copies indexed pixels from a row-strided source buffer into an indexed destination surface with clipping.
    /// </summary>
    /// <param name="source">The indexed source buffer.</param>
    /// <param name="sourceStride">The source row stride in bytes.</param>
    /// <param name="sourceRect">The source rectangle.</param>
    /// <param name="destination">The indexed destination surface.</param>
    /// <param name="destinationX">The X position in the destination where the source rectangle is placed.</param>
    /// <param name="destinationY">The Y position in the destination where the source rectangle is placed.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sourceStride" /> is not positive.</exception>
    /// <exception cref="ArgumentException"><paramref name="destination" /> is not an indexed 8-bit surface.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void CopyIndexed8(ReadOnlySpan<byte> source,
        int sourceStride,
        IntRect sourceRect,
        Surface destination,
        int destinationX,
        int destinationY)
    {
        ValidateFormat(destination, PixelFormat.Indexed8);
        ValidateSourceStride(sourceStride);

        if (!TryClipBufferBlit(source,
                sourceStride,
                sourceRect,
                destination,
                destinationX,
                destinationY,
                out var clippedSource,
                out var clippedDestination))
        {
            return;
        }

        var destinationPixels = destination.GetPixelSpan();
        var buffersOverlap = source.Overlaps(destinationPixels, out var destinationBaseOffset);
        if (ShouldCopyBufferRowsBottomUp(buffersOverlap,
                destinationBaseOffset,
                sourceStride,
                clippedSource,
                destination.Stride,
                clippedDestination))
        {
            for (var row = clippedSource.Height - 1; row >= 0; row--)
            {
                CopyIndexed8BufferRow(source,
                    sourceStride,
                    clippedSource,
                    destinationPixels,
                    destination.Stride,
                    clippedDestination,
                    row);
            }

            return;
        }

        for (var row = 0; row < clippedSource.Height; row++)
        {
            CopyIndexed8BufferRow(source,
                sourceStride,
                clippedSource,
                destinationPixels,
                destination.Stride,
                clippedDestination,
                row);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopyIndexed8BufferRow(ReadOnlySpan<byte> source,
        int sourceStride,
        IntRect clippedSource,
        Span<byte> destinationPixels,
        int destinationStride,
        IntRect clippedDestination,
        int row)
    {
        var sourceOffset = checked((clippedSource.Y + row) * sourceStride + clippedSource.X);
        var destinationOffset = checked((clippedDestination.Y + row) * destinationStride + clippedDestination.X);
        source.Slice(sourceOffset, clippedSource.Width)
            .CopyTo(destinationPixels.Slice(destinationOffset, clippedDestination.Width));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool ShouldCopyBufferRowsBottomUp(bool buffersOverlap,
        int destinationBaseOffset,
        int sourceStride,
        IntRect clippedSource,
        int destinationStride,
        IntRect clippedDestination)
    {
        if (!buffersOverlap)
        {
            return false;
        }

        var sourceStart = (long)clippedSource.Y * sourceStride + clippedSource.X;
        var sourceEnd = (long)(clippedSource.Y + clippedSource.Height - 1) * sourceStride + clippedSource.X +
                        clippedSource.Width;
        var destinationStart = destinationBaseOffset + (long)clippedDestination.Y * destinationStride +
                               clippedDestination.X;

        return destinationStart > sourceStart && destinationStart < sourceEnd;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopyClipped(Surface source,
        Surface destination,
        IntRect clippedSource,
        IntRect clippedDestination)
    {
        if (ReferenceEquals(source, destination) && clippedSource.X == clippedDestination.X &&
            clippedSource.Y == clippedDestination.Y)
        {
            return;
        }

        if (CanCopyAsContiguousRows(source, destination, clippedSource, clippedDestination))
        {
            CopyContiguousRows(source, destination, clippedSource, clippedDestination);
            return;
        }

        CopyRows(source, destination, clippedSource, clippedDestination);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool CanCopyAsContiguousRows(Surface source,
        Surface destination,
        IntRect clippedSource,
        IntRect clippedDestination)
    {
        return source.Stride == destination.Stride && source.Width == destination.Width && clippedSource.X == 0 &&
               clippedDestination.X == 0 && clippedSource.Width == source.Width &&
               clippedDestination.Width == destination.Width;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopyContiguousRows(Surface source,
        Surface destination,
        IntRect clippedSource,
        IntRect clippedDestination)
    {
        var totalBytes = source.Stride * clippedSource.Height;
        source.GetReadOnlyPixelSpan().Slice(clippedSource.Y * source.Stride, totalBytes)
            .CopyTo(destination.GetPixelSpan().Slice(clippedDestination.Y * destination.Stride, totalBytes));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopyRows(Surface source, Surface destination, IntRect clippedSource, IntRect clippedDestination)
    {
        var bytesPerPixel = source.BytesPerPixel;
        var sourceXBytes = clippedSource.X * bytesPerPixel;
        var destinationXBytes = clippedDestination.X * bytesPerPixel;
        var rowWidth = clippedSource.Width * bytesPerPixel;
        var sourcePixels = source.GetReadOnlyPixelSpan();
        var destinationPixels = destination.GetPixelSpan();
        var copyBottomUp = ReferenceEquals(source, destination) && clippedDestination.Y > clippedSource.Y &&
                           clippedDestination.Y < clippedSource.Bottom;

        if (copyBottomUp)
        {
            for (var row = clippedSource.Height - 1; row >= 0; row--)
            {
                var sourceOffset = (clippedSource.Y + row) * source.Stride + sourceXBytes;
                var destinationOffset = (clippedDestination.Y + row) * destination.Stride + destinationXBytes;
                sourcePixels.Slice(sourceOffset, rowWidth).CopyTo(destinationPixels.Slice(destinationOffset, rowWidth));
            }

            return;
        }

        for (var row = 0; row < clippedSource.Height; row++)
        {
            var sourceOffset = (clippedSource.Y + row) * source.Stride + sourceXBytes;
            var destinationOffset = (clippedDestination.Y + row) * destination.Stride + destinationXBytes;
            sourcePixels.Slice(sourceOffset, rowWidth).CopyTo(destinationPixels.Slice(destinationOffset, rowWidth));
        }
    }

    /// <summary>
    ///     Converts an entire indexed 8-bit surface to an RGBA surface using a 256-entry palette.
    /// </summary>
    /// <param name="source">The indexed source surface.</param>
    /// <param name="palette">A 256-entry palette.</param>
    /// <param name="destination">The RGBA destination surface.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="destination" /> is
    ///     <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Source/destination formats or dimensions are invalid, or the palette is too small.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void ConvertIndexed8ToRgba32(Surface source, ReadOnlySpan<Rgba32> palette, Surface destination)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ValidateFormat(source, PixelFormat.Indexed8);
        ValidateFormat(destination, PixelFormat.Rgba32);

        if (source.Width != destination.Width || source.Height != destination.Height)
        {
            throw new ArgumentException("Indexed conversion requires matching source and destination dimensions.",
                nameof(destination));
        }

        if (palette.Length < byte.MaxValue + 1)
        {
            throw new ArgumentException("Indexed conversion requires a 256-entry palette.", nameof(palette));
        }

        ConvertIndexed8ToRgba32(source, new IntRect(0, 0, source.Width, source.Height), palette, destination, 0, 0);
    }

    /// <summary>
    ///     Converts a rectangular region of an indexed surface to RGBA and writes it into a destination surface.
    /// </summary>
    /// <param name="source">The indexed source surface.</param>
    /// <param name="sourceRect">The source rectangle.</param>
    /// <param name="palette">A 256-entry palette.</param>
    /// <param name="destination">The RGBA destination surface.</param>
    /// <param name="destinationX">The X position in the destination where the converted region is placed.</param>
    /// <param name="destinationY">The Y position in the destination where the converted region is placed.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="destination" /> is
    ///     <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Source/destination formats are invalid or the palette is too small.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void ConvertIndexed8ToRgba32(Surface source,
        IntRect sourceRect,
        ReadOnlySpan<Rgba32> palette,
        Surface destination,
        int destinationX,
        int destinationY)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ValidateFormat(source, PixelFormat.Indexed8);
        ValidateFormat(destination, PixelFormat.Rgba32);

        if (palette.Length < byte.MaxValue + 1)
        {
            throw new ArgumentException("Indexed conversion requires a 256-entry palette.", nameof(palette));
        }

        if (!TryClipBlit(source,
                sourceRect,
                destination,
                destinationX,
                destinationY,
                out var clippedSource,
                out var clippedDestination))
        {
            return;
        }

        var palette32 = MemoryMarshal.Cast<Rgba32, uint>(palette);
        if (CanConvertFullSurface(source, destination, clippedSource, clippedDestination))
        {
            ConvertIndexed8ToRgba32Pixels(source.GetReadOnlyPixelSpan(),
                MemoryMarshal.Cast<byte, uint>(destination.GetPixelSpan()),
                palette32);
            return;
        }

        for (var row = 0; row < clippedSource.Height; row++)
        {
            var sourceRow = source.GetReadOnlyRowSpan(clippedSource.Y + row)
                .Slice(clippedSource.X, clippedSource.Width);
            var destinationRow = MemoryMarshal.Cast<byte, uint>(destination.GetRowSpan(clippedDestination.Y + row))
                .Slice(clippedDestination.X, clippedDestination.Width);

            ConvertIndexed8ToRgba32Pixels(sourceRow, destinationRow, palette32);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void ConvertIndexed8ToRgba32Pixels(ReadOnlySpan<byte> sourcePixels,
        Span<uint> destinationPixels,
        ReadOnlySpan<uint> palette)
    {
        var length = sourcePixels.Length;
        if (length == 0)
        {
            return;
        }

        ref var source = ref MemoryMarshal.GetReference(sourcePixels);
        ref var destination = ref MemoryMarshal.GetReference(destinationPixels);
        ref var paletteRef = ref MemoryMarshal.GetReference(palette);

        var offset = 0;
        var lastUnrolledStart = length - 4;
        for (; offset <= lastUnrolledStart; offset += 4)
        {
            Unsafe.Add(ref destination, offset) = Unsafe.Add(ref paletteRef, Unsafe.Add(ref source, offset));
            Unsafe.Add(ref destination, offset + 1) = Unsafe.Add(ref paletteRef, Unsafe.Add(ref source, offset + 1));
            Unsafe.Add(ref destination, offset + 2) = Unsafe.Add(ref paletteRef, Unsafe.Add(ref source, offset + 2));
            Unsafe.Add(ref destination, offset + 3) = Unsafe.Add(ref paletteRef, Unsafe.Add(ref source, offset + 3));
        }

        for (; offset < length; offset++)
        {
            Unsafe.Add(ref destination, offset) = Unsafe.Add(ref paletteRef, Unsafe.Add(ref source, offset));
        }
    }

    private static bool CanConvertFullSurface(Surface source,
        Surface destination,
        IntRect clippedSource,
        IntRect clippedDestination)
    {
        return clippedSource is { X: 0, Y: 0 } && clippedDestination is { X: 0, Y: 0 } &&
               clippedSource.Width == source.Width && clippedSource.Height == source.Height &&
               clippedDestination.Width == destination.Width && clippedDestination.Height == destination.Height &&
               source.Stride == source.Width && destination.Stride == destination.Width * destination.BytesPerPixel;
    }

    /// <summary>
    ///     Fills a rectangle on an indexed 8-bit surface with a palette index.
    /// </summary>
    /// <param name="surface">The destination surface.</param>
    /// <param name="rect">The rectangle to fill.</param>
    /// <param name="value">The palette index to fill with.</param>
    /// <exception cref="ArgumentNullException"><paramref name="surface" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="surface" /> is not an indexed 8-bit surface.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void FillRect(Surface surface, IntRect rect, byte value)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ValidateFormat(surface, PixelFormat.Indexed8);
        if (!TryClipRect(surface, rect, out var clipped))
        {
            return;
        }

        for (var row = clipped.Y; row < clipped.Bottom; row++)
        {
            surface.GetRowSpan(row).Slice(clipped.X, clipped.Width).Fill(value);
        }
    }

    /// <summary>
    ///     Fills a rectangle on an RGBA surface with a color.
    /// </summary>
    /// <param name="surface">The destination surface.</param>
    /// <param name="rect">The rectangle to fill.</param>
    /// <param name="color">The fill color.</param>
    /// <exception cref="ArgumentNullException"><paramref name="surface" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="surface" /> is not an RGBA surface.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void FillRect(Surface surface, IntRect rect, Rgba32 color)
    {
        ArgumentNullException.ThrowIfNull(surface);
        ValidateFormat(surface, PixelFormat.Rgba32);
        if (!TryClipRect(surface, rect, out var clipped))
        {
            return;
        }

        for (var row = clipped.Y; row < clipped.Bottom; row++)
        {
            var destinationRow = MemoryMarshal.Cast<byte, Rgba32>(surface.GetRowSpan(row))
                .Slice(clipped.X, clipped.Width);
            destinationRow.Fill(color);
        }
    }

    /// <summary>
    ///     Copies indexed pixels while treating a specific palette index as transparent.
    /// </summary>
    /// <param name="source">The indexed source surface.</param>
    /// <param name="sourceRect">The source rectangle.</param>
    /// <param name="destination">The indexed destination surface.</param>
    /// <param name="destinationX">The X position in the destination where the source rectangle is placed.</param>
    /// <param name="destinationY">The Y position in the destination where the source rectangle is placed.</param>
    /// <param name="transparentIndex">The palette index to treat as transparent.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="source" /> or <paramref name="destination" /> is
    ///     <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">Source/destination formats are not indexed 8-bit.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void KeyedCopyIndexed8(Surface source,
        IntRect sourceRect,
        Surface destination,
        int destinationX,
        int destinationY,
        byte transparentIndex)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(destination);
        ValidateFormat(source, PixelFormat.Indexed8);
        ValidateFormat(destination, PixelFormat.Indexed8);

        if (!TryClipBlit(source,
                sourceRect,
                destination,
                destinationX,
                destinationY,
                out var clippedSource,
                out var clippedDestination))
        {
            return;
        }

        var copyBottomUp = ReferenceEquals(source, destination) && clippedDestination.Y > clippedSource.Y &&
                           clippedDestination.Y < clippedSource.Bottom;

        if (copyBottomUp)
        {
            for (var row = clippedSource.Height - 1; row >= 0; row--)
            {
                CopyKeyedIndexed8Row(source, destination, clippedSource, clippedDestination, row, transparentIndex);
            }

            return;
        }

        for (var row = 0; row < clippedSource.Height; row++)
        {
            CopyKeyedIndexed8Row(source, destination, clippedSource, clippedDestination, row, transparentIndex);
        }
    }

    /// <summary>
    ///     Copies indexed pixels from a row-strided source buffer into an indexed destination surface while treating a
    ///     specific palette index as transparent.
    /// </summary>
    /// <param name="source">The indexed source buffer.</param>
    /// <param name="sourceStride">The source row stride in bytes.</param>
    /// <param name="sourceRect">The source rectangle.</param>
    /// <param name="destination">The indexed destination surface.</param>
    /// <param name="destinationX">The X position in the destination where the source rectangle is placed.</param>
    /// <param name="destinationY">The Y position in the destination where the source rectangle is placed.</param>
    /// <param name="transparentIndex">The palette index to treat as transparent.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="sourceStride" /> is not positive.</exception>
    /// <exception cref="ArgumentException"><paramref name="destination" /> is not an indexed 8-bit surface.</exception>
    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    public static void KeyedCopyIndexed8(ReadOnlySpan<byte> source,
        int sourceStride,
        IntRect sourceRect,
        Surface destination,
        int destinationX,
        int destinationY,
        byte transparentIndex)
    {
        ValidateFormat(destination, PixelFormat.Indexed8);
        ValidateSourceStride(sourceStride);

        if (!TryClipBufferBlit(source,
                sourceStride,
                sourceRect,
                destination,
                destinationX,
                destinationY,
                out var clippedSource,
                out var clippedDestination))
        {
            return;
        }

        var destinationPixels = destination.GetPixelSpan();
        var buffersOverlap = source.Overlaps(destinationPixels, out var destinationBaseOffset);
        if (ShouldCopyBufferRowsBottomUp(buffersOverlap,
                destinationBaseOffset,
                sourceStride,
                clippedSource,
                destination.Stride,
                clippedDestination))
        {
            for (var row = clippedSource.Height - 1; row >= 0; row--)
            {
                CopyKeyedIndexed8BufferRow(source,
                    sourceStride,
                    clippedSource,
                    destinationPixels,
                    destination.Stride,
                    clippedDestination,
                    row,
                    buffersOverlap,
                    transparentIndex);
            }

            return;
        }

        for (var row = 0; row < clippedSource.Height; row++)
        {
            CopyKeyedIndexed8BufferRow(source,
                sourceStride,
                clippedSource,
                destinationPixels,
                destination.Stride,
                clippedDestination,
                row,
                buffersOverlap,
                transparentIndex);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CopyKeyedIndexed8BufferRow(ReadOnlySpan<byte> source,
        int sourceStride,
        IntRect clippedSource,
        Span<byte> destinationPixels,
        int destinationStride,
        IntRect clippedDestination,
        int row,
        bool buffersOverlap,
        byte transparentIndex)
    {
        var sourceOffset = checked((clippedSource.Y + row) * sourceStride + clippedSource.X);
        var destinationOffset = checked((clippedDestination.Y + row) * destinationStride + clippedDestination.X);
        var sourceRow = source.Slice(sourceOffset, clippedSource.Width);
        var destinationRow = destinationPixels.Slice(destinationOffset, clippedDestination.Width);
        if (!buffersOverlap)
        {
            CopyKeyedIndexed8RowLeftToRightNoOverlap(sourceRow, destinationRow, transparentIndex);
            return;
        }

        if (sourceRow.Overlaps(destinationRow, out var destinationOffsetWithinRow) && destinationOffsetWithinRow > 0)
        {
            CopyKeyedIndexed8RowRightToLeft(sourceRow, destinationRow, transparentIndex);
            return;
        }

        CopyKeyedIndexed8RowLeftToRight(sourceRow, destinationRow, transparentIndex);
    }

    private static void CopyKeyedIndexed8Row(Surface source,
        Surface destination,
        IntRect clippedSource,
        IntRect clippedDestination,
        int row,
        byte transparentIndex)
    {
        var sourceY = clippedSource.Y + row;
        var destinationY = clippedDestination.Y + row;
        var sourceRow = source.GetReadOnlyRowSpan(sourceY).Slice(clippedSource.X, clippedSource.Width);
        var destinationRow = destination.GetRowSpan(destinationY).Slice(clippedDestination.X, clippedDestination.Width);
        var copyRightToLeft = ReferenceEquals(source, destination) && sourceY == destinationY &&
                              clippedDestination.X > clippedSource.X && clippedDestination.X < clippedSource.Right;

        if (copyRightToLeft)
        {
            CopyKeyedIndexed8RowRightToLeft(sourceRow, destinationRow, transparentIndex);
            return;
        }

        CopyKeyedIndexed8RowLeftToRight(sourceRow, destinationRow, transparentIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void CopyKeyedIndexed8RowLeftToRight(ReadOnlySpan<byte> sourceRow,
        Span<byte> destinationRow,
        byte transparentIndex)
    {
        var length = sourceRow.Length;
        if (length == 0)
        {
            return;
        }

        CopyKeyedIndexed8RowLeftToRightNoOverlap(sourceRow, destinationRow, transparentIndex);
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void CopyKeyedIndexed8RowLeftToRightNoOverlap(ReadOnlySpan<byte> sourceRow,
        Span<byte> destinationRow,
        byte transparentIndex)
    {
        var length = sourceRow.Length;
        if (length == 0)
        {
            return;
        }

        ref var source = ref MemoryMarshal.GetReference(sourceRow);
        ref var destination = ref MemoryMarshal.GetReference(destinationRow);

        var offset = 0;
        if (Vector512.IsHardwareAccelerated && length >= Vector512<byte>.Count)
        {
            offset = CopyKeyedIndexed8RowVector512(ref source, ref destination, length, transparentIndex);
        }
        else if (Avx2.IsSupported && length >= Vector256<byte>.Count)
        {
            offset = CopyKeyedIndexed8RowAvx2(ref source, ref destination, length, transparentIndex);
        }
        else if (Vector128.IsHardwareAccelerated && length >= Vector128<byte>.Count)
        {
            offset = CopyKeyedIndexed8RowVector128(ref source, ref destination, length, transparentIndex);
        }

        for (; offset < length; offset++)
        {
            var value = Unsafe.Add(ref source, offset);
            if (value != transparentIndex)
            {
                Unsafe.Add(ref destination, offset) = value;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization)]
    private static void CopyKeyedIndexed8RowRightToLeft(ReadOnlySpan<byte> sourceRow,
        Span<byte> destinationRow,
        byte transparentIndex)
    {
        var length = sourceRow.Length;
        if (length == 0)
        {
            return;
        }

        ref var source = ref MemoryMarshal.GetReference(sourceRow);
        ref var destination = ref MemoryMarshal.GetReference(destinationRow);

        var remaining = length;
        if (Vector512.IsHardwareAccelerated && length >= Vector512<byte>.Count)
        {
            remaining = CopyKeyedIndexed8RowVector512Backward(ref source, ref destination, length, transparentIndex);
        }
        else if (Avx2.IsSupported && length >= Vector256<byte>.Count)
        {
            remaining = CopyKeyedIndexed8RowAvx2Backward(ref source, ref destination, length, transparentIndex);
        }
        else if (Vector128.IsHardwareAccelerated && length >= Vector128<byte>.Count)
        {
            remaining = CopyKeyedIndexed8RowVector128Backward(ref source, ref destination, length, transparentIndex);
        }

        for (var column = remaining - 1; column >= 0; column--)
        {
            var value = Unsafe.Add(ref source, column);
            if (value != transparentIndex)
            {
                Unsafe.Add(ref destination, column) = value;
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyKeyedIndexed8RowVector512(ref byte source,
        ref byte destination,
        int length,
        byte transparentIndex)
    {
        var offset = 0;
        var last = length - Vector512<byte>.Count;
        var transparent = Vector512.Create(transparentIndex);

        for (; offset <= last; offset += Vector512<byte>.Count)
        {
            var sourceVector = Vector512.LoadUnsafe(ref source, (nuint)offset);
            if (!Vector512.EqualsAny(sourceVector, transparent))
            {
                sourceVector.StoreUnsafe(ref destination, (nuint)offset);
                continue;
            }

            if (Vector512.EqualsAll(sourceVector, transparent))
            {
                continue;
            }

            var destinationVector = Vector512.LoadUnsafe(ref destination, (nuint)offset);
            Vector512.ConditionalSelect(Vector512.Equals(sourceVector, transparent), destinationVector, sourceVector)
                .StoreUnsafe(ref destination, (nuint)offset);
        }

        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyKeyedIndexed8RowVector512Backward(ref byte source,
        ref byte destination,
        int length,
        byte transparentIndex)
    {
        var offset = length;
        var transparent = Vector512.Create(transparentIndex);

        while (offset >= Vector512<byte>.Count)
        {
            offset -= Vector512<byte>.Count;

            var sourceVector = Vector512.LoadUnsafe(ref source, (nuint)offset);
            if (!Vector512.EqualsAny(sourceVector, transparent))
            {
                sourceVector.StoreUnsafe(ref destination, (nuint)offset);
                continue;
            }

            if (Vector512.EqualsAll(sourceVector, transparent))
            {
                continue;
            }

            var destinationVector = Vector512.LoadUnsafe(ref destination, (nuint)offset);
            Vector512.ConditionalSelect(Vector512.Equals(sourceVector, transparent), destinationVector, sourceVector)
                .StoreUnsafe(ref destination, (nuint)offset);
        }

        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyKeyedIndexed8RowAvx2(ref byte source,
        ref byte destination,
        int length,
        byte transparentIndex)
    {
        var offset = 0;
        var last = length - Vector256<byte>.Count;
        var transparent = Vector256.Create(transparentIndex);

        for (; offset <= last; offset += Vector256<byte>.Count)
        {
            var sourceVector = Vector256.LoadUnsafe(ref source, (nuint)offset);
            var transparentMask = Vector256.Equals(sourceVector, transparent);
            var bits = Avx2.MoveMask(transparentMask);
            if (bits == 0)
            {
                sourceVector.StoreUnsafe(ref destination, (nuint)offset);
            }
            else if (bits != -1)
            {
                var destinationVector = Vector256.LoadUnsafe(ref destination, (nuint)offset);
                Avx2.BlendVariable(sourceVector, destinationVector, transparentMask)
                    .StoreUnsafe(ref destination, (nuint)offset);
            }
        }

        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyKeyedIndexed8RowAvx2Backward(ref byte source,
        ref byte destination,
        int length,
        byte transparentIndex)
    {
        var offset = length;
        var transparent = Vector256.Create(transparentIndex);

        while (offset >= Vector256<byte>.Count)
        {
            offset -= Vector256<byte>.Count;

            var sourceVector = Vector256.LoadUnsafe(ref source, (nuint)offset);
            var transparentMask = Vector256.Equals(sourceVector, transparent);
            var bits = Avx2.MoveMask(transparentMask);
            if (bits == 0)
            {
                sourceVector.StoreUnsafe(ref destination, (nuint)offset);
            }
            else if (bits != -1)
            {
                var destinationVector = Vector256.LoadUnsafe(ref destination, (nuint)offset);
                Avx2.BlendVariable(sourceVector, destinationVector, transparentMask)
                    .StoreUnsafe(ref destination, (nuint)offset);
            }
        }

        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyKeyedIndexed8RowVector128(ref byte source,
        ref byte destination,
        int length,
        byte transparentIndex)
    {
        var offset = 0;
        var last = length - Vector128<byte>.Count;
        var transparent = Vector128.Create(transparentIndex);

        for (; offset <= last; offset += Vector128<byte>.Count)
        {
            var sourceVector = Vector128.LoadUnsafe(ref source, (nuint)offset);
            if (!Vector128.EqualsAny(sourceVector, transparent))
            {
                sourceVector.StoreUnsafe(ref destination, (nuint)offset);
                continue;
            }

            if (Vector128.EqualsAll(sourceVector, transparent))
            {
                continue;
            }

            var destinationVector = Vector128.LoadUnsafe(ref destination, (nuint)offset);
            Vector128.ConditionalSelect(Vector128.Equals(sourceVector, transparent), destinationVector, sourceVector)
                .StoreUnsafe(ref destination, (nuint)offset);
        }

        return offset;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int CopyKeyedIndexed8RowVector128Backward(ref byte source,
        ref byte destination,
        int length,
        byte transparentIndex)
    {
        var offset = length;
        var transparent = Vector128.Create(transparentIndex);

        while (offset >= Vector128<byte>.Count)
        {
            offset -= Vector128<byte>.Count;

            var sourceVector = Vector128.LoadUnsafe(ref source, (nuint)offset);
            if (!Vector128.EqualsAny(sourceVector, transparent))
            {
                sourceVector.StoreUnsafe(ref destination, (nuint)offset);
                continue;
            }

            if (Vector128.EqualsAll(sourceVector, transparent))
            {
                continue;
            }

            var destinationVector = Vector128.LoadUnsafe(ref destination, (nuint)offset);
            Vector128.ConditionalSelect(Vector128.Equals(sourceVector, transparent), destinationVector, sourceVector)
                .StoreUnsafe(ref destination, (nuint)offset);
        }

        return offset;
    }

    private static bool TryClipBlit(Surface source,
        IntRect sourceRect,
        Surface destination,
        int destinationX,
        int destinationY,
        out IntRect clippedSource,
        out IntRect clippedDestination)
    {
        clippedSource = default;
        clippedDestination = default;

        if (!TryClipRect(source, sourceRect, out var sourceBounds))
        {
            return false;
        }

        var sourceClipOffsetX = sourceBounds.X - sourceRect.X;
        var sourceClipOffsetY = sourceBounds.Y - sourceRect.Y;
        var destinationRect = new IntRect(destinationX + sourceClipOffsetX,
            destinationY + sourceClipOffsetY,
            sourceBounds.Width,
            sourceBounds.Height);
        if (!TryClipRect(destination, destinationRect, out var destinationBounds))
        {
            return false;
        }

        var deltaX = destinationBounds.X - destinationRect.X;
        var deltaY = destinationBounds.Y - destinationRect.Y;
        var clippedWidth = Math.Min(destinationBounds.Width, sourceBounds.Width - deltaX);
        var clippedHeight = Math.Min(destinationBounds.Height, sourceBounds.Height - deltaY);

        clippedSource = new IntRect(sourceBounds.X + deltaX, sourceBounds.Y + deltaY, clippedWidth, clippedHeight);
        clippedDestination = new IntRect(destinationBounds.X, destinationBounds.Y, clippedWidth, clippedHeight);
        return true;
    }

    private static bool TryClipBufferBlit(ReadOnlySpan<byte> source,
        int sourceStride,
        IntRect sourceRect,
        Surface destination,
        int destinationX,
        int destinationY,
        out IntRect clippedSource,
        out IntRect clippedDestination)
    {
        clippedSource = default;
        clippedDestination = default;

        var sourceHeight = source.Length / sourceStride;
        if (!TryClipRect(sourceStride, sourceHeight, sourceRect, out var sourceBounds))
        {
            return false;
        }

        var sourceClipOffsetX = sourceBounds.X - sourceRect.X;
        var sourceClipOffsetY = sourceBounds.Y - sourceRect.Y;
        var destinationRect = new IntRect(destinationX + sourceClipOffsetX,
            destinationY + sourceClipOffsetY,
            sourceBounds.Width,
            sourceBounds.Height);
        if (!TryClipRect(destination, destinationRect, out var destinationBounds))
        {
            return false;
        }

        var deltaX = destinationBounds.X - destinationRect.X;
        var deltaY = destinationBounds.Y - destinationRect.Y;
        var clippedWidth = Math.Min(destinationBounds.Width, sourceBounds.Width - deltaX);
        var clippedHeight = Math.Min(destinationBounds.Height, sourceBounds.Height - deltaY);

        clippedSource = new IntRect(sourceBounds.X + deltaX, sourceBounds.Y + deltaY, clippedWidth, clippedHeight);
        clippedDestination = new IntRect(destinationBounds.X, destinationBounds.Y, clippedWidth, clippedHeight);
        return true;
    }

    private static bool TryClipRect(Surface surface, IntRect rect, out IntRect clipped)
    {
        ArgumentNullException.ThrowIfNull(surface);
        return TryClipRect(surface.Width, surface.Height, rect, out clipped);
    }

    private static bool TryClipRect(int width, int height, IntRect rect, out IntRect clipped)
    {
        clipped = default;

        if (rect.IsEmpty)
        {
            return false;
        }

        var left = Math.Clamp(rect.X, 0, width);
        var top = Math.Clamp(rect.Y, 0, height);
        var right = Math.Clamp(rect.Right, 0, width);
        var bottom = Math.Clamp(rect.Bottom, 0, height);

        if (right <= left || bottom <= top)
        {
            return false;
        }

        clipped = new IntRect(left, top, right - left, bottom - top);
        return true;
    }

    private static void ValidateSourceStride(int sourceStride)
    {
        if (sourceStride <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sourceStride), "Source stride must be positive.");
        }
    }

    private static void ValidateFormat(Surface surface, PixelFormat expected)
    {
        ArgumentNullException.ThrowIfNull(surface);
        if (surface.Format != expected)
        {
            throw new ArgumentException($"Surface must use {expected} pixels.", nameof(surface));
        }
    }
}
