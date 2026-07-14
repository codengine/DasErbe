using Game.Catalogs;
using Game.Display;
using Game.Input;
using Game.Shared.RE;
using Game.State;

namespace Game.Runtime.Bootstrapping;

internal sealed class StartupTitleSequenceRunner(Erbe runtime)
{
    private const ushort IntroTextColumn = 0x0050;
    private const ushort IntroTextRow = 0x009B;
    private const ushort IntroPageTimeoutTicks = 0x015E;
    private const ushort TitlePaletteTableOffset = 0x5D03;
    private readonly StartupIntroMusicController _introMusic = new(runtime);

    /// <summary>
    ///     Runs the startup-title sequence to completion.
    /// </summary>
    [FunctionSymbol("sub_152DD", 0x152DD)]
    internal void Run()
    {
        ushort fadeStep = 0;

        // IDA 0x152E3..0x15327: seed title-text color and refresh the retained title screen.
        runtime.State.Presentation.Display.TextInkColorIndex = 0xFF;
        runtime.FullScreenSourceSurface.Reload(AssetId.TitleScreen);
        var displayRegion = new DisplayCopyRegion(RuntimeState.FrameWidth, RuntimeState.FrameHeight, 0, 0, 0, 0);
        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0, runtime.FullScreenSourceSurface.Buffer, displayRegion);

        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        runtime.DisplayCopy.CopyPublishedBufferToWorkBuffer();
        runtime.DisplayCopy.CopyWorkBufferToSnapshotBuffer();
        _introMusic.Start();

        while (true)
        {
            runtime.Palette.ApplyPaletteFadeStepFromDacTable(TitlePaletteTableOffset, fadeStep);
            if (fadeStep == 0x0080)
            {
                runtime.HostPacing.WaitFrame();
                break;
            }

            fadeStep++;
            runtime.HostPacing.WaitFrame();
        }

        var inputEvent = runtime.InputAdapter.PollInputEvent();
        if (inputEvent == RuntimeInputEvent.None)
        {
            for (var introPageIndex = 0; introPageIndex < runtime.Strings.IntroPages.Count; introPageIndex++)
            {
                runtime.TextCursor.SeedTextBlockCursor(IntroTextColumn, IntroTextRow);
                runtime.TextRenderer.RenderStringBlock(runtime.Strings.IntroPages[introPageIndex]);
                runtime.PointerOverlay.AdvancePointerOverlayFrame();
                runtime.DisplayCopy.CopyPublishedBufferToWorkBuffer();
                inputEvent = WaitForIntroPageExit();

                runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0,
                    0,
                    RuntimeState.FrameWidth,
                    RuntimeState.FrameHeight);
                // runtime.HostPacing.WaitFrame();
                if (inputEvent != RuntimeInputEvent.None || introPageIndex == runtime.Strings.IntroPages.Count - 1)
                {
                    break;
                }
            }
        }

        while (true)
        {
            runtime.Palette.ApplyPaletteFadeStepFromDacTable(TitlePaletteTableOffset, fadeStep);
            _introMusic.ApplyFadeVolume(fadeStep);
            if (fadeStep == 0)
            {
                runtime.HostPacing.WaitFrame();
                _introMusic.Stop();
                return;
            }

            fadeStep--;
            runtime.HostPacing.WaitFrame();
        }
    }

    private RuntimeInputEvent WaitForIntroPageExit()
    {
        var remainingTicks = IntroPageTimeoutTicks;
        RuntimeInputEvent inputEvent;
        do
        {
            inputEvent = runtime.InputAdapter.PollInputEvent();
            if (inputEvent != RuntimeInputEvent.None)
            {
                break;
            }

            runtime.PointerOverlay.AdvancePointerOverlayFrame();
            remainingTicks--;
        } while (remainingTicks != 0);

        return inputEvent;
    }
}
