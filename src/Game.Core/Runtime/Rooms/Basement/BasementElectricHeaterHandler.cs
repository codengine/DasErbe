using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Display;
using Game.Shared.RE;
using Game.State;
using Game.Text;

namespace Game.Runtime.Rooms.Basement;

/// <summary>
///     Replays the basement electric-heater outlet animation and queues its follow-up warning branch.
/// </summary>
/// <param name="runtime">Runtime owner that provides scene, display, pacing, and prompt services.</param>
internal sealed class BasementElectricHeaterHandler(Erbe runtime)
{
    /// <summary>
    ///     Runs the basement electric-heater outlet interaction through replay completion and queued warning
    ///     publication.
    /// </summary>
    [FunctionSymbol("sub_12ADA", 0x12ADA)]
    internal void RunUseOutletOnElectricHeater()
    {
        // IDA 0x12AE0..0x12AEB: publish the temporary heater replay state and suppress backdrop rendering before the
        // animation loop starts.
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BasementElectricHeaterRecord].State =
            StateId.Heater.Replay;
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 0;

        ushort animationGroupIndex = 0;
        ushort animationRepeatIndex = 0;
        var animationRegion = new DisplayCopyRegion(57, 61, 9, 82, 57, 249);

        while (animationGroupIndex < 20)
        {
            animationRegion.SourceColumn = checked((short)(82 + animationGroupIndex % 3 * 71));

            // IDA 0x12AF9..0x12B62: restore the retained snapshot, rebuild the current scene with backdrop
            // suppressed, blit the current transparent heater frame from the decoded scene source surface, invoke the
            // backdrop seam in its original ordering, expose the frame through the pointer overlay cadence, then roll
            // from the second replay into the next heater frame group.
            runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
            runtime.ProgramScene.RenderCurrentScene();
            runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0,
                runtime.State.Program.ScratchBuffers.Scene,
                animationRegion);
            runtime.ProgramScene.RenderBackdrop();
            runtime.PointerOverlay.AdvancePointerOverlayFrame();

            animationRepeatIndex++;
            if (animationRepeatIndex >= 2)
            {
                animationRepeatIndex = 0;
                animationGroupIndex++;
            }

            // IDA 0x12AF9..0x12B62 intent note: this replay adds one symbol-local host-frame wait after the shared
            // pointer-overlay presentation seam. Changing the shared overlay cadence regressed other animations, so
            // this compatibility pacing remains local to sub_12ADA.
            runtime.HostPacing.WaitFrame();
        }

        // IDA 0x12B68..0x12B82: restore the stable heater state, re-enable backdrop rendering, then queue the fixed
        // variant-A warning branch with selection-count increment 5.
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BasementElectricHeaterRecord].State =
            StateId.Heater.Stable;
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 1;
        runtime.PromptController.QueueOzoneAlternateSceneTransition(StringId.Basement_UseOutletOnElectricHeaterWarning,
            5);
    }
}
