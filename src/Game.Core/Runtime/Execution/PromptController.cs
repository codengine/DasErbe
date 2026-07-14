using Game.Catalogs;
using Game.Display;
using Game.Input;
using Game.Shared.RE;
using Game.State;
using Game.Text;

namespace Game.Runtime.Execution;

internal sealed class PromptController(Erbe runtime)
{
    private const int TransitionTextEntryCount = 0x10;
    private const int VisibleLineCount = 0x06;
    private const ushort SelectedInteractionIndexBase = 0x000E;
    private const ushort SelectionResponseTextIndexBase = 0x0080;
    private const ushort PanelLeftColumn = 0x00D9;
    private const ushort PanelTopRow = 0x008F;
    private const ushort PanelRightColumn = 0x0130;
    private const ushort PanelBottomRow = 0x00C4;
    private const ushort TextStartColumn = 0x00DA;
    private const ushort TextStartRow = 0x0092;
    private const byte TextInkColorIndex = 0xFF;
    private const ushort VariantAFrameWidthPixels = 0x0050;
    private const ushort VariantAFrameHeightRows = 0x002B;
    private const ushort VariantAFrameSourceHeightRows = 0x002C;
    private const ushort VariantAAnimationDestinationColumn = 0x0078;
    private const ushort VariantAAnimationDestinationRow = 0x0052;
    private const ushort VariantAAnimationGroupCount = 0x0008;
    private const ushort VariantAAnimationRepeatCount = 0x000A;
    private const ushort OzoneAlternateResetThreshold = 0x0005;
    private const ushort AlternateQueuedSceneVariantAnimationGroupCount = 0x0005;
    private const ushort AlternateQueuedSceneVariantAnimationRepeatCount = 0x000A;
    private const ushort AlternateQueuedSceneVariantAnimationGroupsPerRow = 0x0002;
    private const ushort AlternateQueuedSceneVariantFrameWidthPixels = 0x00A0;
    private const ushort AlternateQueuedSceneVariantFrameHeightRows = 0x002C;
    private const ushort AlternateQueuedSceneVariantFrameSourceRowStride = 0x002D;
    private const ushort AlternateQueuedSceneVariantDestinationColumn = 0x0060;
    private const ushort AlternateQueuedSceneVariantDestinationRow = 0x003F;
    private const ushort InteractivePromptTextRow = 0x0083;
    private const ushort GlobalControlFirstRegionIndex = 0x001E;
    private const ushort GlobalControlRegionCount = 0x0017;
    private const int ConfirmationFirstTerminalSelectionLocalIndex = 0x000B;
    private const int ConfirmationSecondTerminalSelectionLocalIndex = 0x000C;

    /// <summary>
    ///     Copies one null-terminated text byte stream into a target buffer.
    /// </summary>
    /// <param name="destinationBuffer">Destination byte buffer that receives the copied text.</param>
    /// <param name="sourceText">Null-terminated source text bytes.</param>
    [FunctionSymbol("sub_14AE8", 0x14AE8)]
    internal static void CopyTextToBuffer(byte[] destinationBuffer, ReadOnlySpan<byte> sourceText)
    {
        // IDA 0x14AEB..0x14B04: copy the source bytes into the caller-owned buffer until the terminating zero byte
        // is written too.
        var destinationIndex = 0;
        foreach (var textByte in sourceText)
        {
            if ((uint)destinationIndex >= destinationBuffer.Length)
            {
                throw new InvalidOperationException(
                    "sub_14AE8 destination buffer overflowed before the text terminator.");
            }

            destinationBuffer[destinationIndex++] = textByte;
            if (textByte == 0)
            {
                return;
            }
        }

        throw new InvalidOperationException("sub_14AE8 requires a null-terminated source text byte stream.");
    }

    /// <summary>
    ///     Appends one null-terminated text byte stream to a target buffer.
    /// </summary>
    /// <param name="destinationBuffer">Destination byte buffer that already contains a null-terminated string.</param>
    /// <param name="sourceText">Null-terminated source text bytes to append.</param>
    [FunctionSymbol("sub_14B05", 0x14B05)]
    internal static void AppendTextToBuffer(byte[] destinationBuffer, ReadOnlySpan<byte> sourceText)
    {
        // IDA 0x14B08..0x14B2D: walk to the existing terminator and append the new null-terminated byte stream in
        // place.
        var destinationIndex = 0;
        while (destinationIndex < destinationBuffer.Length && destinationBuffer[destinationIndex] != 0)
        {
            destinationIndex++;
        }

        if (destinationIndex >= destinationBuffer.Length)
        {
            throw new InvalidOperationException("sub_14B05 destination buffer is missing a terminating zero byte.");
        }

        foreach (var textByte in sourceText)
        {
            if ((uint)destinationIndex >= destinationBuffer.Length)
            {
                throw new InvalidOperationException(
                    "sub_14B05 destination buffer overflowed before the appended terminator.");
            }

            destinationBuffer[destinationIndex++] = textByte;
            if (textByte == 0)
            {
                return;
            }
        }

        throw new InvalidOperationException("sub_14B05 requires a null-terminated source text byte stream.");
    }

