using System.Collections.Frozen;
using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Rooms.Living;

/// <summary>
///     Owns living-room interaction handlers and their local blocking helpers.
/// </summary>
/// <param name="runtime">Runtime owner that provides shared prompt state and session data.</param>
internal sealed class LivingRoom(Erbe runtime)
{
    private const int LeftDrawerPhoneBookCollectedStateIndex = 0x05;
    private const int RightDrawerLighterCollectedStateIndex = 0x0A;
    private const ushort RightDrawerTakeTextEntryIndex = 0x000A;
    private const int CarKeyCollectedStateIndex = 0x0F;

    private static readonly FrozenDictionary<InteractionHandlerId, StringId> SimpleTextHandlers =
        new Dictionary<InteractionHandlerId, StringId>
        {
            { InteractionHandlerId.LivingRoom_InspectSwitch, StringId.LivingRoom_SwitchInspection },
            { InteractionHandlerId.LivingRoom_InspectCrack, StringId.LivingRoom_CrackInspection },
            { InteractionHandlerId.LivingRoom_UseCrack, StringId.LivingRoom_CrackUseReaction },
            { InteractionHandlerId.LivingRoom_InspectWindow, StringId.LivingRoom_WindowInspection },
            { InteractionHandlerId.LivingRoom_InspectPhoto, StringId.LivingRoom_PhotoInspection },
            { InteractionHandlerId.LivingRoom_ReadPhoto, StringId.LivingRoom_PhotoRead },
            { InteractionHandlerId.LivingRoom_SitOnArmchair, StringId.LivingRoom_ArmchairSitReaction },
            { InteractionHandlerId.LivingRoom_InspectPicture, StringId.LivingRoom_PictureInspection },
            { InteractionHandlerId.LivingRoom_InspectAquarium, StringId.LivingRoom_AquariumInspection }
        }.ToFrozenDictionary();

    private readonly LivingLeftDrawerHandler _leftDrawerHandler = new(runtime);

    internal bool TryDispatchInteraction(InteractionHandlerId handlerId)
    {
        if (SimpleTextHandlers.TryGetValue(handlerId, out var stringId))
        {
            runtime.PromptController.RunTextAnimation(stringId);
            return true;
        }

        switch (handlerId)
        {
            // handlerByteOffset +0x06 / controlMask 0x0004
            // linearAddress: 0x11BA5
            case InteractionHandlerId.LivingRoom_OpenCloseSwitch:
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.LivingRoom_UseSwitch:
                RunHouseSwitchToggleGarageVehicleState();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.LivingRoom_InspectLeftDrawer:
                DispatchInspectLeftDrawer();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.LivingRoom_InspectRightDrawer:
                DispatchInspectRightDrawer();
                return true;

            // handlerByteOffset +0x02 / controlMask 0x0020
            case InteractionHandlerId.LivingRoom_TakeLeftDrawerContents:
                _leftDrawerHandler.RunTakeFromDrawer();
                return true;

            // handlerByteOffset +0x02 / controlMask 0x0020
            case InteractionHandlerId.LivingRoom_TakeRightDrawerContents:
                DispatchTakeRightDrawerContents();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.LivingRoom_InspectArmchair:
                DispatchInspectArmchair();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.LivingRoom_InspectTable:
                DispatchInspectTable();
                return true;

            default:
                return false;
        }
    }

    [FunctionSymbol("sub_12853", 0x12853)]
    private void RunHouseSwitchToggleGarageVehicleState()
    {
        // IDA 0x12856..0x1285F: block through the fixed "strange noise" line before the garage/auto publication
        // branch runs.
        runtime.PromptController.RunTextAnimation(StringId.LivingRoom_SwitchUseNoise);

        // IDA 0x12860..0x128B3: publish the toggled garage/auto state bundle after the blocking prompt returns.
        runtime.HouseRoom.ToggleGarageVehicleStateFromInteriorSwitch();
    }

