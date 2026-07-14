using System.Collections.Frozen;
using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Rooms.FurnitureStore;

/// <summary>
///     Owns furniture-store interaction handlers and their local blocking helpers.
/// </summary>
/// <param name="runtime">Runtime owner that provides shared prompt state and session data.</param>
internal sealed class FurnitureStoreRoom(Erbe runtime)
{
    private const ushort TakeBallpointPenTextEntryIndex = 0x0008;

    private static readonly FrozenDictionary<InteractionHandlerId, StringId> SimpleTextHandlers =
        new Dictionary<InteractionHandlerId, StringId>
        {
            { InteractionHandlerId.FurnitureStore_InspectRightFridge, StringId.FurnitureStore_RightFridgeInspection },
            {
                InteractionHandlerId.FurnitureStore_InspectRemoteControlledFridge,
                StringId.FurnitureStore_RemoteControlledFridgeInspection
            },
            { InteractionHandlerId.FurnitureStore_InspectOakBed, StringId.FurnitureStore_OakBedInspection },
            { InteractionHandlerId.FurnitureStore_InspectTropicanaBed, StringId.FurnitureStore_TropicanaBedInspection },
            {
                InteractionHandlerId.FurnitureStore_InspectFedermatMattress,
                StringId.FurnitureStore_FedermatMattressInspection
            },
            {
                InteractionHandlerId.FurnitureStore_InspectTropicanaMattress,
                StringId.FurnitureStore_TropicanaMattressInspection
            },
            {
                InteractionHandlerId.FurnitureStore_InspectChristophColumbusArmchair,
                StringId.FurnitureStore_ChristophColumbusArmchairInspection
            },
            {
                InteractionHandlerId.FurnitureStore_InspectSchaumiArmchair,
                StringId.FurnitureStore_SchaumiArmchairInspection
            },
            { InteractionHandlerId.FurnitureStore_OpenFridge, StringId.FurnitureStore_FridgeOpenUnnecessary },
            {
                InteractionHandlerId.FurnitureStore_SitOnChristophColumbusArmchair,
                StringId.FurnitureStore_ChristophColumbusArmchairSitReaction
            },
            { InteractionHandlerId.FurnitureStore_InspectSalesman, StringId.FurnitureStore_SalesmanInspection },
            { InteractionHandlerId.FurnitureStore_UseSalesman, StringId.FurnitureStore_SalesmanUseDismissal },
            { InteractionHandlerId.FurnitureStore_InspectGlassTable, StringId.FurnitureStore_GlassTableInspection },
            { InteractionHandlerId.FurnitureStore_InspectWoodenTable, StringId.FurnitureStore_WoodenTableInspection }
        }.ToFrozenDictionary();

    private readonly FurnitureStoreGlassTableHandler _glassTableHandler = new(runtime);

    internal bool TryDispatchInteraction(InteractionHandlerId handlerId)
    {
        if (SimpleTextHandlers.TryGetValue(handlerId, out var stringId))
        {
            runtime.PromptController.RunTextAnimation(stringId);
            return true;
        }

        switch (handlerId)
        {
            // handlerByteOffset +0x0C
            case InteractionHandlerId.FurnitureStore_BuyOakenBed:
                DispatchBuyOakenBed();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.FurnitureStore_BuyBed:
                DispatchBuyBed();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.FurnitureStore_BuyTropicanaMattress:
                DispatchBuyTropicanaMattress();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.FurnitureStore_BuySchaumiArmchair:
                DispatchBuySchaumiArmchair();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.FurnitureStore_BuyRemoteControlledFridge:
                DispatchBuyRemoteControlledFridge();
                return true;

            // handlerByteOffset +0x0C
            case InteractionHandlerId.FurnitureStore_BuyGlassTable:
                _glassTableHandler.RunPurchase();
                return true;

            // handlerByteOffset +0x0C
            case InteractionHandlerId.FurnitureStore_BuyWoodenTable:
                DispatchBuyWoodenTable();
                return true;

            // handlerByteOffset +0x0C
            case InteractionHandlerId.FurnitureStore_BuyRefrigerator:
                DispatchBuyRefrigerator();
                return true;

            // handlerByteOffset +0x0C
            case InteractionHandlerId.FurnitureStore_BuyChristophColumbusArmchair:
                DispatchBuyChristophColumbusArmchair();
                return true;

            // handlerByteOffset +0x02
            case InteractionHandlerId.FurnitureStore_TakeBallpointPen:
                // IDA 0x1376E..0x13776: enable transition-text entry 0x0008 and publish that same entry as the current
                // transition-text panel start index through sub_14270.
                runtime.PromptController.SelectTransitionTextEntry(TakeBallpointPenTextEntryIndex);
                return true;

            default:
                return false;
        }
    }

