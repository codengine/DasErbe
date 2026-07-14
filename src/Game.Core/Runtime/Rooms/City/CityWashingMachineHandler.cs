using Game.Catalogs;
using Game.Display;
using Game.Shared.RE;
using Game.State;
using Game.Text;

namespace Game.Runtime.Rooms.City;

internal sealed class CityWashingMachineHandler(Erbe runtime)
{
    [FunctionSymbol("sub_138C8", 0x138C8)]
    internal void RunUseWashingMachine()
    {
        // IDA 0x138CB..0x1390D: refresh the retained full-screen source surface through asset id 0x02E8 before the
        // washing-machine animation starts, then suppress backdrop rendering ahead of the forward replay loop. The
        // current catalog still names that asset BicyclePump, and the managed runtime preserves the
        // executable-backed asset identity instead of speculating a rename here.
        runtime.FullScreenSourceSurface.Reload(AssetId.BicyclePump);
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 0;

        short animationGroupIndex = 0;
        ushort animationRepeatIndex = 0;
        while (animationGroupIndex < 9)
        {
            // IDA 0x13916..0x13979: restore the retained snapshot, rebuild the scene with the backdrop seam
            // suppressed, blit the current transparent washing-machine frame, then advance the pointer/publication
            // cadence before the next replay iteration and roll from the fourth replay into the next forward group.
            ReplayWashingMachineFrame((ushort)animationGroupIndex);
            animationRepeatIndex++;
            if (animationRepeatIndex >= 4)
            {
                animationRepeatIndex = 0;
                animationGroupIndex++;
            }

            runtime.HostPacing.WaitFrame();
        }

        // IDA 0x1397D..0x1398E: block through the fixed "clean clothes" line before the reverse animation starts.
        runtime.PromptController.RunTextAnimation(StringId.City_WashingMachineUseSuccess);

        animationGroupIndex = 8;
        animationRepeatIndex = 0;
        while (animationGroupIndex >= 0)
        {
            // IDA 0x13996..0x139FA: replay the same snapshot-backed transparent washing-machine overlay frames in
            // reverse group order after the prompt returns, rolling from the fourth replay into the previous group.
            ReplayWashingMachineFrame((ushort)animationGroupIndex);
            animationRepeatIndex++;
            if (animationRepeatIndex >= 4)
            {
                animationRepeatIndex = 0;
                animationGroupIndex--;
            }

            runtime.HostPacing.WaitFrame();
        }

        // IDA 0x139FF..0x13A38: restore the default hero portrait backdrop surface after the washing-machine use
        // flow, then restore the normal backdrop-emission flag after the animation and prompt complete.
        runtime.State.RawDataBlock.Control.UseAlternateHeroPortrait = false;
        ReloadDefaultHeroPortraitBackdrop();
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 1;
    }

    private void ReplayWashingMachineFrame(ushort animationGroupIndex)
    {
        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.ProgramScene.RenderCurrentScene();
        DrawWashingMachineFrame(animationGroupIndex);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }

    private void DrawWashingMachineFrame(ushort animationGroupIndex)
    {
        var backdrop = runtime.State.RawDataBlock.Control;
        var animationRegion = new DisplayCopyRegion(32,
            70,
            115,
            checked((short)(animationGroupIndex * 32)),
            checked((short)(backdrop.BackdropThresholdRow - 70)),
            checked((short)(backdrop.BackdropColumn - 16)));

        runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            animationRegion);
    }

    private void ReloadDefaultHeroPortraitBackdrop()
    {
        var portraitLoadBuffer = runtime.ContentFileLoader.LoadOrThrow(AssetId.HeroPortrait);
        runtime.LbmDecoder.DecodeIntoBuffer(portraitLoadBuffer,
            runtime.State.Program.ScratchBuffers.Backdrop,
            DisplayCompatibilityState.StrideBytes);
    }
}