    [FunctionSymbol("sub_128FE", 0x128FE)]
    private void DispatchInspectArmchair()
    {
        // IDA 0x12901..0x12912: choose between the two fixed armchair inspection lines from the reviewed armchair
        // state word before entering the shared timed transition-text seam.
        var stringId = StringId.LivingRoom_ArmchairDesignerInspection;
        if (runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.HouseArmchairRecord].State ==
            StateId.Broken)
        {
            stringId = StringId.LivingRoom_ArmchairBrokenInspection;
        }

        // IDA 0x12912..0x12919: forward the selected prompt text through the shared timed transition-text seam
        // sub_11A71.
        runtime.PromptController.RunTextAnimation(stringId);
    }

    [FunctionSymbol("sub_128D3", 0x128D3)]
    private void DispatchInspectTable()
    {
        // IDA 0x128D6..0x128E7: choose between the two fixed house-table inspection lines from the reviewed
        // table-state word before entering the shared timed transition-text seam.
        var stringId = StringId.LivingRoom_TableWrongDeliveryInspection;
        if (runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.HouseTableRecord].State == StateId.Broken)
        {
            stringId = StringId.LivingRoom_TableDestroyedInspection;
        }

        // IDA 0x128E7..0x128EE: forward the selected prompt text through the shared timed transition-text seam
        // sub_11A71.
        runtime.PromptController.RunTextAnimation(stringId);
    }

    [FunctionSymbol("sub_127C7", 0x127C7)]
    private void DispatchInspectLeftDrawer()
    {
        // IDA 0x127CA..0x127F6: when the left drawer is open, inspect whether the phone book is still present and,
        // if not, whether the car key has already been taken; otherwise fall back to the shared default line.
        var stringId = StringId.Shared_DefaultInteractionText;
        if (runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.HouseLeftDrawerRecord].State ==
            StateId.Open)
        {
            var entryStates = runtime.State.RawDataBlock.Control.TransitionTextEntryStates;
            if (entryStates[LeftDrawerPhoneBookCollectedStateIndex] == 0)
            {
                stringId = StringId.LivingRoom_LeftDrawerPhoneBookInspection;
            }
            else if (entryStates[CarKeyCollectedStateIndex] == 0)
            {
                stringId = StringId.LivingRoom_LeftDrawerCarKeyInspection;
            }
            else
            {
                stringId = StringId.LivingRoom_RightDrawerEmptyInspection;
            }
        }

        // IDA 0x127F6..0x127FD: forward the selected prompt text through the shared timed transition-text seam
        // sub_11A71.
        runtime.PromptController.RunTextAnimation(stringId);
    }

    [FunctionSymbol("sub_1281A", 0x1281A)]
    private void DispatchInspectRightDrawer()
    {
        // IDA 0x1281D..0x1283C: when the right drawer is open, inspect whether the lighter is still present and
        // choose the corresponding drawer-specific prompt; otherwise fall back to the shared default line.
        StringId stringId;
        if (runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.HouseRightDrawerRecord].State !=
            StateId.Open)
        {
            stringId = StringId.Shared_DefaultInteractionText;
        }
        else if (runtime.State.RawDataBlock.Control.TransitionTextEntryStates[RightDrawerLighterCollectedStateIndex] ==
                 0)
        {
            stringId = StringId.LivingRoom_RightDrawerLighterInspection;
        }
        else
        {
            stringId = StringId.LivingRoom_RightDrawerEmptyInspection;
        }

        // IDA 0x1283C..0x12843: forward the selected prompt text through the shared timed transition-text seam
        // sub_11A71.
        runtime.PromptController.RunTextAnimation(stringId);
    }

    [FunctionSymbol("sub_127FE", 0x127FE)]
    private void DispatchTakeRightDrawerContents()
    {
        // IDA 0x12801..0x12818: enable transition-text entry 0x0A only when the right drawer is open and the
        // lighter has not yet been taken; otherwise fall through without mutating panel state.
        if (runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.HouseRightDrawerRecord].State ==
            StateId.Open &&
            runtime.State.RawDataBlock.Control.TransitionTextEntryStates[RightDrawerLighterCollectedStateIndex] == 0)
        {
            runtime.PromptController.SelectTransitionTextEntry(RightDrawerTakeTextEntryIndex);
        }
    }
}
