namespace Game.DataBlock.Selection;

/// <summary>
///     Selection-entry data-block section.
/// </summary>
internal sealed class SelectionTableSection
{
    /// <summary>
    ///     Section start offset within the data block.
    /// </summary>
    private const int Offset = 0x0000;

    /// <summary>
    ///     Number of selection-entry records.
    /// </summary>
    internal const int Count = 0x65;

    /// <summary>
    ///     Total section length in bytes.
    /// </summary>
    internal const int Length = Count * SelectionEntryData.Size;

    private readonly SelectionEntryData[] _entries = Enumerable.Range(0, Count)
        .Select(static _ => new SelectionEntryData()).ToArray();

    /// <summary>
    ///     Gets one selection-entry record by data-block index.
    /// </summary>
    /// <param name="index">Zero-based selection-entry index.</param>
    internal SelectionEntryData this[int index] => _entries[index];

    /// <summary>
    ///     Reads the full selection-entry section from raw data-block bytes.
    /// </summary>
    /// <param name="block">Complete or sufficiently large data block.</param>
    internal void ReadFromBlock(ReadOnlySpan<byte> block)
    {
        if (block.Length < Offset + Length)
        {
            throw new InvalidOperationException(
                $"Selection table block must contain at least 0x{Offset + Length:X} bytes.");
        }

        var section = block.Slice(Offset, Length);
        for (var index = 0; index < _entries.Length; index++)
        {
            _entries[index].ReadFrom(section.Slice(index * SelectionEntryData.Size, SelectionEntryData.Size));
        }
    }

    /// <summary>
    ///     Writes the full selection-entry section to raw data-block bytes.
    /// </summary>
    /// <param name="block">Complete or sufficiently large data block.</param>
    internal void WriteToBlock(Span<byte> block)
    {
        if (block.Length < Offset + Length)
        {
            throw new InvalidOperationException(
                $"Selection table block must provide at least 0x{Offset + Length:X} bytes.");
        }

        var section = block.Slice(Offset, Length);
        for (var index = 0; index < _entries.Length; index++)
        {
            _entries[index].WriteTo(section.Slice(index * SelectionEntryData.Size, SelectionEntryData.Size));
        }
    }
}
