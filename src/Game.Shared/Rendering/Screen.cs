namespace Game.Shared.Rendering;

/// <summary>
///     Screen-sized presentation target with indexed overlay layers and an RGBA present surface.
/// </summary>
/// <remarks>
///     The screen keeps 8-bit indexed overlay and cursor layers plus a 32-bit RGBA
///     <see cref="PresentSurface" /> for final presentation.
///     <para>
///         <see cref="DirtyRegions" /> tracks invalidated regions so render backends can avoid full-screen copies.
///     </para>
/// </remarks>
public sealed class Screen
{
    /// <summary>
    ///     Creates a screen with the specified size.
    /// </summary>
    /// <param name="width">The screen width in pixels.</param>
    /// <param name="height">The screen height in pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width" /> or <paramref name="height" /> is not positive.</exception>
    public Screen(int width, int height)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Screen width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Screen height must be positive.");
        }

        Width = width;
        Height = height;
        OverlayLayer = new IndexedScreenLayer(width, height, byte.MaxValue);
        CursorLayer = new IndexedScreenLayer(width, height, byte.MaxValue);
        PresentSurface = new Surface(width, height, PixelFormat.Rgba32);
        DirtyRegions = new DirtyRegionSet(width, height);
        Blitter.Clear(OverlayLayer.Surface, OverlayLayer.TransparentIndex);
        Blitter.Clear(CursorLayer.Surface, CursorLayer.TransparentIndex);
        DirtyRegions.MarkFull();
    }

    /// <summary>
    ///     Cursor layer.
    /// </summary>
    public IndexedScreenLayer CursorLayer { get; }

    /// <summary>
    ///     Dirty region tracking for incremental presentation.
    /// </summary>
    public DirtyRegionSet DirtyRegions { get; }

    /// <summary>
    ///     Screen height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    ///     Overlay layer.
    /// </summary>
    public IndexedScreenLayer OverlayLayer { get; }

    /// <summary>
    ///     RGBA present surface.
    /// </summary>
    public Surface PresentSurface { get; }

    /// <summary>
    ///     Screen width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    ///     Clears all dirty tracking.
    /// </summary>
    public void ClearDirty()
    {
        DirtyRegions.Clear();
    }

    /// <summary>
    ///     Marks a rectangle as dirty.
    /// </summary>
    /// <param name="rect">The rectangle to invalidate.</param>
    public void Invalidate(IntRect rect)
    {
        DirtyRegions.Add(rect);
    }

    /// <summary>
    ///     Marks the whole screen as dirty.
    /// </summary>
    public void InvalidateAll()
    {
        DirtyRegions.MarkFull();
    }
}
