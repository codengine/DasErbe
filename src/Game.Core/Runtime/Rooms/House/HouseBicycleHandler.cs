using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Display;
using Game.State;
using Game.Text;

namespace Game.Runtime.Rooms.House;

internal sealed class HouseBicycleHandler(Erbe runtime)
{
    private const string BicycleUnlockCode = "007";

    internal void RunUnlockWithKeypad()
    {
        // IDA 0x126C3..0x126F5: refresh the shared full-screen source surface from DISPLAY.LBM before sub_125EA
        // starts.
        runtime.FullScreenSourceSurface.Reload(AssetId.DisplayBackdrop);

        // IDA 0x126F8..0x12708: run the shared keypad helper and then compare the entered managed keypad input
        // against the fixed executable-backed unlock code.
        runtime.KeypadOverlay.RunCodeEntryPanel();

        var resultPromptText = StringId.House_BicycleWrongCode;
        if (runtime.KeypadOverlay.CurrentInput == BicycleUnlockCode)
        {
            var bicycleState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BicycleRecord];

            // IDA 0x12710..0x12733: a correct code toggles the bicycle lock state from locked+flat -> flat or from
            // locked -> ready, with the neighboring ready-state marker published only on the second branch.
            switch (bicycleState.State)
            {
                case StateId.House.Bicycle.Flat:
                    bicycleState.State = StateId.House.Bicycle.Flat | StateId.Open;
                    break;

                case StateId.House.Bicycle.Ready:
                    bicycleState.State = StateId.House.Bicycle.Ready | StateId.Open;
                    bicycleState.StateId = StateId.House.Bicycle.Ready;
                    break;
            }

            resultPromptText = StringId.House_BicycleUnlocked;
        }

        // IDA 0x12733..0x12743: publish the success or failure line through the shared timed transition-text seam.
        runtime.PromptController.RunTextAnimation(resultPromptText);
    }

    internal void RunRide()
    {
        var bicycleState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BicycleRecord];

        // IDA 0x11CC5..0x11D05: refresh the retained full-screen source surface from GRAFIK/FAHRRADA.LBM, then
        // publish the transient bicycle animation state value 0x0005 and suppress the
        // backdrop seam while the two-phase bicycle-use animation is replayed.
        runtime.FullScreenSourceSurface.Reload(AssetId.BicyclePump);
        bicycleState.State = StateId.Disabled;
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 0;

        var animationRegion = new DisplayCopyRegion(50, 53, 3, 0, 60, 153);

        for (ushort mountAnimationGroupIndex = 0; mountAnimationGroupIndex < 4; mountAnimationGroupIndex++)
        {
            for (ushort mountAnimationRepeatIndex = 0; mountAnimationRepeatIndex < 4; mountAnimationRepeatIndex++)
            {
                animationRegion.SourceRow = checked((short)(mountAnimationGroupIndex / 6 * 54 + 3));
                animationRegion.SourceColumn = checked((short)(mountAnimationGroupIndex % 6 * 50));

                // IDA 0x11D13..0x11D90: restore the retained snapshot, rebuild the scene with the backdrop seam
                // suppressed, blit the current transparent bicycle frame from the decoded FAHRRADA.LBM surface, then
                // advance the shared pointer/publication cadence before the next replay.
                ReplayBicycleFrame(animationRegion);
                runtime.HostPacing.WaitFrame();
            }
        }

        for (ushort rideOffset = 0; rideOffset < 204; rideOffset += 4)
        {
            var animationFrameIndex = checked((ushort)(rideOffset / 8 % 6 + 5));
            animationRegion.SourceRow = checked((short)(animationFrameIndex / 6 * 54 + 3));
            animationRegion.SourceColumn = checked((short)(animationFrameIndex % 6 * 50));
            animationRegion.DestinationColumn = checked((short)(153 - rideOffset));

            // IDA 0x11D93..0x11E1D: on the ride-away phase, rebuild the snapshot-backed scene each step, pick the
            // next transparent bicycle frame from indices 5..10 of the decoded FAHRRADA.LBM sheet, shift the
            // destination column four pixels left per iteration, then advance the shared pointer/publication cadence.
            ReplayBicycleFrame(animationRegion);
            runtime.HostPacing.WaitFrame();
        }

        // IDA 0x11E20..0x11E25: restore the normal backdrop-emission flag and publish the ready-bike state again after
        // the animation completes.
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 1;
        bicycleState.State = StateId.House.Bicycle.Ready | StateId.Open;
    }

    private void ReplayBicycleFrame(DisplayCopyRegion animationRegion)
    {
        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.ProgramScene.RenderCurrentScene();
        runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            animationRegion);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }
}
