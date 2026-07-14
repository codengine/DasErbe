using System.Collections.Frozen;
using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Runtime.Execution;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Rooms.Kitchen;

/// <summary>
///     Owns kitchen interaction handlers while shared text-animation dispatch remains in
///     <see cref="PromptController" />.
/// </summary>
/// <param name="runtime">Runtime owner that provides shared prompt state and session data.</param>
internal sealed class KitchenRoom(Erbe runtime)
{
    private const ushort WhiskTakeTextEntryIndex = 0x0006;
    private const ushort OvenTakeTextEntryIndex = 0x0004;

    private static readonly FrozenDictionary<InteractionHandlerId, StringId> SimpleTextHandlers =
        new Dictionary<InteractionHandlerId, StringId>
        {
            { InteractionHandlerId.Kitchen_InspectWhisk, StringId.Kitchen_WhiskInspection },
            { InteractionHandlerId.Kitchen_InspectPlate, StringId.Kitchen_PlateInspection },
            { InteractionHandlerId.Kitchen_TakePlate, StringId.Kitchen_PlateTake },
            { InteractionHandlerId.Kitchen_UseFaucet, StringId.Kitchen_FaucetUse },
            { InteractionHandlerId.Kitchen_UseOven, StringId.Kitchen_OvenUse },
            { InteractionHandlerId.Kitchen_OpenRefrigerator, StringId.Kitchen_RefrigeratorOpenInspection },
            { InteractionHandlerId.Kitchen_InspectSink, StringId.Kitchen_SinkInspection },
            { InteractionHandlerId.Kitchen_InspectDishwasher, StringId.Kitchen_DishwasherInspection }
        }.ToFrozenDictionary();

    /// <summary>
    ///     Dispatches one kitchen-owned handler directly to completion.
    /// </summary>
    /// <param name="handlerId">Resolved handler identifier from the interaction descriptor.</param>
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
            case InteractionHandlerId.Kitchen_TakeWhisk:
                DispatchTakeWhisk();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.Kitchen_InspectFaucet:
                DispatchInspectFaucet();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.Kitchen_InspectOven:
                DispatchInspectOven();
                return true;

            // handlerByteOffset +0x02 / controlMask 0x0020
            case InteractionHandlerId.Kitchen_TakeBowlFromOven:
                DispatchTakeBowlFromOven();
                return true;

            // handlerByteOffset +0x06 / controlMask 0x0004
            case InteractionHandlerId.Kitchen_ToggleOvenOpenState:
                DispatchToggleOvenOpenState();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.Kitchen_InspectRefrigerator:
                DispatchInspectRefrigerator();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.Kitchen_WashPlate:
                DispatchWashPlate();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.Kitchen_UseDishwasher:
                DispatchUseDishwasher();
                return true;

