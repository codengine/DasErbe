using Game.Display;
using Game.Input;
using Game.Shared.RE;
using Game.State;
using Game.Text;

namespace Game.Runtime.Rooms.Garden;

internal sealed class GardenLoungerHandler(Erbe runtime)
{
    [FunctionSymbol("sub_12996", 0x12996)]
    internal void RunSunbathing()
    {
        // IDA 0x1299C..0x129AB: suppress backdrop rendering before the modal lounger overlay loop starts.
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 0;

        ushort animationGroupIndex = 0;
        ushort animationRepeatIndex = 0;
        var lastInput = RuntimeInputEvent.None;

        var animationRegion = new DisplayCopyRegion(79, 19, 150, 0, 95, 104);

        while (animationGroupIndex < 8)
        {
            animationRegion.SourceRow = checked((short)(animationGroupIndex / 4 * 20 + 150));
            animationRegion.SourceColumn = checked((short)(animationGroupIndex % 4 * 80));

            // IDA 0x129B6..0x12A5B: poll before drawing, then rebuild the snapshot-backed scene with backdrop
            // suppressed, blit the current lounger pose from the decoded scene source surface, advance the shared
            // transition band, render the centered stand-up prompt, expose the frame through the pointer overlay
            // cadence, and preserve the original primary-confirm early-exit check before the next repeated frame.
            lastInput = runtime.InputAdapter.PollInputEvent();
            if (lastInput.IsPrimaryConfirmAction)
            {
                break;
            }

            runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
            runtime.ProgramScene.RenderCurrentScene();
            runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
                runtime.State.Program.ScratchBuffers.Scene,
                animationRegion);
            runtime.TransitionEffect.AdvanceTransitionEffect();

            var standUpPromptWidthPixels =
                runtime.TextRenderer.MeasureStringWidthPixels(StringId.Garden_LoungerStandUpPrompt);
            var textCursorColumn = (ushort)((RuntimeState.FrameWidth - standUpPromptWidthPixels) >> 1);
            runtime.TextCursor.SeedTextBlockCursor(textCursorColumn, 131);
            runtime.TextRenderer.RenderStringBlock(StringId.Garden_LoungerStandUpPrompt);
            runtime.PointerOverlay.AdvancePointerOverlayFrame();

            animationRepeatIndex++;
            if (animationRepeatIndex >= 10)
            {
                animationRepeatIndex = 0;
                animationGroupIndex++;
            }

            runtime.HostPacing.WaitFrame();
        }

        // IDA 0x12A6D..0x12A87: re-enable backdrop rendering, then queue the fixed UV-warning text-only branch
        // unless the loop exited on the primary confirm action.
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 1;
        if (!lastInput.IsPrimaryConfirmAction)
        {
            runtime.PromptController.QueueTextAnimationWithErrors(StringId.Garden_LoungerUvWarning, 5);
        }
    }
}
