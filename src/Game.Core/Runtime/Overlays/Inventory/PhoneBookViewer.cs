using Game.Catalogs;
using Game.Display;
using Game.Input;
using Game.State;

namespace Game.Runtime.Overlays.Inventory;

/// <summary>
///     Owns the blocking phone-book image-viewer flow.
/// </summary>
/// <param name="runtime">Runtime owner that provides retained surfaces and shared prompt state.</param>
internal sealed class PhoneBookViewer(Erbe runtime)
{
    /// <summary>
    ///     Runs the phone-book read-display helper to completion.
    /// </summary>
    internal void RunDisplayPhonebook()
    {
        var phoneBookRegion = new DisplayCopyRegion(RuntimeState.FrameWidth, RuntimeState.StageHeight, 0, 0, 0, 0);

        // IDA 0x13A56..0x13A88: reviewed exception. This symbol reloads TELEBUCH.LBM into the retained full-screen
        // source surface before the blocking phone-book presentation begins.
        runtime.FullScreenSourceSurface.Reload(AssetId.PhoneBook);

        // IDA 0x13A8B..0x13ABC: blit the top 320x128 phone-book image once and expose that first modal frame before
        // the second blit enters the polling loop.
        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0, runtime.FullScreenSourceSurface.Buffer, phoneBookRegion);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        do
        {
            // IDA 0x13ABF..0x13ADC: blit the same phone-book frame again, then keep flipping the pointer overlay
            // until any semantic input event arrives, exposing each visible polling frame at the original sub_1155E
            // cadence.
            runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
                runtime.FullScreenSourceSurface.Buffer,
                phoneBookRegion);
            runtime.PointerOverlay.AdvancePointerOverlayFrame();
        } while (runtime.InputAdapter.PollInputEvent() == RuntimeInputEvent.None);

        // IDA 0x13ADE..0x13B05: restore the retained top-of-screen snapshot twice and expose both cleanup frames
        // before the interaction loop redraws the prompt panel.
        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }
}
