using System.Buffers.Binary;

namespace Game.DataBlock.Selection;

/// <summary>
///     Raw 6-byte selection-entry record from the data block.
/// </summary>
internal sealed class SelectionEntryData
{
    /// <summary>
    ///     Size of one selection-entry record in bytes.
    /// </summary>
    internal const int Size = 0x06;

    /// <summary>
    ///     Selection state id at record word <c>+0x00</c>.
    /// </summary>
    internal ushort StateId { get; set; }

    /// <summary>
    ///     Selection state mask at record word <c>+0x02</c>.
    /// </summary>
    internal ushort State { get; set; }

    /// <summary>
    ///     Paired-selection key at record word <c>+0x04</c>.
    /// </summary>
    internal ushort LinkedDescriptorKey { get; set; }

    /// <summary>
    ///     Reads this record from raw data-block bytes.
    /// </summary>
    /// <param name="source">Source span that starts at the record boundary.</param>
    internal void ReadFrom(ReadOnlySpan<byte> source)
    {
        if (source.Length < Size)
        {
            throw new InvalidOperationException($"Selection entry source must contain at least 0x{Size:X} bytes.");
        }

        StateId = BinaryPrimitives.ReadUInt16LittleEndian(source[..sizeof(ushort)]);
        State = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x02, sizeof(ushort)));
        LinkedDescriptorKey = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x04, sizeof(ushort)));
    }

    /// <summary>
    ///     Writes this record to raw data-block bytes.
    /// </summary>
    /// <param name="destination">Destination span that starts at the record boundary.</param>
    internal void WriteTo(Span<byte> destination)
    {
        if (destination.Length < Size)
        {
            throw new InvalidOperationException($"Selection entry destination must provide at least 0x{Size:X} bytes.");
        }

        BinaryPrimitives.WriteUInt16LittleEndian(destination[..sizeof(ushort)], StateId);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x02, sizeof(ushort)), State);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x04, sizeof(ushort)), LinkedDescriptorKey);
    }
}
