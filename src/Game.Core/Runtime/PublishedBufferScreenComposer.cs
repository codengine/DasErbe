using Game.Shared.Rendering;

namespace Game.Runtime;

/// <summary>
///     Composes the published game software framebuffer into the host-presented screen surface.
/// </summary>
/// <param name="runtime">Owning runtime state.</param>
/// <param name="width">Presentation surface width in pixels.</param>
/// <param name="height">Presentation surface height in pixels.</param>
internal sealed class PublishedBufferScreenComposer(Erbe runtime, int width, int height)
{
    private readonly IndexedLayerScreenComposer _indexedLayerScreenComposer = new(width, height);
    private ulong _lastComposedPaletteVersion;

    /// <summary>
    ///     Composes the currently published software framebuffer into the presentation surface.
    /// </summary>
    internal void ComposeFrame()
    {
        // Managed host boundary: game logic prepares runtime-owned CPU framebuffers during Update(); Draw only presents
        // the already-composed PresentSurface through the host render backend.
        var presentation = runtime.State.Presentation;
        var screen = presentation.Screen;
        var display = presentation.Display;
        var publishedBuffer = display.GetPublishedBuffer();

        if (presentation.Palette.Version != _lastComposedPaletteVersion)
        {
            screen.InvalidateAll();
        }

        // The published software framebuffer is already the opaque game layer for this port. Composing from it directly
        // avoids a second game-owned framebuffer copy while keeping overlay/cursor layers owned by the engine renderer.
        _indexedLayerScreenComposer.ComposeFromGameSurface(screen, publishedBuffer, presentation.Palette);
        _lastComposedPaletteVersion = presentation.Palette.Version;
    }
}