    /// <summary>
    ///     Runs the fixed default interaction text animation.
    /// </summary>
    [FunctionSymbol("sub_11BAF", 0x11BAF)]
    internal void RunDefaultInteractionTextAnimation()
    {
        RunTextAnimation(StringId.Shared_DefaultInteractionText);
    }

    /// <summary>
    ///     Runs the currently queued text animation.
    /// </summary>
    [FunctionSymbol("sub_13E40", 0x13E40)]
    internal void ShowQueuedTextAnimation()
    {
        var program = runtime.State.Program;
        RunTextAnimation(program.QueuedTransitionText);
        program.ClearQueuedTextAnimation();
    }

    /// <summary>
    ///     Runs the shared text animation effect to completion.
    /// </summary>
    /// <param name="stringId">Semantic reference for the animated text stream.</param>
    [FunctionSymbol("sub_11A71", 0x11A71)]
    internal void RunTextAnimation(StringId stringId)
    {
        RunTextAnimation(runtime.TextRenderer.MeasureStringWidthPixels(stringId),
            () => runtime.TransitionEffect.QueueStateTransitionEffectText(stringId));
    }

    /// <summary>
    ///     Runs the shared text animation effect to completion for one dynamic text stream.
    /// </summary>
    /// <param name="textBytes">Null-terminated CP437 text bytes.</param>
    internal void RunTextAnimation(ReadOnlySpan<byte> textBytes)
    {
        var copiedText = textBytes.ToArray();
        RunTextAnimation(runtime.TextRenderer.MeasureTextWidthPixels(copiedText),
            () => runtime.TransitionEffect.QueueStateTransitionEffectText(copiedText));
    }

    private void RunTextAnimation(ushort measuredWidthPixels, Action queueAnimation)
    {
        var remainingTimedTransitionFrames = measuredWidthPixels + 0x012C;
        queueAnimation();

        while (true)
        {
            // IDA 0x11A92..0x11AA7: advance the transition-effect band, expose the frame through the pointer overlay,
            // poll for early input, decrement the measured frame budget, then either keep scrolling or leave the loop.
            runtime.TransitionEffect.AdvanceTransitionEffect();
            runtime.PointerOverlay.AdvancePointerOverlayFrame();

            var continueLoop = runtime.InputAdapter.PollInputEvent() == RuntimeInputEvent.None &&
                               remainingTimedTransitionFrames > 0;
            if (remainingTimedTransitionFrames > 0)
            {
                remainingTimedTransitionFrames--;
            }

            if (!continueLoop)
            {
                break;
            }
        }

        // IDA 0x11AA9..0x11AB8: after the final scrolling frame, emit the two cleanup frames on successive host frames.
        runtime.TransitionEffect.ResetTransitionEffect();
        runtime.TransitionEffect.AdvanceTransitionEffect();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        runtime.TransitionEffect.AdvanceTransitionEffect();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }

