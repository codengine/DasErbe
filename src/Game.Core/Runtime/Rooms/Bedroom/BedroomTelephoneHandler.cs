using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Text;

namespace Game.Runtime.Rooms.Bedroom;

internal sealed class BedroomTelephoneHandler(Erbe runtime)
{
    private const string LolitaPhoneNumber = "690815";
    private const string HeatingCompanyPhoneNumber = "42031";
    private const string PainterPhoneNumber = "57512";
    private const string WasteServicePhoneNumber = "60700";
    private const string PlumberPhoneNumber = "66381";
    private const string SpecialNumber = "170286";

    private readonly byte[] _specialCallText = TextUtils.EncodeNullTerminated(
        "Vielen Dank, dass du diesen Port spielst und ich hoffe, dass du weiterhin Spaß daran hast :) Der Entwickler (codengine)");

    internal void RunUseTelephone()
    {
        // IDA 0x12FB4..0x12FE8: refresh the shared full-screen source surface from DISPLAY.LBM before the telephone
        // keypad prompt starts.
        runtime.FullScreenSourceSurface.Reload(AssetId.DisplayBackdrop);
        runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneDialPrompt);
        runtime.KeypadOverlay.RunCodeEntryPanel();

        var currentInput = runtime.KeypadOverlay.CurrentInput;

