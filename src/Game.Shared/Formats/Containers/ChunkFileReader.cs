using System.Globalization;
using Game.Shared.IO;

namespace Game.Shared.Formats.Containers;

/// <summary>
///     Reads simple tag-plus-length chunk layouts into raw entry descriptors.
/// </summary>
public static class ChunkFileReader
{
    /// <summary>
    ///     Reads one bounded chunk region from a buffer.
    /// </summary>
    /// <param name="bytes">Source bytes that contain the chunk region.</param>
    /// <param name="offset">Start offset of the chunk region.</param>
    /// <param name="length">Length of the chunk region.</param>
    /// <param name="options">Chunk layout options.</param>
    /// <returns>Chunk descriptors in encounter order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="offset" /> or <paramref name="length" /> is negative, or <paramref name="options" /> is invalid.
    /// </exception>
    /// <exception cref="InvalidDataException">
    ///     The bounded region contains a truncated header, truncated padded chunk, or unsupported chunk layout.
    /// </exception>
    public static IReadOnlyList<ArchiveEntryDescriptor> ReadEntries(ReadOnlyMemory<byte> bytes,
        int offset,
        int length,
        ChunkFileReaderOptions options)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);
        if (options.TagLength <= 0 || options.LengthLength is not (2 or 4) || options.Alignment <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Chunk reader options are invalid.");
        }

        var reader = new BinarySpanReader(bytes);
        var bounded = reader.CreateSubreader(offset, length);

        var entries = new List<ArchiveEntryDescriptor>();
        while (bounded.RemainingLength > 0)
        {
            var tag = bounded.ReadFixedAscii(options.TagLength);
            var chunkLength = ReadChunkLength(ref bounded, options);
            var payload = bounded.ReadMemory(chunkLength);

            entries.Add(new ArchiveEntryDescriptor(tag, chunkLength, payload));

            var padding = ComputePadding(chunkLength, options.Alignment);
            if (padding <= 0)
            {
                continue;
            }

            bounded.Skip(padding);
        }

        return entries;
    }

    private static int ComputePadding(int length, int alignment)
    {
        var remainder = length % alignment;
        return remainder == 0 ? 0 : alignment - remainder;
    }

    private static int ReadChunkLength(ref BinarySpanReader reader, ChunkFileReaderOptions options)
    {
        return options.LengthLength switch
        {
            2 => options.LengthLittleEndian ? reader.ReadUInt16LE() : reader.ReadUInt16BE(),
            4 => checked((int)(options.LengthLittleEndian ? reader.ReadUInt32LE() : reader.ReadUInt32BE())),
            _ => throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture,
                $"Unsupported chunk-length width {options.LengthLength}."))
        };
    }
}
