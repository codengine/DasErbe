using Game.Shared.Formats.Containers;
using Game.Shared.IO;
using Game.Shared.Palette;
using Game.Shared.RE;
using Game.State;

namespace Game.Assets;

/// <summary>
///     Decodes the game's FORM-based image resources.
/// </summary>
/// <param name="state">Live runtime state.</param>
internal sealed class LbmDecoder(RuntimeState state)
{
    private const int PaletteByteCount = ColorPalette.EntryCount * 3;
    private const int PlaneCount = 8;

    private static readonly ChunkFileReaderOptions FormChunkLayout = new(4, 4, false, 2);

    /// <summary>
    ///     Decodes one loaded image resource into the target buffer and updates the runtime DAC table when present.
    /// </summary>
    /// <param name="sourceBuffer">Loaded image resource bytes.</param>
    /// <param name="destinationBuffer">Decode target buffer.</param>
    /// <param name="destinationStrideBytes">Row stride of the decode target buffer.</param>
    [FunctionSymbol("sub_10CF3", 0x10CF3)]
    internal void DecodeIntoBuffer(byte[] sourceBuffer, byte[] destinationBuffer, int destinationStrideBytes)
    {
        // IDA 0x10CF3..0x10D80: classify the FORM type and locate the BMHD / CMAP chunks by
        // scanning forward in word-aligned steps through the loaded resource buffer.
        var resource = ParseResource(sourceBuffer);

        // IDA 0x10DC6..0x10DDF: publish the CMAP into the retained DAC backing table. The live presentation palette is
        // updated only by caller-owned palette-upload seams, not by the shared decode boundary itself.
        PublishPaletteDacTable(resource.PaletteData.Span);

        // IDA 0x10DF2..0x10FD1: locate BODY and expand it into the caller-provided buffer using
        // either the packed PBM byte stream or the planar bitplane path.
        if (resource.IsPackedBitmap)
        {
            DecodePackedBody(resource.BodyData.Span, destinationBuffer, resource.Height, destinationStrideBytes);
            return;
        }

        // IDA 0x10EC5..0x10ECE derives the per-plane byte count from width >> 3, not a word-padded ILBM row size.
        DecodePlanarBody(resource.BodyData.Span,
            destinationBuffer,
            resource.Width,
            resource.Height,
            destinationStrideBytes);
    }