    [FunctionSymbol("sub_13676", 0x13676)]
    private void DispatchBuyBed()
    {
        // IDA 0x13679..0x13688: queue the fixed furniture-store bed-purchase rainforest warning through
        // the shared scene-variant-A queued transition seam sub_1416D with selection-count increment 0x0002.
        runtime.PromptController.QueueOzoneAlternateSceneTransition(
            StringId.FurnitureStore_BedPurchaseRainforestWarning,
            2);
    }

    [FunctionSymbol("sub_13662", 0x13662)]
    private void DispatchBuyTropicanaMattress()
    {
        // IDA 0x13665..0x13674: queue the fixed Tropicana-mattress purchase warning through the shared alternate-scene
        // queued transition seam sub_14190 with selection-count increment 0x0002.
        runtime.PromptController.QueueOzoneSceneTransition(
            StringId.FurnitureStore_TropicanaMattressPurchaseCfcFoamWarning,
            2);
    }

    [FunctionSymbol("sub_136A8", 0x136A8)]
    private void DispatchBuySchaumiArmchair()
    {
        // IDA 0x136AB..0x136BA: queue the fixed Schaumi-armchair purchase warning through the shared alternate-scene
        // queued transition seam sub_14190 with selection-count increment 0x0002.
        runtime.PromptController.QueueOzoneSceneTransition(
            StringId.FurnitureStore_SchaumiArmchairPurchaseCfcFoamWarning,
            2);
    }

    [FunctionSymbol("sub_136E9", 0x136E9)]
    private void DispatchBuyRemoteControlledFridge()
    {
        // IDA 0x136EC..0x136FB: queue the fixed remote-controlled-fridge purchase energy warning through the shared
        // scene-variant-A queued transition seam sub_1416D with selection-count increment 0x0001.
        runtime.PromptController.QueueOzoneAlternateSceneTransition(
            StringId.FurnitureStore_RemoteControlledFridgePurchaseEnergyWarning,
            1);
    }

    [FunctionSymbol("sub_13748", 0x13748)]
    private void DispatchBuyWoodenTable()
    {
        // IDA 0x1374B..0x1375A: queue the fixed wooden-table purchase warning through the shared text-only queued
        // transition seam sub_1414A with selection-count increment 0x0002.
        runtime.PromptController.QueueTextAnimationWithErrors(
            StringId.FurnitureStore_WoodenTablePurchaseFormaldehydeWarning,
            2);
    }

    [FunctionSymbol("sub_13B1B", 0x13B1B)]
    private void DispatchBuyOakenBed()
    {
        // IDA 0x13B1E..0x13B24: material side effect: latch the reviewed villa-condition bit 0x0200 before the
        // congratulatory purchase prompt runs.
        runtime.State.RawDataBlock.Control.StoryProgress.MarkBedPurchased();

        // IDA 0x13B24..0x13B2A: material side effect: publish bedroom bed state 0x0008 before the prompt.
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomBedRecord].State =
            StateId.Workflow.Completed;

        // IDA 0x13B2A..0x13B35: block through the fixed furniture-purchase congratulation line via the shared
        // timed transition-text seam sub_11A71.
        runtime.PromptController.RunTextAnimation(StringId.FurnitureStore_FurniturePurchaseCongratulations);
    }

    [FunctionSymbol("sub_13B51", 0x13B51)]
    private void DispatchBuyChristophColumbusArmchair()
    {
        // IDA 0x13B54..0x13B5A: material side effect: latch the reviewed villa-condition bit 0x0100 before the
        // congratulatory purchase prompt runs.
        runtime.State.RawDataBlock.Control.StoryProgress.MarkChristophColumbusArmchairPurchased();

        // IDA 0x13B5A..0x13B60: material side effect: publish house armchair state 0x0008 before the prompt.
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.HouseArmchairRecord].State =
            StateId.Workflow.Completed;

        // IDA 0x13B60..0x13B6B: block through the fixed furniture-purchase congratulation line via the shared
        // timed transition-text seam sub_11A71.
        runtime.PromptController.RunTextAnimation(StringId.FurnitureStore_FurniturePurchaseCongratulations);
    }

    [FunctionSymbol("sub_13B36", 0x13B36)]
    private void DispatchBuyRefrigerator()
    {
        // IDA 0x13B39..0x13B3F: material side effect: latch the reviewed villa-condition bit 0x0400 before the
        // congratulatory purchase prompt runs.
        runtime.State.RawDataBlock.Control.StoryProgress.MarkRefrigeratorPurchased();

        // IDA 0x13B3F..0x13B45: material side effect: publish kitchen refrigerator state 0x0008 before the prompt.
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.KitchenRefrigeratorRecord].State =
            StateId.Workflow.Completed;

        // IDA 0x13B45..0x13B50: block through the fixed furniture-purchase congratulation line via the shared
        // timed transition-text seam sub_11A71.
        runtime.PromptController.RunTextAnimation(StringId.FurnitureStore_FurniturePurchaseCongratulations);
    }
}
