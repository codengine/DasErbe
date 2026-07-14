namespace Game.Shared.Formats.Containers;

/// <summary>
///     Describes the chunk header and payload alignment used by <see cref="ChunkFileReader" />.
/// </summary>
/// <param name="TagLength">Chunk tag length in bytes.</param>
/// <param name="LengthLength">Chunk length-field width in bytes.</param>
/// <param name="LengthLittleEndian"><see langword="true" /> when the chunk length is little-endian.</param>
/// <param name="Alignment">Payload alignment in bytes.</param>
public readonly record struct ChunkFileReaderOptions(
    int TagLength = 4,
    int LengthLength = 4,
    bool LengthLittleEndian = true,
    int Alignment = 1)
{
    /// <summary>
    ///     Total chunk-header size in bytes.
    /// </summary>
    public int HeaderLength => checked(TagLength + LengthLength);
}
