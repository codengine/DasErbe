using Game.Catalogs;
using Game.DataBlock.Scene;
using Game.DataBlock.Selection;
using Game.Shared.Diagnostics;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Rooms.Bedroom;

internal sealed class BedroomHeaterHandler(Erbe runtime)
{
    internal void RunUseHeater()
    {
        // IDA 0x12ED8..0x12F04: when the reviewed bedroom heater state is not 0x0080, this callback jumps straight
        // into the existing heater inspection symbol sub_12EB8 with the original forwarded selection index.
        if (runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomHeaterRecord].State !=
            StateId.Bedroom.HeaterActive)
        {
            RunInspectHeater();
            return;
        }

        // IDA 0x12EE0..0x12EF2: material side effects before the blocking prompt: clear the bedroom heater state,
        // latch villa-condition bit 0x1000, clear the bedroom re-entry follow-up text in SceneDescriptor[0x09], and
        // clear the executable-backed bedroom door-exit current-selection handler slot at selection global index
        // 0x0076.
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomHeaterRecord].State =
            StateId.Bedroom.HeaterInactive;
        runtime.State.RawDataBlock.Control.StoryProgress.MarkBedroomHeaterResolved();
        runtime.Scenes.SetOptionalTransitionText(SceneCatalog.Bedroom, StringId.None);

        var doorExitHandlerId = runtime.Interactions.ReadHandlerId(BedroomDoorExitInteractionSlot.SelectionGlobalIndex,
            BedroomDoorExitInteractionSlot.CurrentSelectionHandlerByteOffset);
        if (doorExitHandlerId != InteractionHandlerId.None)
        {
            //Fixes a softlock
            GameLog.Warning(LoggingChannel.Runtime,
                $"Correcting bedroom door-exit transient handler misbehavior: selectionGlobal=0x{BedroomDoorExitInteractionSlot.SelectionGlobalIndex:X4} " +
                $"handlerByteOffset=+0x{BedroomDoorExitInteractionSlot.CurrentSelectionHandlerByteOffset:X2} old=0x{(ushort)doorExitHandlerId:X4} new=0x0000.");
        }

        runtime.Interactions.SetHandlerId(BedroomDoorExitInteractionSlot.SelectionGlobalIndex,
            BedroomDoorExitInteractionSlot.CurrentSelectionHandlerByteOffset,
            InteractionHandlerId.None);

        // IDA 0x12EF8..0x12F02: publish the fixed "good idea to turn the heater off" line through the shared timed
        // transition-text seam sub_11A71 after the hot-branch state publications above.
        runtime.PromptController.RunTextAnimation(StringId.Bedroom_HeaterTurnOffSuggestion);
    }

    [FunctionSymbol("sub_12EB8", 0x12EB8)]
    internal void RunInspectHeater()
    {
        var stringId =
            runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomHeaterRecord].State ==
            StateId.Bedroom.HeaterActive
                ? StringId.Bedroom_InspectHeaterActive
                : StringId.Bedroom_InspectHeaterInactive;
        runtime.PromptController.RunTextAnimation(stringId);
    }
}
