using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Display;
using Game.State;
using Game.Text;

namespace Game.Runtime.Rooms.Bedroom;

internal static class BedroomDoorExitInteractionSlot
{
    internal const ushort SelectionGlobalIndex = 0x0076;
    internal const int CurrentSelectionHandlerByteOffset = 0x0E;
}

internal sealed class BedroomDoorHandler(Erbe runtime)
{
    internal void RunDoorExit()
    {
        // IDA 0x13D4B..0x13D60: the door-exit hook only proceeds when the heater is still on and the bird is still
        // alive. Turning the heater off makes bedroom exit safe again.
        if (runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomHeaterRecord].State !=
            StateId.Bedroom.HeaterActive ||
            runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomBirdRecord].State !=
            StateId.Bedroom.BirdAlive)
        {
            return;
        }

        var animationRegion = new DisplayCopyRegion(26, 15, 5, 164, 51, 150);

        for (ushort animationGroupIndex = 0; animationGroupIndex < 3; animationGroupIndex++)
        {
            for (ushort animationRepeatIndex = 0; animationRepeatIndex < 4; animationRepeatIndex++)
            {
                animationRegion.SourceRow = checked((short)(animationGroupIndex * 17 + 5));

                // IDA 0x13D60..0x13DD0: restore the retained snapshot, rebuild the current scene, blit the current
                // transparent door frame from the decoded scene surface, then advance the pointer overlay before the
                // next replay iteration.
                runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0,
                    0,
                    RuntimeState.FrameWidth,
                    RuntimeState.StageHeight);
                runtime.ProgramScene.RenderCurrentScene();
                runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0,
                    runtime.State.Program.ScratchBuffers.Scene,
                    animationRegion);
                runtime.PointerOverlay.AdvancePointerOverlayFrame();
                runtime.HostPacing.WaitFrame();
            }
        }

        // IDA 0x13DD2..0x13DD8: material side effect on the unsafe hot-room exit path: publish the dead-bird
        // bedroom state value 0x0010 before the blocking mourning prompt starts.
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomBirdRecord].State =
            StateId.Bedroom.BirdDead;
        runtime.PromptController.RunTextAnimation(StringId.Bedroom_DoorExitKarlHeinzWarning);

        // IDA 0x13DE2: clear the executable-backed current-selection callback slot after the prompt completes.
        runtime.Interactions.SetHandlerId(BedroomDoorExitInteractionSlot.SelectionGlobalIndex,
            BedroomDoorExitInteractionSlot.CurrentSelectionHandlerByteOffset,
            InteractionHandlerId.None);
    }
}
