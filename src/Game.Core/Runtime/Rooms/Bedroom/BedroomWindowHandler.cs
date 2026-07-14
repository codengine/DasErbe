using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Display;
using Game.Shared.RE;
using Game.State;
using Game.Text;

namespace Game.Runtime.Rooms.Bedroom;

internal sealed class BedroomWindowHandler(Erbe runtime)
{
    internal void RunWindowOpen(ushort selectionIndex)
    {
        // IDA 0x12F8C..0x12FB0: when the reviewed bedroom heater state is not 0x0080, this handler does not enter the
        // animation branch and instead delegates directly to the shared persistent-data state-mask toggle seam.
        if (runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomHeaterRecord].State !=
            StateId.Bedroom.HeaterActive)
        {
            runtime.SaveScreenController.DispatchToggleSelectionDataState(selectionIndex);
            return;
        }

        RunOpenWindowWithActiveHeaterAnimation();

        // IDA 0x12F97..0x12FA6: queue the fixed bedroom warning follow-up through the shared variant-A selector with
        // selection-count increment 5 after the helper replay finishes.
        runtime.PromptController.QueueOzoneAlternateSceneTransition(StringId.Bedroom_WindowOpenHeaterWarning, 5);
    }

    [FunctionSymbol("sub_12F0D", 0x12F0D)]
    private void RunOpenWindowWithActiveHeaterAnimation()
    {
        var animationRegion = new DisplayCopyRegion(48, 48, 144, 0, 56, 165);

        for (ushort animationGroupIndex = 0; animationGroupIndex < 6; animationGroupIndex++)
        {
            for (ushort animationRepeatIndex = 0; animationRepeatIndex < 3; animationRepeatIndex++)
            {
                animationRegion.SourceColumn = checked((short)(animationGroupIndex * 48));

                // IDA 0x12F21..0x12F73: restore the retained snapshot, rebuild the current scene, blit the current
                // window animation frame from the decoded scene source surface, invoke the shared backdrop seam in its
                // original ordering, then advance the pointer overlay before the next host-paced frame.
                runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0,
                    0,
                    RuntimeState.FrameWidth,
                    RuntimeState.StageHeight);
                runtime.ProgramScene.RenderCurrentScene();
                runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
                    runtime.State.Program.ScratchBuffers.Scene,
                    animationRegion);
                runtime.ProgramScene.RenderBackdrop();
                runtime.PointerOverlay.AdvancePointerOverlayFrame();
                runtime.HostPacing.WaitFrame();
            }
        }
    }
}
