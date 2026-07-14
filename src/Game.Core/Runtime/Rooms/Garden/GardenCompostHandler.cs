using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Display;
using Game.Shared.RE;
using Game.State;
using Game.Text;

namespace Game.Runtime.Rooms.Garden;

internal sealed class GardenCompostHandler(Erbe runtime)
{
    private const ushort LighterUseAnimationProgramStateId = 0x0005;

    [FunctionSymbol("sub_13C54", 0x13C54)]
    internal void RunUseLighterWithCompost()
    {
        // IDA 0x13C5A..0x13C61: return immediately unless the current program state is 0x0005.
        if (runtime.State.RawDataBlock.Control.ProgramStateId != LighterUseAnimationProgramStateId)
        {
            return;
        }

        // IDA 0x13C64: suppress backdrop rendering while the overlay animation replays on top of the rebuilt scene.
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 0;

        ushort animationGroupIndex = 0;
        ushort animationRepeatIndex = 0;

        var animationRegion = new DisplayCopyRegion(47, 73, 0, 0, 24, 113);

        while (animationGroupIndex < 8)
        {
            animationRegion.SourceRow = checked((short)(animationGroupIndex / 4 * 74));
            animationRegion.SourceColumn = checked((short)(animationGroupIndex % 4 * 48));

            // IDA 0x13C77..0x13CE6: restore the retained snapshot, rebuild the current scene with backdrop
            // suppressed, blit the current overlay frame from the decoded scene surface, advance the pointer overlay
            // once before exposing the next frame, then roll the repeat counter into the next frame group after every
            // sixth replay.
            runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
            runtime.ProgramScene.RenderCurrentScene();
            runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
                runtime.State.Program.ScratchBuffers.Scene,
                animationRegion);
            runtime.PointerOverlay.AdvancePointerOverlayFrame();

            animationRepeatIndex++;
            if (animationRepeatIndex >= 6)
            {
                animationRepeatIndex = 0;
                animationGroupIndex++;
            }

            runtime.HostPacing.WaitFrame();
        }

        // IDA 0x13CEC..0x13CFD: re-enable backdrop rendering and queue the fixed variant-A prompt branch.
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 1;
        runtime.PromptController.QueueOzoneAlternateSceneTransition(StringId.Garden_LighterUseCompostDisposalHint, 5);
    }

    [FunctionSymbol("sub_12929", 0x12929)]
    internal void RunUseLeafPileWithCompost()
    {
        // IDA 0x1292C..0x12931: publish the stable "leaf pile moved into compost" state before the prompt starts.
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.GardenLeafPileRecord].State =
            StateId.Garden.LeafPileComposted;

        // IDA 0x12932..0x1293B: block through the fixed "the pile is well placed in the compost" line before the
        // alternate-portrait reload/decode tail runs.
        runtime.PromptController.RunTextAnimation(StringId.Garden_CompostUseOnLeafPileSuccess);

        // IDA 0x1293C..0x12976: switch to the alternate hero portrait and refresh the backdrop surface from HELD_S.LBM.
        runtime.State.RawDataBlock.Control.UseAlternateHeroPortrait = true;

        var portraitLoadBuffer =
            runtime.ContentFileLoader.LoadOrThrow(AssetId.HeroPortraitAlternate);
        runtime.LbmDecoder.DecodeIntoBuffer(portraitLoadBuffer,
            runtime.State.Program.ScratchBuffers.Backdrop,
            DisplayCompatibilityState.StrideBytes);
    }
}
