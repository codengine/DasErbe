using System.Buffers.Binary;

namespace Game.DataBlock.Scene;

/// <summary>
///     Raw 14-byte scene-descriptor record from the data block.
/// </summary>
internal sealed class SceneDescriptorRecord
{
    /// <summary>
    ///     Size of one scene-descriptor record in bytes.
    /// </summary>
    internal const int Size = 0x0E;

    /// <summary>
    ///     Secondary asset id published by this scene descriptor.
    /// </summary>
    internal ushort SecondAssetId { get; set; }

    /// <summary>
    ///     Primary asset id published by this scene descriptor.
    /// </summary>
    internal ushort FirstAssetId { get; set; }

    /// <summary>
    ///     Raw source offset for the optional transition text published for this scene.
    /// </summary>
    internal ushort OptionalTransitionTextSourceOffset { get; set; }

    /// <summary>
    ///     First scene-entry descriptor index for this scene.
    /// </summary>
    internal ushort SceneEntryStartIndex { get; set; }

    /// <summary>
    ///     Number of scene-entry descriptors in this scene.
    /// </summary>
    internal ushort SceneEntryCount { get; set; }

    /// <summary>
    ///     RLE word-stream offset for the scene asset payload.
    /// </summary>
    internal ushort RleWordStreamOffset { get; set; }

    /// <summary>
    ///     Backdrop-enabled flag published by this scene descriptor.
    /// </summary>
    internal byte BackdropEnabledFlag { get; set; }

    /// <summary>
    ///     Base visible-row count published by this scene descriptor.
    /// </summary>
    internal byte VisibleRowsBase { get; set; }

    /// <summary>
    ///     Reads this record from raw data-block bytes.
    /// </summary>
    /// <param name="source">Source span that starts at the record boundary.</param>
    internal void ReadFrom(ReadOnlySpan<byte> source)
    {
        if (source.Length < Size)
        {
            throw new InvalidOperationException(
                $"Scene descriptor record source must contain at least 0x{Size:X} bytes.");
        }

        SecondAssetId = BinaryPrimitives.ReadUInt16LittleEndian(source[..sizeof(ushort)]);
        FirstAssetId = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x02, sizeof(ushort)));
        OptionalTransitionTextSourceOffset =
            BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x04, sizeof(ushort)));
        SceneEntryStartIndex = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x06, sizeof(ushort)));
        SceneEntryCount = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x08, sizeof(ushort)));
        RleWordStreamOffset = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x0A, sizeof(ushort)));
        BackdropEnabledFlag = source[0x0C];
        VisibleRowsBase = source[0x0D];
    }

    /// <summary>
    ///     Writes this record to raw data-block bytes.
    /// </summary>
    /// <param name="destination">Destination span that starts at the record boundary.</param>
    internal void WriteTo(Span<byte> destination)
    {
        if (destination.Length < Size)
        {
            throw new InvalidOperationException(
                $"Scene descriptor record destination must provide at least 0x{Size:X} bytes.");
        }

        BinaryPrimitives.WriteUInt16LittleEndian(destination[..sizeof(ushort)], SecondAssetId);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x02, sizeof(ushort)), FirstAssetId);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x04, sizeof(ushort)),
            OptionalTransitionTextSourceOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x06, sizeof(ushort)), SceneEntryStartIndex);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x08, sizeof(ushort)), SceneEntryCount);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x0A, sizeof(ushort)), RleWordStreamOffset);
        destination[0x0C] = BackdropEnabledFlag;
        destination[0x0D] = VisibleRowsBase;
    }
}