            default:
                return false;
        }
    }

    [FunctionSymbol("sub_12C25", 0x12C25)]
    private void DispatchInspectFaucet()
    {
        // IDA 0x12C28..0x12C39: choose between the dripping and fixed faucet inspection lines from the kitchen faucet
        // state word before entering the shared timed transition-text seam.
        var faucetState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.KitchenFaucetRecord];
        var stringId = faucetState.State == StateId.Broken
            ? StringId.Kitchen_FaucetDrippingInspection
            : StringId.Kitchen_FaucetFixedInspection;

        // IDA 0x12C39..0x12C40: forward the selected prompt text through the shared timed transition-text seam
        // sub_11A71, which remains owned by PromptController.
        runtime.PromptController.RunTextAnimation(stringId);
    }

    [FunctionSymbol("sub_12C50", 0x12C50)]
    private void DispatchInspectOven()
    {
        // IDA 0x12C53..0x12C72: select the oven inspection prompt from the reviewed kitchen oven state. State 0x0002
        // reports that something is inside, state 0x0082 reveals the bowl, and all other states fall back to the
        // shared default line.
        var ovenState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.KitchenOvenRecord];
        var stringId = ovenState.State switch
        {
            StateId.Container.Filled => StringId.Kitchen_OvenContainsSomethingInspection,
            StateId.Container.Filled | StateId.Open => StringId.Kitchen_OvenBowlInspection,
            _ => StringId.Shared_DefaultInteractionText
        };

        // IDA 0x12C72..0x12C79: forward the selected prompt text through the shared timed transition-text seam
        // sub_11A71, which remains owned by PromptController.
        runtime.PromptController.RunTextAnimation(stringId);
    }

    [FunctionSymbol("sub_12BFA", 0x12BFA)]
    private void DispatchTakeWhisk()
    {
        // IDA 0x12BFD..0x12C06: enable transition-text entry 0x06 and publish that same entry as the current
        // transition-text panel start index through sub_14270, which remains owned by PromptController.
        runtime.PromptController.SelectTransitionTextEntry(WhiskTakeTextEntryIndex);
    }

    [FunctionSymbol("sub_12C7A", 0x12C7A)]
    private void DispatchTakeBowlFromOven()
    {
        // IDA 0x12C7D..0x12C94: only when the reviewed bowl-visible oven state is active, clear it back to the
        // open-empty state and publish transition-text entry 0x04 through sub_14270. All other states are a no-op.
        var ovenState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.KitchenOvenRecord];
        if (ovenState.State != (StateId.Container.Filled | StateId.Open))
        {
            return;
        }

        // IDA 0x12C85..0x12C8A: material side effect: mark the bowl as no longer visible while keeping the oven open.
        ovenState.State = StateId.Container.Empty | StateId.Open;

        // IDA 0x12C8B..0x12C94: enable transition-text entry 0x04 and publish that same entry as the current
        // transition-text panel start index through sub_14270, which remains owned by PromptController.
        runtime.PromptController.SelectTransitionTextEntry(OvenTakeTextEntryIndex);
    }

    [FunctionSymbol("sub_12CFA", 0x12CFA)]
    private void DispatchInspectRefrigerator()
    {
        // IDA 0x12CFD..0x12D1C: choose between the broken, empty, and intact refrigerator inspection lines from the
        // reviewed kitchen refrigerator state word before entering the shared timed transition-text seam.
        var refrigeratorState =
            runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.KitchenRefrigeratorRecord];
        var stringId = refrigeratorState.State switch
        {
            StateId.Broken => StringId.Kitchen_RefrigeratorBrokenInspection,
            StateId.Kitchen.RefrigeratorEmpty => StringId.Kitchen_RefrigeratorEmptyInspection,
            _ => StringId.Kitchen_RefrigeratorWorkingInspection
        };

        // IDA 0x12D1C..0x12D23: forward the selected prompt text through the shared timed transition-text seam
        // sub_11A71, which remains owned by PromptController.
        runtime.PromptController.RunTextAnimation(stringId);
    }

    [FunctionSymbol("sub_12D42", 0x12D42)]
    private void DispatchWashPlate()
    {
        // IDA 0x12D45..0x12D4A: material side effect: advance the reviewed kitchen plate state to the cleaned value
        // before the prompt is shown.
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.KitchenDishesRecord].State =
            StateId.Kitchen.DishesClean;

        // IDA 0x12D4B..0x12D56: forward the fixed clean-plates line through the shared timed transition-text seam
        // sub_11A71, which remains owned by PromptController.
        runtime.PromptController.RunTextAnimation(StringId.Kitchen_PlateCleaned);
    }

    [FunctionSymbol("sub_12CA4", 0x12CA4)]
    private void DispatchToggleOvenOpenState()
    {
        // IDA 0x12CA9..0x12CE5: toggle the reviewed oven open-state table mapping over { 0x0001, 0x0002, 0x0081,
        // 0x0082 }. Material side effect: write OvenState only for matched states; unknown states are a no-op.
        var ovenState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.KitchenOvenRecord];
        ovenState.State = ovenState.State switch
        {
            StateId.Container.Empty => StateId.Container.Empty | StateId.Open,
            StateId.Container.Filled => StateId.Container.Filled | StateId.Open,
            StateId.Container.Empty | StateId.Open => StateId.Container.Empty,
            StateId.Container.Filled | StateId.Open => StateId.Container.Filled,
            _ => ovenState.State
        };
    }

    [FunctionSymbol("sub_12D66", 0x12D66)]
    private void DispatchUseDishwasher()
    {
        // IDA 0x12D69..0x12D78: queue the fixed dishwasher-use warning through the shared scene-variant-A selector
        // with selection-count increment 0x0002.
        runtime.PromptController.QueueOzoneAlternateSceneTransition(StringId.Kitchen_DishwasherUseWarning, 2);
    }
}
