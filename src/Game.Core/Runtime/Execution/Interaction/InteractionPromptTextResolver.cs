using Game.State;
using Game.Text;

namespace Game.Runtime.Execution.Interaction;

/// <summary>
///     Resolves the currently displayed interaction prompt text from the committed selection state plus the current
///     pointer hover target.
/// </summary>
/// <param name="runtime">Owning runtime state.</param>
/// <param name="selectionResolver">Shared pointer-hit selection resolver for the interaction loop.</param>
internal sealed class InteractionPromptTextResolver(Erbe runtime, InteractionSelectionResolver selectionResolver)
{
    /// <summary>
    ///     Resolves the text that should be rendered for the current interactive phase.
    /// </summary>
    /// <param name="session">Session-owned interactive selection state.</param>
    /// <param name="previewBuffer">Preview buffer used when hover text differs from the committed prompt.</param>
    /// <returns>
    ///     The committed prompt buffer when no hover preview applies; otherwise the preview buffer.
    /// </returns>
    internal ReadOnlySpan<byte> ResolveDisplayedText(InteractionSession session, byte[] previewBuffer)
    {
        return TryBuildDisplayedText(session, previewBuffer) ? previewBuffer : session.TextBuffer;
    }

    /// <summary>
    ///     Copies one primary-action prompt label into the prompt buffer.
    /// </summary>
    /// <param name="button">Verb/control button whose prompt label should be copied.</param>
    /// <param name="destinationBuffer">Destination prompt buffer.</param>
    internal void CopyPrimaryActionPromptText(InteractionButton button, byte[] destinationBuffer)
    {
        PromptController.CopyTextToBuffer(destinationBuffer,
            runtime.Strings.GetCp437String(GameStringCatalog.ReadInteractionResponseStringId((ushort)button)));
    }

    private bool TryBuildDisplayedText(InteractionSession session, byte[] previewBuffer)
    {
        if (runtime.UseClassicInteractions)
        {
            return false;
        }

        return session.SelectionPhase switch
        {
            InteractiveProgramStatePhase.PrimaryControlSelection => TryBuildDefaultPrimaryActionPreview(previewBuffer),
            InteractiveProgramStatePhase.PrimarySceneSelection => TryBuildSceneSelectionPreview(session.TextBuffer,
                previewBuffer),
            InteractiveProgramStatePhase.SecondarySceneSelection => TryBuildSceneSelectionPreview(session.TextBuffer,
                previewBuffer),
            _ => false
        };
    }

    private bool TryBuildDefaultPrimaryActionPreview(byte[] previewBuffer)
    {
        if (!selectionResolver.TryResolveCurrentStateSelection(runtime.State.RawDataBlock.Control.ProgramStateId,
                out var resolution))
        {
            return false;
        }

        CopyPrimaryActionPromptText(InteractionButton.GoTo, previewBuffer);
        PromptController.AppendTextToBuffer(previewBuffer,
            runtime.Strings.GetCp437String(resolution.Descriptor.SelectionText));
        return true;
    }

    private bool TryBuildSceneSelectionPreview(byte[] committedPromptBuffer, byte[] previewBuffer)
    {
        if (!selectionResolver.TryResolveCurrentStateOrTransitionSelection(
                runtime.State.RawDataBlock.Control.ProgramStateId,
                out var resolution,
                out _))
        {
            return false;
        }

        PromptController.CopyTextToBuffer(previewBuffer, committedPromptBuffer);
        PromptController.AppendTextToBuffer(previewBuffer,
            runtime.Strings.GetCp437String(resolution.Descriptor.SelectionText));
        return true;
    }
}
