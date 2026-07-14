namespace Game.DataBlock.Scene;

/// <summary>
///     Scene-descriptor data-block section.
/// </summary>
internal sealed class SceneDescriptorSection
{
    /// <summary>
    ///     Section start offset within the data block.
    /// </summary>
    internal const int Offset = 0x16FE;

    /// <summary>
    ///     Number of scene-descriptor records.
    /// </summary>
    internal const int Count = 0x11;

    /// <summary>
    ///     Total section length in bytes.
    /// </summary>
    internal const int Length = Count * SceneDescriptorRecord.Size;

    private readonly SceneDescriptorRecord[] _records = Enumerable.Range(0, Count)
        .Select(static _ => new SceneDescriptorRecord()).ToArray();

    /// <summary>
    ///     Gets one scene-descriptor record by data-block index.
    /// </summary>
    /// <param name="index">Zero-based scene-descriptor index.</param>
    internal SceneDescriptorRecord this[int index] => _records[index];

    /// <summary>
    ///     Reads the full scene-descriptor section from raw data-block bytes.
    /// </summary>
    /// <param name="block">Complete or sufficiently large data block.</param>
    internal void ReadFromBlock(ReadOnlySpan<byte> block)
    {
        if (block.Length < Offset + Length)
        {
            throw new InvalidOperationException(
                $"Scene descriptor section block must contain at least 0x{Offset + Length:X} bytes.");
        }

        var section = block.Slice(Offset, Length);
        for (var index = 0; index < _records.Length; index++)
        {
            _records[index].ReadFrom(section.Slice(index * SceneDescriptorRecord.Size, SceneDescriptorRecord.Size));
        }
    }

    /// <summary>
    ///     Writes the full scene-descriptor section to raw data-block bytes.
    /// </summary>
    /// <param name="block">Complete or sufficiently large data block.</param>
    internal void WriteToBlock(Span<byte> block)
    {
        if (block.Length < Offset + Length)
        {
            throw new InvalidOperationException(
                $"Scene descriptor section block must provide at least 0x{Offset + Length:X} bytes.");
        }

        var section = block.Slice(Offset, Length);
        for (var index = 0; index < _records.Length; index++)
        {
            _records[index].WriteTo(section.Slice(index * SceneDescriptorRecord.Size, SceneDescriptorRecord.Size));
        }
    }
}
