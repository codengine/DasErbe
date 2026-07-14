using Game.Catalogs;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Rooms.City2;

internal sealed class City2MailboxHandler(Erbe runtime)
{
    private const ushort MailSlotSelectionEntryIndex = 0x0009;
    private const ushort LetterSentEntryIndex = 0x0056;
    private const byte PreparedLetterTransitionTextEntryIndex = 0x02;

    [FunctionSymbol("sub_13538", 0x13538)]
    internal void RunLetterUseOnMailSlot()
    {
        var mailSlotSelectionEntry = runtime.State.RawDataBlock.SelectionTable[MailSlotSelectionEntryIndex];

        // IDA 0x1353B..0x13546: the City2 mail-slot callback first requires the row-local mail-slot selection
        // record to be open; a zero state mask falls through to the fixed "open the slot first" prompt.
        if (mailSlotSelectionEntry.State == StateId.Default)
        {
            runtime.PromptController.RunTextAnimation(StringId.City2_LetterMailSlotOpenFirst);
            return;
        }

        var entryStates = runtime.State.RawDataBlock.Control.TransitionTextEntryStates;

        // IDA 0x13548..0x1356F: only the prepared-letter state value 0x02 enters the delivery branch. The helper
        // clears transition-text entry 0x0002 and republishes start index 0 before the prompt, then publishes the
        // villa-condition bit and posted-letter persistent state only after the prompt returns.
        if (entryStates[PreparedLetterTransitionTextEntryIndex] == 2)
        {
            entryStates[PreparedLetterTransitionTextEntryIndex] = 0;
            runtime.State.RawDataBlock.Control.TransitionTextStartIndex = 0;
            runtime.PromptController.RunTextAnimation(StringId.City2_LetterDelivery);
            runtime.State.RawDataBlock.Control.StoryProgress.MarkLetterPosted();
            runtime.State.RawDataBlock.SelectionTable[LetterSentEntryIndex].State = StateId.City2.LetterSent;
            return;
        }

        // IDA 0x13570..0x1357B: an open mail slot without the prepared-letter state falls through to the fixed
        // "stamped letters only" prompt with no further side effects.
        runtime.PromptController.RunTextAnimation(StringId.City2_LetterStampRequired);
    }
}
