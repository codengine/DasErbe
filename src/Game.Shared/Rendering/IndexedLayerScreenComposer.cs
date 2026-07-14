using Game.Shared.Palette;

namespace Game.Shared.Rendering;

/// <summary>
///     Composes indexed screen layers into the final RGBA present surface.
/// </summary>
public sealed class IndexedLayerScreenComposer
{
    private readonly Surface _indexedCompositionScratch;

    /// <summary>
    ///     Creates a composer for a fixed output size.
    /// </summary>
    /// <param name="width">The screen width in pixels.</param>
    /// <param name="height">The screen height in pixels.</param>
    public IndexedLayerScreenComposer(int width, int height)
    {
        _indexedCompositionScratch = new Surface(width, height, PixelFormat.Indexed8);
    }

    /// <summary>
    ///     Composes the dirty parts of <paramref name="screen" /> into <see cref="Screen.PresentSurface" />.
    /// </summary>
    /// <param name="screen">The screen to compose.</param>
    /// <param name="gameSurface">Opaque indexed base layer.</param>
    /// <param name="palette">Palette used for indexed-to-RGBA conversion.</param>
    /// <exception cref="ArgumentException"><paramref name="gameSurface" /> is incompatible with <paramref name="screen" />.</exception>
    public void ComposeFromGameSurface(Screen screen, Surface gameSurface, ColorPalette palette)
    {
        ArgumentNullException.ThrowIfNull(screen);
        ArgumentNullException.ThrowIfNull(gameSurface);
        ArgumentNullException.ThrowIfNull(palette);

        if (gameSurface.Format != PixelFormat.Indexed8 || gameSurface.Width != screen.Width ||
            gameSurface.Height != screen.Height)
        {
            throw new ArgumentException("The game surface must be an indexed surface matching the screen dimensions.",
                nameof(gameSurface));
        }

        if (!screen.DirtyRegions.HasAny)
        {
            return;
        }

        if (screen.DirtyRegions.IsFull)
        {
            var fullRect = new IntRect(0, 0, screen.Width, screen.Height);
            ComposeRegion(screen, gameSurface, palette, fullRect);
            return;
        }

        foreach (var region in screen.DirtyRegions.Regions)
        {
            ComposeRegion(screen, gameSurface, palette, region);
        }
    }

    private void ComposeRegion(Screen screen, Surface gameSurface, ColorPalette palette, IntRect rect)
    {
        Blitter.Copy(gameSurface, rect, _indexedCompositionScratch, rect.X, rect.Y);
        Blitter.KeyedCopyIndexed8(screen.OverlayLayer.Surface,
            rect,
            _indexedCompositionScratch,
            rect.X,
            rect.Y,
            screen.OverlayLayer.TransparentIndex);
        Blitter.KeyedCopyIndexed8(screen.CursorLayer.Surface,
            rect,
            _indexedCompositionScratch,
            rect.X,
            rect.Y,
            screen.CursorLayer.TransparentIndex);
        Blitter.ConvertIndexed8ToRgba32(_indexedCompositionScratch,
            rect,
            palette.AsSpan(),
            screen.PresentSurface,
            rect.X,
            rect.Y);
    }
}