        switch (currentInput)
        {
            case LolitaPhoneNumber:
                RunLolitaCall();
                return;
            case HeatingCompanyPhoneNumber:
                RunHeatingCompanyCall();
                return;
            case PainterPhoneNumber:
                RunPainterCall();
                return;
            case WasteServicePhoneNumber:
                RunWasteServiceCall();
                return;
            case PlumberPhoneNumber:
                RunPlumberCall();
                return;
            case SpecialNumber:
                runtime.PromptController.RunTextAnimation(_specialCallText);
                return;
            default:
                runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneNoConnection);
                return;
        }
    }

    private void RunLolitaCall()
    {
        // IDA 0x13008..0x13014: block through the fixed Lolita greeting line before publishing the contact bit.
        runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneLolitaGreeting);

        // IDA 0x13015..0x1301B: material side effect: publish the Lolita contact milestone in the shared Lolita-heart
        // progress tracker.
        runtime.State.RawDataBlock.Control.LolitaProgress.MarkLolitaContacted();
    }

    private void RunHeatingCompanyCall()
    {
        if (runtime.PromptController.RunTextAnimationWithConfirmationPrompt(StringId
                .Bedroom_TelephoneHeatingInterestPrompt))
        {
            if (runtime.PromptController.RunTextAnimationWithConfirmationPrompt(StringId
                    .Bedroom_TelephoneHeatingStandardOfferPrompt))
            {
                // IDA 0x13054..0x13063: queue the standard heating follow-up through the shared variant-A selector.
                runtime.PromptController.QueueOzoneAlternateSceneTransition(
                    StringId.Bedroom_TelephoneHeatingStandardQueued,
                    5);
            }
            else if (runtime.PromptController.RunTextAnimationWithConfirmationPrompt(StringId
                         .Bedroom_TelephoneHeatingEcoOfferPrompt))
            {
                // IDA 0x13074..0x13086: material side effects: latch the heating-resolution villa-condition bit,
                // publish the heating installation state slot to 0x0009, and advance the heating-system state to
                // 0x0008 before the acceptance line.
                runtime.State.RawDataBlock.Control.StoryProgress.MarkHeatingResolved();
                runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.NoItemPlaceholderRecordC].State =
                    StateId.Workflow.Scheduled;
                runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BasementHeatingSystemRecord].State =
                    StateId.Workflow.Completed;
                runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneHeatingAccepted);
            }
        }

        if (!runtime.PromptController.RunTextAnimationWithConfirmationPrompt(StringId
                .Bedroom_TelephoneInsulationInterestPrompt))
        {
            runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneWrongDepartmentResponse);
            return;
        }

        if (runtime.PromptController.RunTextAnimationWithConfirmationPrompt(StringId
                .Bedroom_TelephoneInsulationMineralFiberPrompt))
        {
            // IDA 0x130AE..0x130B4: material side effects: publish both reviewed insulation state words to 0x0005.
            runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.HouseCrackRecord].State =
                StateId.Bedroom.PhoneServiceScheduled;
            runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BlankPromptPlaceholderRecord].State =
                StateId.Bedroom.PhoneServiceScheduled;
            runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneInsulationMineralFiberAccepted);
        }
        else
        {
            runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneInsulationFoamOffer);
            runtime.PromptController.QueueOzoneSceneTransition(StringId.Bedroom_TelephoneInsulationFoamQueued, 1);
        }

        // IDA 0x130DF..0x130E5: material side effect: latch the insulation-resolution villa-condition bit before the
        // shared goodbye line.
        runtime.State.RawDataBlock.Control.StoryProgress.MarkInsulationResolved();
        runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneGoodbye);
    }

    private void RunPainterCall()
    {
        if (runtime.PromptController.RunTextAnimationWithConfirmationPrompt(StringId.Bedroom_TelephonePainterPrompt))
        {
            // IDA 0x13117..0x13130: material side effect: latch the painter villa-condition bit before the shared
            // goodbye line and queued alternate follow-up.
            runtime.State.RawDataBlock.Control.StoryProgress.MarkPainterResolved();
            runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneGoodbye);
            runtime.PromptController.QueueOzoneSceneTransition(StringId.Bedroom_TelephonePainterQueued, 2);
            return;
        }

        if (runtime.PromptController.RunTextAnimationWithConfirmationPrompt(StringId.Bedroom_TelephonePainterEcoPrompt))
        {
            // IDA 0x13148..0x1314E: material side effects: latch the painter villa-condition bit and publish the
            // reviewed house-painting state word to 0x0005 before the acceptance line.
            runtime.State.RawDataBlock.Control.StoryProgress.MarkPainterResolved();
            runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.NoItemPlaceholderRecordA].State =
                StateId.Bedroom.PhoneServiceScheduled;
            runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephonePainterAccepted);
            runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneGoodbye);
            return;
        }

        runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephonePainterDismissal);
    }

    private void RunWasteServiceCall()
    {
        if (runtime.PromptController.RunTextAnimationWithConfirmationPrompt(
                StringId.Bedroom_TelephoneWasteServicePrompt))
        {
            runtime.PromptController.QueueOzoneSceneTransition(StringId.Bedroom_TelephoneWasteServiceQueued, 5);
            runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneGoodbye);
            return;
        }

        if (runtime.PromptController.RunTextAnimationWithConfirmationPrompt(StringId
                .Bedroom_TelephoneWasteServiceHazardousPrompt))
        {
            runtime.State.RawDataBlock.Control.StoryProgress.MarkWasteServiceResolved();
            runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneWasteServiceHazardousAccepted);
            runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneGoodbye);
            return;
        }

        runtime.PromptController.QueueOzoneSceneTransition(StringId.Bedroom_TelephoneWasteServiceQueued, 5);
        runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephoneGoodbye);
    }

    private void RunPlumberCall()
    {
        // IDA 0x131BA..0x131E2: run the fixed acceptance line before the plumber route publishes its state writes.
        runtime.PromptController.RunTextAnimation(StringId.Bedroom_TelephonePlumberAccepted);

        // IDA 0x131E3..0x131EF: material side effects: latch the plumber villa-condition bit, publish the kitchen
        // plumber state to 0x0009, and advance the kitchen faucet state to 0x0008.
        runtime.State.RawDataBlock.Control.StoryProgress.MarkPlumberResolved();
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.NoItemPlaceholderRecordB].State =
            StateId.Workflow.Scheduled;
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.KitchenFaucetRecord].State =
            StateId.Workflow.Completed;
    }
}
