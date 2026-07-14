using Game.Catalogs;
using Game.Display;
using Game.State;
using Game.Text;

namespace Game.Runtime.Rooms.House;

internal sealed class HouseBusRideHandler(Erbe runtime)
{
    private const int BusFareAvailableStateIndex = 0x09;

    internal void RunBusRide()
    {
        // IDA 0x11E5F..0x11E70: when the bus-fare gate is clear, ignore the forwarded selection index and dispatch the
        // fixed "Ohne Moos Nix Los." line through the shared timed transition-text seam.
        if (runtime.State.RawDataBlock.Control.TransitionTextEntryStates[BusFareAvailableStateIndex] == 0)
        {
            runtime.PromptController.RunTextAnimation(StringId.Shared_BusStopNoMoney);
            return;
        }

        // IDA 0x11E73..0x11EA5: refresh the retained full-screen source surface from GRAFIK/BUS.LBM before the bus
        // animation starts.
        runtime.FullScreenSourceSurface.Reload(AssetId.Bus);

        short busDestinationColumn = RuntimeState.FrameWidth;
        while (busDestinationColumn > 280)
        {
            // IDA 0x11EA8..0x11F89: restore the retained snapshot, rebuild the current scene with the existing
            // backdrop state, blit the transparent bus body plus both animated wheel regions from BUS.LBM, then
            // advance the shared pointer/publication cadence before the next 4-pixel step.
            var wheelAnimationFrameIndex = checked((ushort)(3 - (busDestinationColumn >> 3) % 4));
            ReplayBusFrame(busDestinationColumn, wheelAnimationFrameIndex);
            busDestinationColumn -= 4;
            runtime.HostPacing.WaitFrame();
        }

        // IDA 0x11F97..0x11F9C: suppress the program-scene backdrop once the bus has arrived before the fixed hold
        // and departure phases begin.
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 0;

        // IDA 0x11F9C..0x11FAE: adapt the original 100x waitForVerticalRetrace hard wait into 100 host-paced frame
        // waits so the bus-at-stop hold period remains externally paced without resumable state.
        runtime.HostPacing.WaitFrames(100);

        busDestinationColumn = 280;
        while (busDestinationColumn > -170)
        {
            // IDA 0x11FAF..0x12090: after the hold, keep the backdrop suppressed while replaying the same bus body
            // and wheel overlays as the bus continues four pixels left per step until it is fully off screen.
            var wheelAnimationFrameIndex = checked((ushort)(3 - ((busDestinationColumn + 200) >> 3) % 4));
            ReplayBusFrame(busDestinationColumn, wheelAnimationFrameIndex);
            busDestinationColumn -= 4;
            runtime.HostPacing.WaitFrame();
        }

        // IDA 0x120A1: restore the normal backdrop-emission flag after the departure animation leaves the scene.
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 1;
    }

    private void ReplayBusFrame(short busDestinationColumn, ushort wheelAnimationFrameIndex)
    {
        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.ProgramScene.RenderCurrentScene();
        DrawBusFrame(busDestinationColumn, wheelAnimationFrameIndex);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }

    private void DrawBusFrame(short busDestinationColumn, ushort wheelAnimationFrameIndex)
    {
        var busBodyRegion = new DisplayCopyRegion(156, 61, 0, 0, 67, busDestinationColumn);
        runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            busBodyRegion);

        var wheelSourceColumn = checked((short)(wheelAnimationFrameIndex * 19 + 1));

        var frontWheelRegion = new DisplayCopyRegion(13,
            11,
            67,
            wheelSourceColumn,
            112,
            checked((short)(busDestinationColumn + 20)));
        runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            frontWheelRegion);

        var rearWheelRegion = frontWheelRegion with
        {
            DestinationColumn = checked((short)(busDestinationColumn + 111))
        };

        runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            rearWheelRegion);
    }
}