    private static ResourceView ParseResource(byte[] sourceBuffer)
    {
        var reader = new BinarySpanReader(sourceBuffer);
        if (!string.Equals(reader.ReadFixedAscii(4), "FORM", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Image resource does not start with a FORM header.");
        }

        var declaredFormSize = checked((int)reader.ReadUInt32BE());
        var totalLength = Math.Min(sourceBuffer.Length, declaredFormSize + 8);
        if (totalLength < 12)
        {
            throw new InvalidDataException("Image resource FORM header is truncated.");
        }

        var resourceMemory = sourceBuffer.AsMemory(0, totalLength);
        var formReader = new BinarySpanReader(resourceMemory);
        _ = formReader.ReadFixedAscii(4);
        _ = formReader.ReadUInt32BE();
        var formType = formReader.ReadFixedAscii(4);

        var chunks = ChunkFileReader.ReadEntries(resourceMemory, 12, totalLength - 12, FormChunkLayout);

        var bmhdIndex = FindRequiredChunkIndex(chunks, 0, "BMHD");
        var bmhd = chunks[bmhdIndex];
        if (bmhd.Length < 8)
        {
            throw new InvalidDataException("Image resource BMHD chunk is truncated.");
        }

        var headerReader = new BinarySpanReader(bmhd.Payload);
        var width = headerReader.ReadUInt16BE();
        var height = headerReader.ReadUInt16BE();

        var cmapIndex = FindRequiredChunkIndex(chunks, bmhdIndex + 1, "CMAP");
        var cmap = chunks[cmapIndex];
        if (cmap.Length < PaletteByteCount)
        {
            throw new InvalidDataException("Image resource CMAP chunk is truncated.");
        }

        var body = chunks[FindRequiredChunkIndex(chunks, cmapIndex + 1, "BODY")];
        return new ResourceView(formType == "PBM ", width, height, cmap.Payload[..PaletteByteCount], body.Payload);
    }

    private void PublishPaletteDacTable(ReadOnlySpan<byte> paletteData)
    {
        var dacTable = state.Presentation.Display.PaletteDacTable;

        for (var index = 0; index < PaletteByteCount; index++)
        {
            dacTable[index] = (byte)(paletteData[index] >> 2);
        }
    }

    private static void DecodePackedBody(ReadOnlySpan<byte> bodyData,
        byte[] destinationBuffer,
        ushort height,
        int destinationStrideBytes)
    {
        var decodedBytesRemaining = GetDecodedByteCount(height, destinationStrideBytes, destinationBuffer.Length);
        var bodyOffset = 0;
        var destinationOffset = 0;

        while (decodedBytesRemaining > 0)
        {
            EnsureReadable(bodyData, bodyOffset, 1, "packed BODY control byte");
            var control = unchecked((sbyte)bodyData[bodyOffset++]);

            if (control >= 0)
            {
                var literalCount = control + 1;
                EnsureProgress(decodedBytesRemaining, literalCount, "packed BODY literal run");
                EnsureReadable(bodyData, bodyOffset, literalCount, "packed BODY literal run");
                EnsureWritable(destinationBuffer, destinationOffset, literalCount);
                bodyData.Slice(bodyOffset, literalCount)
                    .CopyTo(destinationBuffer.AsSpan(destinationOffset, literalCount));
                bodyOffset += literalCount;
                destinationOffset += literalCount;
                decodedBytesRemaining -= literalCount;
                continue;
            }

            // IDA 0x10E6A..0x10E96 treats every negative control byte, including -128, as a repeat run.
            var repeatedCount = 1 - control;
            EnsureProgress(decodedBytesRemaining, repeatedCount, "packed BODY repeat run");
            EnsureReadable(bodyData, bodyOffset, 1, "packed BODY repeat byte");
            EnsureWritable(destinationBuffer, destinationOffset, repeatedCount);
            destinationBuffer.AsSpan(destinationOffset, repeatedCount).Fill(bodyData[bodyOffset++]);
            destinationOffset += repeatedCount;
            decodedBytesRemaining -= repeatedCount;
        }
    }

    private static void DecodePlanarBody(ReadOnlySpan<byte> bodyData,
        byte[] destinationBuffer,
        ushort width,
        ushort height,
        int destinationStrideBytes)
    {
        var decodedBytesRemaining = GetDecodedByteCount(height, destinationStrideBytes, destinationBuffer.Length);
        var widthBytes = width >> 3;
        var bodyOffset = 0;
        var destinationRowOffset = 0;

        for (var row = 0; row < height && decodedBytesRemaining > 0; row++)
        {
            for (var planeMask = 1; planeMask <= 0x80; planeMask <<= 1)
            {
                var planeBytesRemaining = widthBytes;
                var destinationOffset = destinationRowOffset;

                while (planeBytesRemaining > 0)
                {
                    EnsureReadable(bodyData, bodyOffset, 1, "planar BODY control byte");
                    var control = unchecked((sbyte)bodyData[bodyOffset++]);

                    if (control >= 0)
                    {
                        var literalCount = control + 1;
                        EnsureProgress(planeBytesRemaining, literalCount, "planar BODY literal run");
                        EnsureProgress(decodedBytesRemaining, literalCount, "planar BODY literal run");
                        EnsureReadable(bodyData, bodyOffset, literalCount, "planar BODY literal run");

                        for (var index = 0; index < literalCount; index++)
                        {
                            destinationOffset = ApplyPlaneByte(destinationBuffer,
                                destinationOffset,
                                planeMask,
                                bodyData[bodyOffset++]);
                        }

                        planeBytesRemaining -= literalCount;
                        decodedBytesRemaining -= literalCount;
                        continue;
                    }

                    // IDA 0x10F3D..0x10F9D mirrors the packed path and treats -128 as a 129-byte repeat run.
                    var repeatedCount = 1 - control;
                    EnsureProgress(planeBytesRemaining, repeatedCount, "planar BODY repeat run");
                    EnsureProgress(decodedBytesRemaining, repeatedCount, "planar BODY repeat run");
                    EnsureReadable(bodyData, bodyOffset, 1, "planar BODY repeat byte");
                    var repeatedByte = bodyData[bodyOffset++];

                    for (var index = 0; index < repeatedCount; index++)
                    {
                        destinationOffset =
                            ApplyPlaneByte(destinationBuffer, destinationOffset, planeMask, repeatedByte);
                    }

                    planeBytesRemaining -= repeatedCount;
                    decodedBytesRemaining -= repeatedCount;
                }
            }

            destinationRowOffset += destinationStrideBytes;
        }
    }

    private static int ApplyPlaneByte(byte[] destinationBuffer, int destinationOffset, int planeMask, byte planeByte)
    {
        EnsureWritable(destinationBuffer, destinationOffset, PlaneCount);

        for (var bit = 0; bit < PlaneCount; bit++)
        {
            if ((planeByte & 0x80) != 0)
            {
                destinationBuffer[destinationOffset] |= (byte)planeMask;
            }
            else
            {
                destinationBuffer[destinationOffset] = (byte)(destinationBuffer[destinationOffset] & ~planeMask);
            }

            planeByte <<= 1;
            destinationOffset++;
        }

        return destinationOffset;
    }

    private static int GetDecodedByteCount(ushort height, int destinationStrideBytes, int destinationBufferLength)
    {
        return Math.Min(destinationStrideBytes * height, destinationBufferLength);
    }

    private static int FindRequiredChunkIndex(IReadOnlyList<ArchiveEntryDescriptor> chunks, int startIndex, string tag)
    {
        for (var index = Math.Max(startIndex, 0); index < chunks.Count; index++)
        {
            if (!string.Equals(chunks[index].Tag, tag, StringComparison.Ordinal))
            {
                continue;
            }

            return index;
        }

        throw new InvalidDataException($"Image resource is missing the required '{tag}' chunk.");
    }

    private static void EnsureReadable(ReadOnlySpan<byte> data, int offset, int length, string description)
    {
        if (offset < 0 || length < 0 || offset > data.Length - length)
        {
            throw new InvalidDataException($"Image resource is truncated while reading {description}.");
        }
    }

    private static void EnsureProgress(int remainingBytes, int requestedBytes, string description)
    {
        if (requestedBytes > remainingBytes)
        {
            throw new InvalidDataException($"Image resource overruns the expected decoded range during {description}.");
        }
    }

    private static void EnsureWritable(byte[] destinationBuffer, int offset, int length)
    {
        if (offset < 0 || length < 0 || offset > destinationBuffer.Length - length)
        {
            throw new InvalidDataException("Decoded image data exceeds the destination buffer.");
        }
    }

    private readonly record struct ResourceView(
        bool IsPackedBitmap,
        ushort Width,
        ushort Height,
        ReadOnlyMemory<byte> PaletteData,
        ReadOnlyMemory<byte> BodyData);
}
