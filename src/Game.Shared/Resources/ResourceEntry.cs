namespace Game.Shared.Resources;

/// <summary>
///     Represents a resolved resource entry.
/// </summary>
public sealed class ResourceEntry
{
    private readonly string _physicalPath;
    private readonly Lazy<ReadOnlyMemory<byte>> _readAll;

    private ResourceEntry(string physicalPath, long? length)
    {
        _physicalPath = physicalPath;
        Length = length;
        _readAll = new Lazy<ReadOnlyMemory<byte>>(() => ReadStreamFully(OpenRead(), length));
    }

    /// <summary>
    ///     Gets the entry length in bytes, when known.
    /// </summary>
    public long? Length { get; }

    /// <summary>
    ///     Creates a resource entry backed by a physical file.
    /// </summary>
    /// <param name="physicalPath">The physical file path.</param>
    /// <param name="length">Optional file length hint.</param>
    public static ResourceEntry FromFile(string physicalPath,
        long? length = null)
    {
        return new ResourceEntry(physicalPath, length);
    }

    /// <summary>
    ///     Opens a readable stream for the entry contents.
    /// </summary>
    public Stream OpenRead()
    {
        return File.OpenRead(_physicalPath);
    }

    /// <summary>
    ///     Materializes the full contents of the entry.
    /// </summary>
    /// <returns>The entry contents as an in-memory byte buffer.</returns>
    public ReadOnlyMemory<byte> ReadAll()
    {
        return _readAll.Value;
    }

    private static ReadOnlyMemory<byte> ReadStreamFully(Stream stream, long? lengthHint)
    {
        try
        {
            using var memory = lengthHint is > 0 and <= int.MaxValue
                ? new MemoryStream((int)lengthHint.Value)
                : new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }
        finally
        {
            stream.Dispose();
        }
    }
}
