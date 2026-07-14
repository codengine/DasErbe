using System.Collections.Frozen;
using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Rooms.City2;

internal sealed class City2Room(Erbe runtime)
{
    private static readonly FrozenDictionary<InteractionHandlerId, StringId> SimpleTextHandlers =
        new Dictionary<InteractionHandlerId, StringId>
        {
            { InteractionHandlerId.City2_InspectMailbox, StringId.City2_MailboxInspection },
            { InteractionHandlerId.City2_InspectTrash, StringId.City2_TrashInspection },
            { InteractionHandlerId.City2_InspectSign, StringId.City2_SignInspection },
            { InteractionHandlerId.City2_ReadSign, StringId.City2_SignRead },
            { InteractionHandlerId.City2_InspectCigarette, StringId.City2_CigaretteInspection }
        }.ToFrozenDictionary();

    private readonly City2MailboxHandler _mailboxHandler = new(runtime);

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
            case InteractionHandlerId.City2_UseLetterOnMailSlot:
                DispatchUseLetterOnMailSlot();
                return true;

            case InteractionHandlerId.City2_VisitIntBurgerConnect:
                DispatchVisitIntBurgerConnect();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.City2_InspectIntBurgerConnectDoor:
                DispatchInspectIntBurgerConnectDoor();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.City2_InspectBillysBurgerBarDoor:
                DispatchInspectBillysBurgerBarDoor();
                return true;

            case InteractionHandlerId.City2_VisitBillysBurgerBar:
                RunVisitBillysBurgerBar();
                return true;

            default:
                return false;
        }
    }

    private void DispatchUseLetterOnMailSlot()
    {
        _mailboxHandler.RunLetterUseOnMailSlot();
    }

    [FunctionSymbol("sub_1358C", 0x1358C)]
    private void DispatchInspectIntBurgerConnectDoor()
    {
        // IDA 0x1358F..0x13595: only the prompt-enabled City2 fast-food door state (0x0080) enters the patron
        // observation path; all other states return immediately without side effects.
        var doorState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.IntBurgerConnectDoorRecord];
        if (doorState.State != StateId.Open)
        {
            return;
        }

        // IDA 0x13597..0x135A0: forward the fixed Int Burger Connect patron observation line through PromptController's
        // shared timed transition-text seam sub_11A71.
        runtime.PromptController.RunTextAnimation(StringId.City2_IntBurgerConnectPatronObservation);
    }

    [FunctionSymbol("sub_135A3", 0x135A3)]
    private void DispatchVisitIntBurgerConnect()
    {
        // IDA 0x135A6..0x135C3: only the prompt-enabled city2 fast-food door state (0x0080) enters the queued
        // packaging-waste text path; all other states return immediately without side effects.
        var doorState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.IntBurgerConnectDoorRecord];
        if (doorState.State != StateId.Open)
        {
            return;
        }

        // IDA 0x135AE..0x135BA: queue the fixed packaging-waste prompt through PromptController's shared
        // queueProgramStateTransitionTextOnly seam sub_1414A with selection-count increment 0x0001.
        runtime.PromptController.QueueTextAnimationWithErrors(StringId.City2_IntBurgerConnectPackagingWaste, 1);

        // IDA 0x135BD..0x135BD: material side effect: publish backdrop selection row 3 for the queued follow-up
        // presentation.
        runtime.State.RawDataBlock.Control.BackdropSelectionRow = 3;
    }

    [FunctionSymbol("sub_135C5", 0x135C5)]
    private void DispatchInspectBillysBurgerBarDoor()
    {
        // IDA 0x135C8..0x135DA: only the prompt-enabled city2 door state (0x0080) enters PromptController's shared timed
        // transition-text seam; all other states return immediately without side effects.
        var doorState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BillysBurgerBarDoorRecord];
        if (doorState.State != StateId.Open)
        {
            return;
        }

        // IDA 0x135D0..0x135D9: forward the fixed city2 door inspection line through the shared timed
        // transition-text seam sub_11A71.
        runtime.PromptController.RunTextAnimation(StringId.City2_DoorInspection);
    }

    [FunctionSymbol("sub_135DC", 0x135DC)]
    private void RunVisitBillysBurgerBar()
    {
        // IDA 0x135DF..0x135F7: only the prompt-enabled city2 door state (0x0080) enters the burger-bar visit path;
        // all other states return immediately without side effects.
        var doorState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BillysBurgerBarDoorRecord];
        if (doorState.State != StateId.Open)
        {
            return;
        }

        // IDA 0x135E7..0x135F0: block through the fixed Billy's Burger Bar visit line via the shared timed
        // transition-text seam sub_11A71.
        runtime.PromptController.RunTextAnimation(StringId.City2_BillysBurgerBarVisit);

        // IDA 0x135F1..0x135F1: material side effect: publish backdrop selection row 3 after the visit prompt
        // returns.
        runtime.State.RawDataBlock.Control.BackdropSelectionRow = 3;
    }
}
