namespace Game.Shared.Formats.Containers;

/// <summary>
///     One parsed container entry as raw tag and payload data.
/// </summary>
/// <param name="tag">Optional entry tag.</param>
/// <param name="length">Payload length in bytes.</param>
/// <param name="payload">Payload bytes.</param>
public sealed class ArchiveEntryDescriptor(string? tag, int length, ReadOnlyMemory<byte> payload)
{
    /// <summary>
    ///     Payload length in bytes.
    /// </summary>
    public int Length { get; } = length;

    /// <summary>
    ///     Payload bytes.
    /// </summary>
    public ReadOnlyMemory<byte> Payload { get; } = payload;

    /// <summary>
    ///     Optional entry tag.
    /// </summary>
    public string? Tag { get; } = tag;
}
