namespace Game.DataBlock.Interaction;

/// <summary>
///     Interaction-descriptor data-block section.
/// </summary>
internal sealed class InteractionDescriptorSection
{
    /// <summary>
    ///     Section start offset within the data block.
    /// </summary>
    private const int Offset = 0x025E;

    /// <summary>
    ///     Number of interaction-descriptor records.
    /// </summary>
    private const int Count = 0xA5;

    /// <summary>
    ///     Total section length in bytes.
    /// </summary>
    internal const int Length = Count * InteractionDescriptorRecord.Size;

    private readonly InteractionDescriptorRecord[] _records = Enumerable.Range(0, Count)
        .Select(static _ => new InteractionDescriptorRecord()).ToArray();

    /// <summary>
    ///     Gets one interaction-descriptor record by data-block index.
    /// </summary>
    /// <param name="index">Zero-based interaction-descriptor index.</param>
    internal InteractionDescriptorRecord this[int index] => _records[index];

    /// <summary>
    ///     Reads the full interaction-descriptor section from raw data-block bytes.
    /// </summary>
    /// <param name="block">Complete or sufficiently large data block.</param>
    internal void ReadFromBlock(ReadOnlySpan<byte> block)
    {
        if (block.Length < Offset + Length)
        {
            throw new InvalidOperationException(
                $"Interaction descriptor block must contain at least 0x{Offset + Length:X} bytes.");
        }

        var section = block.Slice(Offset, Length);
        for (var index = 0; index < _records.Length; index++)
        {
            _records[index].ReadFrom(section.Slice(index * InteractionDescriptorRecord.Size,
                InteractionDescriptorRecord.Size));
        }
    }

    /// <summary>
    ///     Writes the full interaction-descriptor section to raw data-block bytes.
    /// </summary>
    /// <param name="block">Complete or sufficiently large data block.</param>
    internal void WriteToBlock(Span<byte> block)
    {
        if (block.Length < Offset + Length)
        {
            throw new InvalidOperationException(
                $"Interaction descriptor block must provide at least 0x{Offset + Length:X} bytes.");
        }

        var section = block.Slice(Offset, Length);
        for (var index = 0; index < _records.Length; index++)
        {
            _records[index]
                .WriteTo(section.Slice(index * InteractionDescriptorRecord.Size, InteractionDescriptorRecord.Size));
        }
    }
}