    /// <summary>
    ///     Runs the shared timed-text confirmation prompt and returns whether the primary option was selected.
    /// </summary>
    /// <param name="stringId">Semantic reference for the leading timed prompt.</param>
    /// <returns><see langword="true" /> when the primary terminal option was selected; otherwise <see langword="false" />.</returns>
    [FunctionSymbol("sub_11ABC", 0x11ABC)]
    internal bool RunTextAnimationWithConfirmationPrompt(StringId stringId)
    {
        RunTextAnimation(stringId);

        while (true)
        {
            // IDA 0x11AD2..0x11B20: redraw the fixed yes/no prompt until the primary confirm action selects one of the
            // terminal indices.
            var confirmationPromptWidthPixels =
                runtime.TextRenderer.MeasureStringWidthPixels(StringId.Shared_ConfirmationPrompt);
            runtime.TextCursor.SeedTextBlockCursor(
                (ushort)((RuntimeState.FrameWidth - confirmationPromptWidthPixels) >> 1),
                InteractivePromptTextRow);
            runtime.TextRenderer.RenderStringBlock(StringId.Shared_ConfirmationPrompt);
            runtime.PointerOverlay.AdvancePointerOverlayFrame();

            var selectedLocalIndex = -1;
            var inputEvent = runtime.InputAdapter.PollInputEvent();
            if (inputEvent.IsPrimaryConfirmAction)
            {
                selectedLocalIndex = runtime.InteractiveRegions.FindFirstSelectablePanelRegionAtPointer(
                    GlobalControlFirstRegionIndex,
                    GlobalControlRegionCount);
            }

            if (selectedLocalIndex != ConfirmationFirstTerminalSelectionLocalIndex &&
                selectedLocalIndex != ConfirmationSecondTerminalSelectionLocalIndex)
            {
                continue;
            }

            // IDA 0x11B22..0x11B39: emit the two confirmation cleanup frames, then map the terminal selection back to
            // the original AX return shape: local index 0x0B returns nonzero and local index 0x0C returns zero.
            runtime.TransitionEffect.AdvanceTransitionEffect();
            runtime.PointerOverlay.AdvancePointerOverlayFrame();

            runtime.TransitionEffect.AdvanceTransitionEffect();
            runtime.PointerOverlay.AdvancePointerOverlayFrame();
            return selectedLocalIndex == ConfirmationFirstTerminalSelectionLocalIndex;
        }
    }

    /// <summary>
    ///     Queues the alternate ozone-scene warning branch and its follow-up text.
    /// </summary>
    /// <param name="stringId">Semantic reference for the queued warning text stream.</param>
    /// <param name="errorCount">Environmental-error count increment applied by this queued branch.</param>
    [FunctionSymbol("sub_1416D", 0x1416D)]
    internal void QueueOzoneAlternateSceneTransition(StringId stringId, ushort errorCount)
    {
        var program = runtime.State.Program;
        program.QueueTextAnimation(stringId);
        runtime.State.RawDataBlock.Control.ErrorCount += errorCount;
        program.OzoneAlternateTransitionQueuedFlag = 1;
        program.OzoneTransitionQueuedFlag = 0;
    }

    /// <summary>
    ///     Queues one text-only post-action branch and clears both queued ozone-scene selectors.
    /// </summary>
    /// <param name="stringId">Semantic reference for the queued text stream.</param>
    /// <param name="errorCount">Environmental-error count increment applied by this queued branch.</param>
    [FunctionSymbol("sub_1414A", 0x1414A)]
    internal void QueueTextAnimationWithErrors(StringId stringId, ushort errorCount)
    {
        var program = runtime.State.Program;
        program.QueueTextAnimation(stringId);
        runtime.State.RawDataBlock.Control.ErrorCount += errorCount;
        program.OzoneAlternateTransitionQueuedFlag = 0;
        program.OzoneTransitionQueuedFlag = 0;
    }

    /// <summary>
    ///     Queues the ozone-scene warning branch and its follow-up text.
    /// </summary>
    /// <param name="stringId">Semantic reference for the queued warning text stream.</param>
    /// <param name="errorCount">Environmental-error count increment applied by this queued branch.</param>
    [FunctionSymbol("sub_14190", 0x14190)]
    internal void QueueOzoneSceneTransition(StringId stringId, ushort errorCount)
    {
        var program = runtime.State.Program;
        var control = runtime.State.RawDataBlock.Control;
        program.QueueTextAnimation(stringId);
        control.ErrorCount += errorCount;
        program.OzoneAlternateTransitionQueuedFlag = 0;
        program.OzoneTransitionQueuedFlag = 1;
    }

