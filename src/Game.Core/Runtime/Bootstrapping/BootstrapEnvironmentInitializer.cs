using Game.Catalogs;
using Game.Display;
using Game.Shared.RE;
using Game.Shared.Rendering;
using Game.State;
using Game.Text;

namespace Game.Runtime.Bootstrapping;

/// <summary>
///     Initializes the managed runtime state that replaces the original process and video bootstrap environment.
/// </summary>
/// <param name="runtime">Owning runtime state.</param>
internal sealed class BootstrapEnvironmentInitializer(Erbe runtime)
{
    private readonly FontPublisher _fontVgaPublisher = new(runtime.State.Presentation.Font);

    /// <summary>
    ///     Initializes display, input, and font state before entering the startup title sequence.
    /// </summary>
    internal void InitializeBootEnvironment()
    {
        // IDA 0x15542: capture the current display mode so it can be restored at shutdown.
        // obsolete in port

        // IDA 0x15548..0x15562: capture and replace the current critical-error policy.
        // obsolete in port

        // IDA 0x15565..0x155A8: initialize display state, initialize pointer input, then decode and publish the
        // built-in FONT_VGA atlas before the private bootstrap continuation enters the startup-title sequence.
        InitializeGraphics();
        var fontBytes = runtime.ContentFileLoader.LoadOrThrow(AssetId.FontVga);
        var decodedFontAtlas = new byte[RuntimeState.FrameByteCount];
        runtime.LbmDecoder.DecodeIntoBuffer(fontBytes,
            decodedFontAtlas,
            DisplayCompatibilityState.StrideBytes);
        _fontVgaPublisher.PublishDecodedFontAtlas(decodedFontAtlas);
    }

    /// <summary>
    ///     Initializes retained display state for the 320x200 indexed bootstrap mode.
    /// </summary>
    [FunctionSymbol("sub_1038F", 0x1038F, FunctionFlags.AdaptedForMonoGame)]
    private void InitializeGraphics()
    {
        var display = runtime.State.Presentation.Display;
        display.InitializeBufferRoles(0, 1, 2);

        // IDA 0x10271..0x102A9: clear the retained palette to black through the now-inlined reset-palette seam.
        runtime.State.Presentation.Palette.Clear();

        ClearFrameBuffer();

        // IDA 0x1040D..0x1040F: publish the initial prepared software framebuffer via sub_1052D.
        runtime.DisplayBufferPublisher.PublishPreparedBuffer();
    }

    private void ClearFrameBuffer()
    {
        var presentation = runtime.State.Presentation;
        var screen = presentation.Screen;
        foreach (var displayBuffer in presentation.Display.RetainedDisplayBuffers)
        {
            Blitter.Clear(displayBuffer, 0);
        }

        Blitter.Clear(screen.OverlayLayer.Surface, screen.OverlayLayer.TransparentIndex);
        Blitter.Clear(screen.CursorLayer.Surface, screen.CursorLayer.TransparentIndex);
        screen.InvalidateAll();
    }

    /// <summary>
    ///     Loads and draws the shared lower display panel before the main flow begins.
    /// </summary>
    [FunctionSymbol("sub_142D1", 0x142D1)]
    internal void InitializeDisplayPanel()
    {
        // IDA 0x142D7..0x14309: refresh the retained display-panel source surface from GRAFIK/DISPLAY.LBM.
        runtime.FullScreenSourceSurface.Reload(AssetId.DisplayBackdrop);

        // IDA 0x1430C..0x1433A: copy the top 320x72 region from the decoded DISPLAY.LBM into the current work buffer at
        // row 0x80 through the shared indexed blit seam sub_10578.
        var displayRegion = new DisplayCopyRegion(RuntimeState.FrameWidth, 72, 0, 0, 128, 0);
        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0, runtime.FullScreenSourceSurface.Buffer, displayRegion);

        // IDA 0x1433D..0x14357: publish indexed draw color 0 and fill the inclusive rectangle
        // [0x00D9..0x0130] x [0x008F..0x00C4] through the shared primitive draw seams.
        runtime.State.Presentation.Display.CurrentDrawColorIndex = 0x00;
        runtime.DisplayPrimitives.FillRectangleWithCurrentColor(217, 143, 304, 196);

        // IDA 0x1435A..0x14363: snapshot the current work buffer, advance the pointer overlay once, restore the snapshot
        // into the new work buffer, then advance the pointer overlay again.
        runtime.DisplayCopy.CopyWorkBufferToSnapshotBuffer();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.FrameHeight);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        // IDA 0x14366..0x1436F: publish the runtime DAC table at DS:0x5D03 through the full-palette upload seam.
        runtime.Palette.UploadFullPalette(runtime.State.Presentation.Display.PaletteDacTable);
    }
}
