namespace Game.Shared.Rendering;

/// <summary>
///     Tracks which parts of a surface need to be redrawn.
/// </summary>
public sealed class DirtyRegionSet
{
    private readonly int _height;
    private readonly List<IntRect> _regions = [];
    private readonly int _width;

    /// <summary>
    ///     Creates a dirty-region tracker for a fixed surface size.
    /// </summary>
    /// <param name="width">The width in pixels.</param>
    /// <param name="height">The height in pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width" /> or <paramref name="height" /> is not positive.</exception>
    public DirtyRegionSet(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Dirty-region width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Dirty-region height must be positive.");
        }

        _width = width;
        _height = height;
    }

    /// <summary>
    ///     True when anything is dirty.
    /// </summary>
    public bool HasAny => IsFull || _regions.Count > 0;

    /// <summary>
    ///     True when the whole surface is dirty.
    /// </summary>
    public bool IsFull { get; private set; }

    /// <summary>
    ///     Explicit dirty regions when <see cref="IsFull" /> is <see langword="false" />.
    /// </summary>
    public IReadOnlyList<IntRect> Regions => _regions;

    /// <summary>
    ///     Adds one dirty region.
    /// </summary>
    /// <param name="rect">The region to add.</param>
    /// <remarks>
    ///     The region is clipped to the configured bounds. Empty regions are ignored.
    ///     When <see cref="IsFull" /> is <see langword="true" />, this method is a no-op.
    /// </remarks>
    public void Add(IntRect rect)
    {
        if (IsFull)
        {
            return;
        }

        var clipped = Clip(rect);
        if (clipped.IsEmpty)
        {
            return;
        }

        _regions.Add(clipped);
    }

    /// <summary>
    ///     Clears all dirty state.
    /// </summary>
    public void Clear()
    {
        IsFull = false;
        _regions.Clear();
    }

    /// <summary>
    ///     Marks the whole surface dirty and drops any tracked subregions.
    /// </summary>
    public void MarkFull()
    {
        IsFull = true;
        _regions.Clear();
    }

    private IntRect Clip(IntRect rect)
    {
        if (rect.IsEmpty)
        {
            return default;
        }

        var left = Math.Clamp(rect.X, 0, _width);
        var top = Math.Clamp(rect.Y, 0, _height);
        var right = Math.Clamp(rect.Right, 0, _width);
        var bottom = Math.Clamp(rect.Bottom, 0, _height);

        return right <= left || bottom <= top ? default : new IntRect(left, top, right - left, bottom - top);
    }
}
