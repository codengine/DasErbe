using Game.Catalogs;
using Game.Display;
using Game.Shared.RE;
using Game.State;
using Game.Text;

namespace Game.Runtime.Rooms.City;

internal sealed class CityBusRideHandler(Erbe runtime)
{
    private const int BusFareAvailableStateIndex = 0x09;

    [FunctionSymbol("sub_120AA", 0x120AA)]
    internal void RunBusRide()
    {
        // IDA 0x120B0..0x120C1: the city-side bus-stop callback shares the bus-fare gate and fixed no-money line
        // with the house bus-stop flow before entering its city-specific BUS.LBM animation branch.
        if (runtime.State.RawDataBlock.Control.TransitionTextEntryStates[BusFareAvailableStateIndex] == 0)
        {
            runtime.PromptController.RunTextAnimation(StringId.Shared_BusStopNoMoney);
            return;
        }

        // IDA 0x120C4..0x120F6: refresh the retained full-screen source surface from GRAFIK/BUS.LBM before the city
        // bus animation starts.
        runtime.FullScreenSourceSurface.Reload(AssetId.Bus);

        short busDestinationColumn = RuntimeState.FrameWidth;
        while (busDestinationColumn > 120)
        {
            // IDA 0x12101..0x121DC: restore the retained snapshot, rebuild the current scene with the existing
            // backdrop state, blit the larger city-side bus body plus both wheel overlays from BUS.LBM, then advance
            // the shared pointer/publication cadence before the next 4-pixel step.
            var wheelAnimationFrameIndex = checked((ushort)(3 - (busDestinationColumn >> 3) % 4));
            ReplayBusFrame(busDestinationColumn, wheelAnimationFrameIndex);
            busDestinationColumn -= 4;
            runtime.HostPacing.WaitFrame();
        }

        // IDA 0x121E5..0x121EA: suppress the program-scene backdrop once the bus has arrived before the fixed hold
        // and departure phases begin.
        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = 0;

        // IDA 0x121F1..0x121FB: adapt the original 100x waitForVerticalRetrace hard wait into 100 host-paced frame
        // waits while the bus remains parked.
        runtime.HostPacing.WaitFrames(100);

        busDestinationColumn = 120;
        while (busDestinationColumn > -230)
        {
            // IDA 0x12205..0x122E3: after the hold, keep the backdrop suppressed while replaying the same city-side
            // bus body and wheel overlays as the bus continues left until it is fully off screen.
            var wheelAnimationFrameIndex = checked((ushort)(3 - ((busDestinationColumn + 240) >> 3) % 4));
            ReplayBusFrame(busDestinationColumn, wheelAnimationFrameIndex);
            busDestinationColumn -= 4;
            runtime.HostPacing.WaitFrame();
        }

        // IDA 0x122ED: restore the normal backdrop-emission flag after the departure animation leaves the scene.
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
        var busBodyRegion = new DisplayCopyRegion(221, 88, 79, 0, 40, busDestinationColumn);
        runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            busBodyRegion);

        var frontWheelRegion = new DisplayCopyRegion(19,
            15,
            180,
            checked((short)(wheelAnimationFrameIndex * 25)),
            106,
            checked((short)(busDestinationColumn + 28)));
        runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            frontWheelRegion);

        var rearWheelRegion = frontWheelRegion with
        {
            DestinationColumn = checked((short)(busDestinationColumn + 157))
        };

        runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            rearWheelRegion);
    }
}