    /// <summary>
    ///     Runs the queued alternate ozone-scene animation and prompt dispatch to completion.
    /// </summary>
    [FunctionSymbol("sub_13E5E", 0x13E5E)]
    internal void ShowOzoneAlternateScene()
    {
        ushort animationGroupIndex = 0;
        ushort animationRepeatIndex = 0;

        runtime.FullScreenSourceSurface.Reload(AssetId.OzoneScene);
        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            CreateVariantAFullScreenRegion());
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            CreateVariantAFullScreenRegion());
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        runtime.FullScreenSourceSurface.Reload(AssetId.OzoneAnimation);

        while (true)
        {
            var animationRegion = CreateVariantAAnimationRegion(animationGroupIndex);
            runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
                runtime.FullScreenSourceSurface.Buffer,
                animationRegion);
            runtime.PointerOverlay.AdvancePointerOverlayFrame();

            animationRepeatIndex++;
            if (animationRepeatIndex >= VariantAAnimationRepeatCount)
            {
                animationRepeatIndex = 0;
                animationGroupIndex++;
            }

            var lastFrame = animationGroupIndex >= VariantAAnimationGroupCount;
            if (lastFrame)
            {
                break;
            }
        }

        ShowQueuedTextAnimation();
        if (runtime.State.RawDataBlock.Control.ErrorCount >= OzoneAlternateResetThreshold)
        {
            return;
        }

        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }

    /// <summary>
    ///     Runs the queued ozone-scene animation and prompt dispatch to completion.
    /// </summary>
    [FunctionSymbol("sub_13FD4", 0x13FD4)]
    internal void ShowOzoneScene()
    {
        ushort animationGroupIndex = 0;
        ushort animationRepeatIndex = 0;

        runtime.FullScreenSourceSurface.Reload(AssetId.OzoneScene);
        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            CreateAlternateQueuedSceneFullScreenRegion());
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            CreateAlternateQueuedSceneFullScreenRegion());
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        runtime.FullScreenSourceSurface.Reload(AssetId.OzoneIntro);

        while (true)
        {
            var animationRegion = CreateAlternateQueuedSceneAnimationRegion(animationGroupIndex);
            runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
                runtime.FullScreenSourceSurface.Buffer,
                animationRegion);
            runtime.PointerOverlay.AdvancePointerOverlayFrame();

            animationRepeatIndex++;
            if (animationRepeatIndex >= AlternateQueuedSceneVariantAnimationRepeatCount)
            {
                animationRepeatIndex = 0;
                animationGroupIndex++;
            }

            var lastFrame = animationGroupIndex >= AlternateQueuedSceneVariantAnimationGroupCount;
            if (lastFrame)
            {
                break;
            }
        }

        ShowQueuedTextAnimation();
        if (runtime.State.RawDataBlock.Control.ErrorCount >= OzoneAlternateResetThreshold)
        {
            runtime.DisplayCopy.CopyPublishedBufferToWorkBuffer();
            runtime.PointerOverlay.RestorePreviousPointerBackground();
            return;
        }

        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }

    /// <summary>
    ///     Renders the current text-panel animation contents.
    /// </summary>
    [FunctionSymbol("sub_141E1", 0x141E1)]
    internal void RenderTextPanelAnimation()
    {
        short currentTextEntryIndex = runtime.State.RawDataBlock.Control.TransitionTextStartIndex;
        runtime.State.Presentation.Display.CurrentDrawColorIndex = 0x00;
        runtime.DisplayPrimitives.FillRectangleWithCurrentColor(PanelLeftColumn,
            PanelTopRow,
            PanelRightColumn,
            PanelBottomRow);
        runtime.TextCursor.SeedTextBlockCursor(TextStartColumn, TextStartRow);
        runtime.State.Presentation.Display.TextInkColorIndex = TextInkColorIndex;

        for (var renderedLineCount = 0;
             renderedLineCount < VisibleLineCount && currentTextEntryIndex >= 0;
             renderedLineCount++)
        {
            runtime.TextRenderer.RenderStringBlock(GameStringCatalog.ReadTransitionTextStringId(currentTextEntryIndex));
            runtime.TextCursor.AdvanceTextBlockCursorToNextRow();
            currentTextEntryIndex = FindNextEnabledTextAnimationIndex(currentTextEntryIndex);
        }
    }

    /// <summary>
    ///     Finds the next enabled text-panel entry after the current one.
    /// </summary>
    /// <param name="currentTextEntryIndex">Current enabled-entry index.</param>
    [FunctionSymbol("sub_141B3", 0x141B3)]
    internal short FindNextEnabledTextAnimationIndex(short currentTextEntryIndex)
    {
        var enabledFlags = runtime.State.RawDataBlock.Control.TransitionTextEntryStates;
        if (currentTextEntryIndex < 0)
        {
            return -1;
        }

        for (var nextTextEntryIndex = currentTextEntryIndex + 1;
             nextTextEntryIndex < TransitionTextEntryCount;
             nextTextEntryIndex++)
        {
            if (enabledFlags[nextTextEntryIndex] != 0)
            {
                return (short)nextTextEntryIndex;
            }
        }

        return -1;
    }

    /// <summary>
    ///     Redraws the text-panel animation across two overlay frames.
    /// </summary>
    [FunctionSymbol("sub_1425F", 0x1425F)]
    internal void RefreshTextPanelAnimation()
    {
        RenderTextPanelAnimation();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        RenderTextPanelAnimation();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }

    /// <summary>
    ///     Enables one text-panel entry and selects it as the current start index.
    /// </summary>
    /// <param name="textEntryIndex">Prompt-table entry index to enable and select.</param>
    [FunctionSymbol("sub_14270", 0x14270)]
    internal void SelectTransitionTextEntry(ushort textEntryIndex)
    {
        var enabledFlags = runtime.State.RawDataBlock.Control.TransitionTextEntryStates;
        if (textEntryIndex >= TransitionTextEntryCount)
        {
            throw new InvalidOperationException(
                $"Program-state transition text entry index {textEntryIndex} falls outside the 16-entry enable table.");
        }

        enabledFlags[textEntryIndex] = 1;
        runtime.State.RawDataBlock.Control.TransitionTextStartIndex = (byte)textEntryIndex;
    }

    /// <summary>
    ///     Moves the prompt selection backward to the previous enabled entry.
    /// </summary>
    [FunctionSymbol("sub_14283", 0x14283)]
    internal void StepTransitionTextSelectionBackward()
    {
        var control = runtime.State.RawDataBlock.Control;
        var enabledFlags = control.TransitionTextEntryStates;
        var currentTextEntryIndex = control.TransitionTextStartIndex;

        while (currentTextEntryIndex > 0)
        {
            currentTextEntryIndex--;
            if (currentTextEntryIndex == 0 || enabledFlags[currentTextEntryIndex] != 0)
            {
                break;
            }
        }

        control.TransitionTextStartIndex = currentTextEntryIndex;
        RefreshTextPanelAnimation();
    }

    /// <summary>
    ///     Moves the prompt selection forward to the next enabled entry.
    /// </summary>
    [FunctionSymbol("sub_142AD", 0x142AD)]
    internal void StepTransitionTextSelectionForward()
    {
        var control = runtime.State.RawDataBlock.Control;
        var nextTextEntryIndex = FindNextEnabledTextAnimationIndex(control.TransitionTextStartIndex);
        if (nextTextEntryIndex < 0)
        {
            return;
        }

        control.TransitionTextStartIndex = (byte)nextTextEntryIndex;
        RefreshTextPanelAnimation();
    }

    internal static ushort ResolveFallbackSelectedInteractionIndex(short currentTextEntryIndex)
    {
        return (ushort)(currentTextEntryIndex + SelectedInteractionIndexBase);
    }

    internal static ushort ResolveFallbackSelectionLocalIndex(short currentTextEntryIndex)
    {
        return (ushort)(currentTextEntryIndex + SelectionResponseTextIndexBase);
    }

    internal static void ResetInteractionTextBuffer(byte[] textBuffer)
    {
        textBuffer[0] = 0;
    }

    private static DisplayCopyRegion CreateVariantAFullScreenRegion()
    {
        return new DisplayCopyRegion(RuntimeState.FrameWidth, RuntimeState.StageHeight, 0, 0, 0, 0);
    }

    private static DisplayCopyRegion CreateVariantAAnimationRegion(ushort animationGroupIndex)
    {
        return new DisplayCopyRegion(checked((short)VariantAFrameWidthPixels),
            checked((short)VariantAFrameHeightRows),
            checked((short)(animationGroupIndex / 4 * VariantAFrameSourceHeightRows)),
            checked((short)(animationGroupIndex % 4 * VariantAFrameWidthPixels)),
            checked((short)VariantAAnimationDestinationRow),
            checked((short)VariantAAnimationDestinationColumn));
    }

    private static DisplayCopyRegion CreateAlternateQueuedSceneFullScreenRegion()
    {
        return new DisplayCopyRegion(RuntimeState.FrameWidth, RuntimeState.StageHeight, 0, 0, 0, 0);
    }

    private static DisplayCopyRegion CreateAlternateQueuedSceneAnimationRegion(ushort animationGroupIndex)
    {
        return new DisplayCopyRegion(checked((short)AlternateQueuedSceneVariantFrameWidthPixels),
            checked((short)AlternateQueuedSceneVariantFrameHeightRows),
            checked((short)(animationGroupIndex / AlternateQueuedSceneVariantAnimationGroupsPerRow *
                            AlternateQueuedSceneVariantFrameSourceRowStride)),
            checked((short)(animationGroupIndex % AlternateQueuedSceneVariantAnimationGroupsPerRow *
                            AlternateQueuedSceneVariantFrameWidthPixels)),
            checked((short)AlternateQueuedSceneVariantDestinationRow),
            checked((short)AlternateQueuedSceneVariantDestinationColumn));
    }
}
