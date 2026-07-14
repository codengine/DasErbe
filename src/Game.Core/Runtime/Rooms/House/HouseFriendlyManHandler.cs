using Game.Text;

namespace Game.Runtime.Rooms.House;

internal sealed class HouseFriendlyManHandler(Erbe runtime)
{
    internal void RunInspectFriendlyMan()
    {
        // IDA 0x13D07..0x13D18: when every required story-progress bit except bit 0 is set, publish the certificate
        // line. Otherwise fall back to the unmet-conditions line below.
        if (!runtime.State.RawDataBlock.Control.StoryProgress.MeetsFriendlyManCertificateRequirements())
        {
            // IDA 0x13D39..0x13D42: requirements still missing, so publish the fixed refusal line instead.
            runtime.PromptController.RunTextAnimation(StringId.House_FriendlyManCertificateConditionsMissing);
            return;
        }

        runtime.PromptController.RunTextAnimation(StringId.House_FriendlyManCertificate);

        // IDA 0x13D1D..0x13D31: re-read StoryProgressFlags after the blocking certificate prompt. If the
        // flight-decision bit is still clear, run the shared confirmation helper. A pre-set bit requests the shared
        // interactive-flow advance immediately.
        if (runtime.State.RawDataBlock.Control.StoryProgress.HasFriendlyManFlightDecision())
        {
            runtime.State.RawDataBlock.Control.AdvanceRequestedFlag = 1;
            return;
        }

        // IDA 0x13D27..0x13D31: the focus symbol skips the advance-request side effect on the prompt's primary
        // option and only publishes the flag after the secondary option.
        if (!runtime.PromptController.RunTextAnimationWithConfirmationPrompt(StringId.House_FriendlyManFlight))
        {
            runtime.State.RawDataBlock.Control.AdvanceRequestedFlag = 1;
        }
    }
}
