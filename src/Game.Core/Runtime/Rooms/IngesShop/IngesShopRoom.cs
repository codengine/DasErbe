using System.Collections.Frozen;
using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Runtime.Overlays.Inventory;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Rooms.IngesShop;

/// <summary>
///     Owns Inge's shop interaction handlers and their local blocking helpers.
/// </summary>
/// <param name="runtime">Runtime owner that provides shared prompt state and session data.</param>
internal sealed class IngesShopRoom(Erbe runtime)
{
    private const ushort StampDiscoveryTextEntryIndex = 0x000E;

    private static readonly FrozenDictionary<InteractionHandlerId, StringId> SimpleTextHandlers =
        new Dictionary<InteractionHandlerId, StringId>
        {
            { InteractionHandlerId.IngesShop_InspectCashRegister, StringId.IngesShop_CashRegisterInspection },
            { InteractionHandlerId.IngesShop_TakeCashRegister, StringId.IngesShop_CashRegisterTakeRejection },
            { InteractionHandlerId.IngesShop_InspectShelf, StringId.IngesShop_ShelfInspection },
            { InteractionHandlerId.IngesShop_InspectBell, StringId.IngesShop_BellInspection },
            { InteractionHandlerId.IngesShop_LaterDismissal, StringId.IngesShop_LaterDismissal },
            {
                InteractionHandlerId.IngesShop_InspectCo2FireExtinguisher,
                StringId.IngesShop_Co2FireExtinguisherInspection
            },
            { InteractionHandlerId.IngesShop_InspectFireExtinguisher, StringId.IngesShop_FireExtinguisherInspection },
            {
                InteractionHandlerId.IngesShop_InspectHalonifaxFireExtinguisher,
                StringId.IngesShop_HalonifaxFireExtinguisherInspection
            }
        }.ToFrozenDictionary();

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
            case InteractionHandlerId.IngesShop_UseBell:
                DispatchUseBell();
                return true;

            // handlerByteOffset +0x0C / controlMask 0x0080
            case InteractionHandlerId.IngesShop_BuyHalonifaxFireExtinguisher:
                DispatchBuyHalonifaxFireExtinguisher();
                return true;

            // handlerByteOffset +0x0C / controlMask 0x0080
            case InteractionHandlerId.IngesShop_BuyFireExtinguisher:
                DispatchBuyFireExtinguisher();
                return true;

            // handlerByteOffset +0x0C / controlMask 0x0080
            case InteractionHandlerId.IngesShop_BuyWhiteWritingPaper:
                DispatchBuyWhiteWritingPaper();
                return true;

            // handlerByteOffset +0x0C / controlMask 0x0080
            case InteractionHandlerId.IngesShop_BuySprayCream:
                DispatchBuySprayCream();
                return true;

            // handlerByteOffset +0x0C / controlMask 0x0080
            case InteractionHandlerId.Inventory_BuyRecycledWritingPaper:
                DispatchBuyRecycledWritingPaper();
                return true;

            // handlerByteOffset +0x0C / controlMask 0x0080
            case InteractionHandlerId.Inventory_BuyLiquidWhippedCream:
                DispatchBuyLiquidWhippedCream();
                return true;

            default:
                return false;
        }
    }

    [FunctionSymbol("sub_13827", 0x13827)]
    private void DispatchUseBell()
    {
        // IDA 0x1382A..0x13831: enable transition-text entry 0x000E and publish it as the current start index
        // through the shared selectTransitionTextEntry seam sub_14270 before the discovery prompt runs.
        runtime.PromptController.SelectTransitionTextEntry(StampDiscoveryTextEntryIndex);

        // IDA 0x13832..0x1383C: block through the fixed stamp-discovery line via the shared timed
        // transition-text seam sub_11A71.
        runtime.PromptController.RunTextAnimation(StringId.IngesShop_StampDiscovery);
    }

    [FunctionSymbol("sub_1387A", 0x1387A)]
    private void DispatchBuyHalonifaxFireExtinguisher()
    {
        // IDA 0x1387D..0x1388B: queue the fixed ozone-layer warning for the Halonifax fire-extinguisher purchase row
        // through the shared alternate-scene queued transition seam sub_14190 with selection-count increment 0x0002.
        runtime.PromptController.QueueOzoneSceneTransition(
            StringId.IngesShop_HalonifaxFireExtinguisherPurchaseOzoneWarningQueued,
            2);
    }

    [FunctionSymbol("sub_13BB8", 0x13BB8)]
    private void DispatchBuyFireExtinguisher()
    {
        // IDA 0x13BBB..0x13BC1: material side effect: latch the reviewed villa-condition bit 0x0800 before the
        // delivery prompt runs.
        runtime.State.RawDataBlock.Control.StoryProgress.MarkFireExtinguisherPurchased();

        // IDA 0x13BC1..0x13BC7: material side effect: publish heating-basement fire-extinguisher state 0x0008 before
        // the delivery prompt runs.
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BasementFireExtinguisherRecord].State =
            StateId.Workflow.Completed;

        // IDA 0x13BC7..0x13BD1: block through the fixed extinguisher-delivery line via the shared timed
        // transition-text seam sub_11A71.
        runtime.PromptController.RunTextAnimation(StringId.IngesShop_FireExtinguisherPurchaseDelivery);
    }

    [FunctionSymbol("sub_137F5", 0x137F5)]
    private void DispatchBuyWhiteWritingPaper()
    {
        // IDA 0x137F5..0x13808: queue the fixed recycled-paper warning for the white-writing-paper buy row and bump
        // the selection count by 0x0002 through the shared sub_1416D seam.
        runtime.PromptController.QueueOzoneAlternateSceneTransition(
            StringId.Inventory_WhiteWritingPaperBuyRecyclingWarningQueued,
            2);
    }

    [FunctionSymbol("sub_137B4", 0x137B4)]
    private void DispatchBuySprayCream()
    {
        // IDA 0x137B4..0x137C7: queue the fixed ozone-damaging propellant warning for the spray-cream buy row
        // and bump the selection count by 0x0002 through the shared program-state alternate-scene seam sub_14190.
        runtime.PromptController.QueueOzoneSceneTransition(
            StringId.Inventory_SprayCreamPurchaseOzoneDamagingPropellantWarningQueued,
            2);
    }

    [FunctionSymbol("sub_13BAB", 0x13BAB)]
    private void DispatchBuyRecycledWritingPaper()
    {
        // IDA 0x13BAB..0x13BB6: enable transition-text entry 0x000D through the shared sub_14270 seam.
        runtime.PromptController.SelectTransitionTextEntry(InventoryOverlay.RecycledWritingPaperBuyTextEntryIndex);
    }

    [FunctionSymbol("sub_13BD3", 0x13BD3)]
    private void DispatchBuyLiquidWhippedCream()
    {
        // IDA 0x13BD3..0x13BDD: enable transition-text entry 0x0007 through the shared sub_14270 seam.
        runtime.PromptController.SelectTransitionTextEntry(InventoryOverlay.LiquidWhippedCreamBuyTextEntryIndex);

        // IDA 0x13BDE..0x13BE3: the original helper redundantly republishes TransitionTextStartIndex = 7 after
        // sub_14270. SelectTransitionTextEntry already leaves the same observable final state, so the managed owner
        // keeps this collapsed into the shared prompt-selection seam.
    }
}
