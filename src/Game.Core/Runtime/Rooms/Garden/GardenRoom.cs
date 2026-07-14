using System.Collections.Frozen;
using Game.Catalogs;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Rooms.Garden;

/// <summary>
///     Owns garden interaction handlers and their local blocking helpers.
/// </summary>
/// <param name="runtime">Runtime owner that provides shared prompt state and session data.</param>
internal sealed class GardenRoom(Erbe runtime)
{
    private const ushort StrawberryTakeTextEntryIndex = 0x000C;

    private static readonly FrozenDictionary<InteractionHandlerId, StringId> SimpleTextHandlers =
        new Dictionary<InteractionHandlerId, StringId>
        {
            { InteractionHandlerId.Garden_InspectLeafPile, StringId.Garden_LeafPileInspection },
            { InteractionHandlerId.Garden_InspectLounger, StringId.Garden_LoungerInspection },
            { InteractionHandlerId.Garden_InspectCompost, StringId.Garden_CompostInspection },
            { InteractionHandlerId.Garden_InspectFence, StringId.Garden_FenceInspection },
            { InteractionHandlerId.Garden_InspectTree, StringId.Garden_TreeInspection },
            { InteractionHandlerId.Garden_InspectStrawberry, StringId.Garden_StrawberryInspection }
        }.ToFrozenDictionary();

    private readonly GardenCompostHandler _compostHandler = new(runtime);
    private readonly GardenLoungerHandler _loungerHandler = new(runtime);

    internal bool TryDispatchInteraction(InteractionHandlerId handlerId)
    {
        if (SimpleTextHandlers.TryGetValue(handlerId, out var stringId))
        {
            runtime.PromptController.RunTextAnimation(stringId);
            return true;
        }

        switch (handlerId)
        {
            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.Garden_UseLeafPileOnCompost:
                _compostHandler.RunUseLeafPileWithCompost();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.Garden_UseLounger:
                _loungerHandler.RunSunbathing();
                return true;

            // handlerByteOffset +0x02 / controlMask 0x0020
            case InteractionHandlerId.Garden_TakeStrawberries:
                DispatchTakeStrawberries();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.Garden_UseLighterOnCompost:
                _compostHandler.RunUseLighterWithCompost();
                return true;

            default:
                return false;
        }
    }

    [FunctionSymbol("sub_12A9A", 0x12A9A)]
    private void DispatchTakeStrawberries()
    {
        // IDA 0x12A9D..0x12AA4: enable transition-text entry 0x0C and publish that same entry as the current
        // transition-text panel start index through sub_14270.
        runtime.PromptController.SelectTransitionTextEntry(StrawberryTakeTextEntryIndex);

        // IDA 0x12AA5..0x12AAB: material side effect: publish the strawberry milestone in the shared Lolita-heart
        // progress tracker.
        runtime.State.RawDataBlock.Control.LolitaProgress.MarkStrawberriesTaken();
    }
}
