using Game.Shared.Rendering;

namespace Game.Shared.Palette;

/// <summary>
///     Mutable 256-color RGBA palette for indexed rendering.
/// </summary>
public sealed class ColorPalette
{
    /// <summary>
    ///     Palette size.
    /// </summary>
    public const int EntryCount = 256;
    private static readonly Rgba32 OpaqueBlack = new(0, 0, 0);

    private readonly Rgba32[] _entries = new Rgba32[EntryCount];

    /// <summary>
    ///     Starts with every entry set to opaque black.
    /// </summary>
    public ColorPalette()
    {
        _entries.AsSpan().Fill(OpaqueBlack);
    }

    /// <summary>
    ///     Bumped whenever the palette contents change.
    /// </summary>
    public ulong Version { get; private set; }

    /// <summary>
    ///     Resets the whole palette to opaque black.
    /// </summary>
    public void Clear()
    {
        _entries.AsSpan().Fill(OpaqueBlack);
        Version++;
    }

    /// <summary>
    ///     Returns the current entries as a read-only span.
    /// </summary>
    public ReadOnlySpan<Rgba32> AsSpan()
    {
        return _entries;
    }

    /// <summary>
    ///     Replaces the whole palette in one write.
    /// </summary>
    /// <param name="entries">A span containing exactly 256 entries.</param>
    /// <exception cref="ArgumentException"><paramref name="entries" /> does not contain exactly 256 entries.</exception>
    public void SetAll(ReadOnlySpan<Rgba32> entries)
    {
        if (entries.Length != EntryCount)
        {
            throw new ArgumentException("A full palette update must provide exactly 256 entries.", nameof(entries));
        }

        entries.CopyTo(_entries);
        Version++;
    }
}
