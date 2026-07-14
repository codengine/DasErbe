namespace Game.Shared.Rendering;

/// <summary>
///     Indexed screen layer plus its transparent color key.
/// </summary>
public sealed class IndexedScreenLayer
{
    /// <summary>
    ///     Creates an indexed screen layer.
    /// </summary>
    /// <param name="width">Layer width in pixels.</param>
    /// <param name="height">Layer height in pixels.</param>
    /// <param name="transparentIndex">Palette index used as the transparent color key.</param>
    public IndexedScreenLayer(int width, int height, byte transparentIndex = 0)
    {
        Surface = new Surface(width, height, PixelFormat.Indexed8);
        TransparentIndex = transparentIndex;
    }

    /// <summary>
    ///     Indexed pixel surface for this layer.
    /// </summary>
    public Surface Surface { get; }

    /// <summary>
    ///     Palette index treated as transparent during keyed copies.
    /// </summary>
    public byte TransparentIndex { get; }
}
