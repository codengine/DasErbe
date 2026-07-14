using System.Collections.Frozen;
using Game.Catalogs;
using Game.Shared.RE;
using Game.State;
using Game.Text;

namespace Game.Runtime.Rooms.City;

internal sealed class CityRoom(Erbe runtime)
{
    private const ushort BrochureTakeTextEntryIndex = 0x0003;
    private const ushort QuickLaundromatOutsideBrochureSceneEntry = 0x0084;
    private const ushort QuickLaundromatInsideBrochureSceneEntry = 0x00A2;

    private static readonly FrozenDictionary<InteractionHandlerId, StringId> SimpleTextHandlers =
        new Dictionary<InteractionHandlerId, StringId>
        {
            { InteractionHandlerId.City_InspectWashingMachine, StringId.City_WashingMachineInspection }
        }.ToFrozenDictionary();

    private readonly CityBusRideHandler _busRideHandler = new(runtime);
    private readonly CityWashingMachineHandler _washingMachineHandler = new(runtime);

    internal bool TryDispatchInteraction(InteractionHandlerId handlerId)
    {
        if (SimpleTextHandlers.TryGetValue(handlerId, out var stringId))
        {
            runtime.PromptController.RunTextAnimation(stringId);
            return true;
        }

        switch (handlerId)
        {
            // handlerByteOffset +0x02 / controlMask 0x0020
            case InteractionHandlerId.Inventory_TakeBrochure:
                DispatchTakeBrochure();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.City_UseBusStop:
                _busRideHandler.RunBusRide();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.City_UseWashingMachine:
                _washingMachineHandler.RunUseWashingMachine();
                return true;

            // handlerByteOffset +0x0E / controlMask 0x0008
            case InteractionHandlerId.City_UseQuickLaundromatDoor:
                DispatchUseQuickLaundromatDoor();
                return true;

            default:
                return false;
        }
    }

    [FunctionSymbol("sub_13DEC", 0x13DEC)]
    private void DispatchUseQuickLaundromatDoor()
    {
        // IDA 0x13DEF..0x13DF5: publish backdrop selection row 3 before the queued alternate-scene prompt runs.
        runtime.State.RawDataBlock.Control.BackdropSelectionRow = 3;

        // IDA 0x13DF5..0x13DF9: force the default hero portrait for the quick-laundromat warning presentation.
        runtime.State.RawDataBlock.Control.UseAlternateHeroPortrait = false;

        // IDA 0x13DFA..0x13E2C: refresh the backdrop source surface from GRAFIK/HELD.LBM.
        var portraitLoadBuffer = runtime.ContentFileLoader.LoadOrThrow(AssetId.HeroPortrait);
        runtime.LbmDecoder.DecodeIntoBuffer(portraitLoadBuffer,
            runtime.State.Program.ScratchBuffers.Backdrop,
            DisplayCompatibilityState.StrideBytes);

        // IDA 0x13E2F..0x13E3B: queue the fixed quick-laundromat environmental warning through PromptController's
        // alternate-scene queued transition seam sub_14190 with selection-count increment 0x0002.
        runtime.PromptController.QueueOzoneSceneTransition(StringId.City_QuickLaundromatDoorEnvironmentalWarning, 2);
    }

    [FunctionSymbol("sub_138AC", 0x138AC)]
    private void DispatchTakeBrochure()
    {
        // IDA 0x138AF..0x138B8: enable transition-text entry 0x0003 and publish that same entry as the current
        // transition-text panel start index through sub_14270.
        runtime.PromptController.SelectTransitionTextEntry(BrochureTakeTextEntryIndex);

        // Port ownership note: the shared IDA 0x150D5..0x15108 take branch marks only the descriptor that was
        // clicked. The quick-laundromat brochure exists as duplicated inside/outside scene entries, so the item
        // handler publishes both persistent descriptor bytes together to keep live play and exported save files from
        // re-showing the outside copy after the inside copy was taken.
        runtime.Interactions.SetSelectionState(
            InteractionDescriptorRef.SceneEntry(QuickLaundromatInsideBrochureSceneEntry),
            StateId.Disabled);
        runtime.Interactions.SetSelectionState(
            InteractionDescriptorRef.SceneEntry(QuickLaundromatOutsideBrochureSceneEntry),
            StateId.Disabled);
    }
}
