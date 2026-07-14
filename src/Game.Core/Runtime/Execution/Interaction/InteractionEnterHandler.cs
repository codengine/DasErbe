using Game.Catalogs;
using Game.Shared.Diagnostics;
using Game.State;
using Game.Text;

namespace Game.Runtime.Execution.Interaction;

internal sealed class InteractionEnterHandler(
    Erbe runtime,
    PromptController promptController,
    SaveScreenController saveScreenController,
    InteractionHandlerRouter handlerRouter,
    DksnInteractionHandler dksnInteractionHandler,
    InteractionSelectionResolver selectionResolver,
    InteractionPromptTextResolver promptTextResolver)
{
    internal void HandleEnterInputForCurrentSelectionPhase(InteractionSession session)
    {
        switch (session.SelectionPhase)
        {
            case InteractiveProgramStatePhase.PrimarySceneSelection:
                HandlePrimarySceneSelectionEnter(session);
                return;

            case InteractiveProgramStatePhase.SecondarySceneSelection:
                HandleSecondarySceneSelectionEnter(session);
                return;

            case InteractiveProgramStatePhase.ConfirmPrimary:
            case InteractiveProgramStatePhase.ConfirmSecondary:
                HandleConfirmationSelectionEnter(session);
                return;

            default:
                HandlePrimaryControlSelectionEnter(session);
                return;
        }
    }

    private void ContinueConfirmationSelectionAfterBackdropAnimation(InteractionSession session)
    {
        session.SelectionPhase = InteractiveProgramStatePhase.PrimaryControlSelection;

        switch (session.PrimaryButton)
        {
            case InteractionButton.Inspect:
            {
                var currentSelectionHandlerId =
                    runtime.Interactions.ReadHandlerId(session.PrimarySelectedInteractionIndex, 0x00);
                if (currentSelectionHandlerId != InteractionHandlerId.None)
                {
                    handlerRouter.DispatchInteractionOrThrow("current selection",
                        session.PrimarySelectedInteractionIndex,
                        0x00,
                        currentSelectionHandlerId);
                    break;
                }

                HandleMissingHotspotAction(session.PrimaryDescriptorRef,
                    DksnFallbackBucket.Inspect,
                    promptController.RunDefaultInteractionTextAnimation);
                break;
            }

            case InteractionButton.Read:
                _ = DispatchCurrentSelectionHandler(session,
                    session.PrimaryDescriptorRef,
                    "current selection",
                    session.PrimarySelectedInteractionIndex,
                    0x08,
                    DksnFallbackBucket.Read,
                    runtime.Interactions.ReadHandlerId(session.PrimarySelectedInteractionIndex, 0x08));
                break;

            case InteractionButton.Write:
                HandleMissingHotspotAction(session.PrimaryDescriptorRef, DksnFallbackBucket.Write);
                break;

            case InteractionButton.SitStand:
                _ = DispatchCurrentSelectionHandler(session,
                    session.PrimaryDescriptorRef,
                    "current selection",
                    session.PrimarySelectedInteractionIndex,
                    0x0A,
                    DksnFallbackBucket.SitStand,
                    runtime.Interactions.ReadHandlerId(session.PrimarySelectedInteractionIndex, 0x0A));
                break;

            case InteractionButton.Take:
            {
                var currentSelectionHandlerId =
                    runtime.Interactions.ReadHandlerId(session.PrimarySelectedInteractionIndex, 0x02);
                if (currentSelectionHandlerId == InteractionHandlerId.None)
                {
                    HandleMissingHotspotAction(session.PrimaryDescriptorRef, DksnFallbackBucket.Take);
                    break;
                }

                if (session.PrimaryDescriptor.SelectionStateId == 4)
                {
                    runtime.Interactions.SetSelectionState(session.PrimaryDescriptorRef, 5);
                    session.PrimaryDescriptor = session.PrimaryDescriptor with { SelectionStateId = 5 };
                }

                handlerRouter.DispatchInteractionOrThrow("current selection",
                    session.PrimarySelectedInteractionIndex,
                    0x02,
                    currentSelectionHandlerId);
                break;
            }

            case InteractionButton.Buy:
                _ = DispatchCurrentSelectionHandler(session,
                    session.PrimaryDescriptorRef,
                    "current selection",
                    session.PrimarySelectedInteractionIndex,
                    0x0C,
                    DksnFallbackBucket.Buy,
                    runtime.Interactions.ReadHandlerId(session.PrimarySelectedInteractionIndex, 0x0C));
                break;

            case InteractionButton.OpenClose:
                _ = DispatchCurrentSelectionHandler(session,
                    session.PrimaryDescriptorRef,
                    "current selection",
                    session.PrimarySelectedInteractionIndex,
                    0x06,
                    DksnFallbackBucket.OpenClose,
                    runtime.Interactions.ReadHandlerId(session.PrimarySelectedInteractionIndex, 0x06));
                break;

            case InteractionButton.GoTo:
            {
                var currentSelectionHandlerId =
                    runtime.Interactions.ReadHandlerId(session.PrimarySelectedInteractionIndex, 0x0E);
                if (currentSelectionHandlerId != InteractionHandlerId.None)
                {
                    handlerRouter.DispatchInteractionOrThrow("current selection",
                        session.PrimarySelectedInteractionIndex,
                        0x0E,
                        currentSelectionHandlerId);
                    break;
                }

                if (session.PrimaryRecord is { StateId: 1, State: 0x0080 })
                {
                    session.SelectionPhase = InteractiveProgramStatePhase.Completed;
                    GameLog.Write(LoggingChannel.Runtime,
                        $"sub_14C83 completion via controlMask 0x{InteractionSelectionResolver.ToMask(session.PrimaryButton):X4} -> nextState={runtime.Scenes.FormatStateIdForDiagnostics(session.PrimaryDescriptor.NextProgramStateId)}");
                }

                break;
            }

            case InteractionButton.Use:
                var handledUseAction = false;
                var continuationPhase = InteractiveProgramStatePhase.PrimaryControlSelection;
                if (session.PrimaryRecord.StateId == 2 && session.PrimaryDescriptor.NextProgramStateId != 0)
                {
                    continuationPhase = InteractiveProgramStatePhase.Completed;
                    session.SelectionPhase = continuationPhase;
                    GameLog.Write(LoggingChannel.Runtime,
                        $"sub_14C83 completion via controlMask 0x{InteractionSelectionResolver.ToMask(session.PrimaryButton):X4} -> nextState={runtime.Scenes.FormatStateIdForDiagnostics(session.PrimaryDescriptor.NextProgramStateId)}");
                }

                if (session.ConfirmationPassCount == 0)
                {
                    if (session.PrimaryRecord.LinkedDescriptorKey == 0)
                    {
                        var currentSelectionHandlerId =
                            runtime.Interactions.ReadHandlerId(session.PrimarySelectedInteractionIndex, 0x04);
                        handledUseAction = DispatchCurrentSelectionHandler(session,
                            session.PrimaryDescriptorRef,
                            "current selection",
                            session.PrimarySelectedInteractionIndex,
                            0x04,
                            DksnFallbackBucket.Use,
                            currentSelectionHandlerId);
                    }
                }
                else if (session.PrimaryRecord.LinkedDescriptorKey == session.SecondaryDescriptor.SelectionEntryIndex)
                {
                    var currentSelectionHandlerId =
                        runtime.Interactions.ReadHandlerId(session.PrimarySelectedInteractionIndex, 0x04);
                    handledUseAction = DispatchCurrentSelectionHandler(session,
                        session.PrimaryDescriptorRef,
                        "current selection",
                        session.PrimarySelectedInteractionIndex,
                        0x04,
                        DksnFallbackBucket.Use,
                        currentSelectionHandlerId);
                }
                else if (session.SecondaryRecord.LinkedDescriptorKey == session.PrimaryDescriptor.SelectionEntryIndex)
                {
                    var secondarySelectionHandlerId =
                        runtime.Interactions.ReadHandlerId(session.SecondarySelectedInteractionIndex, 0x04);
                    handledUseAction = DispatchCurrentSelectionHandler(session,
                        session.SecondaryDescriptorRef,
                        "secondary selection",
                        session.SecondarySelectedInteractionIndex,
                        0x04,
                        DksnFallbackBucket.Use,
                        secondarySelectionHandlerId);
                }

                if (!handledUseAction && continuationPhase == InteractiveProgramStatePhase.PrimaryControlSelection)
                {
                    HandleMissingHotspotAction(session.PrimaryDescriptorRef, DksnFallbackBucket.Use);
                }

                session.SelectionPhase = continuationPhase;
                break;
        }

        promptController.RefreshTextPanelAnimation();
        PromptController.ResetInteractionTextBuffer(session.TextBuffer);
    }

    private void HandlePrimarySceneSelectionEnter(InteractionSession session)
    {
        if (!selectionResolver.TryResolveCurrentStateOrTransitionSelection(
                runtime.State.RawDataBlock.Control.ProgramStateId,
                out var resolution,
                out _))
        {
            HandlePrimaryControlSelectionEnter(session);
            return;
        }

        CommitPrimarySceneSelection(session, resolution);
    }

    private void CommitPrimarySceneSelection(InteractionSession session, InteractionSelectionResolution resolution)
    {
        session.PrimarySelectedInteractionLocalIndex = resolution.SelectionLocalIndex;
        session.PrimarySelectedInteractionIndex = resolution.SelectedInteractionIndex;
        session.PrimaryDescriptorRef = resolution.DescriptorRef;
        session.PrimaryDescriptor = resolution.Descriptor;
        session.PrimaryRecord =
            runtime.State.RawDataBlock.SelectionTable[session.PrimaryDescriptor.SelectionEntryIndex];
        PromptController.AppendTextToBuffer(session.TextBuffer,
            runtime.Strings.GetCp437String(session.PrimaryDescriptor.SelectionText));
        GameLog.Write(LoggingChannel.Runtime,
            $"sub_14C83 primary selection local=0x{session.PrimarySelectedInteractionLocalIndex:X4} global=0x{session.PrimarySelectedInteractionIndex:X4} text={runtime.Strings.FormatStringId(session.PrimaryDescriptor.SelectionText)} nextState={runtime.Scenes.FormatStateIdForDiagnostics(session.PrimaryDescriptor.NextProgramStateId)} selectionEntry=0x{session.PrimaryDescriptor.SelectionEntryIndex:X4}");

        if (runtime.UseClassicInteractions || session.PrimaryButton == InteractionButton.Use)
        {
            session.SelectionPhase = InteractiveProgramStatePhase.ConfirmPrimary;
            return;
        }

        if (session.PrimarySelectedInteractionLocalIndex < 0x0080)
        {
            runtime.ProgramScene.AnimateBackdropSelection(session.PrimaryDescriptorRef);
        }

        ContinueConfirmationSelectionAfterBackdropAnimation(session);
    }

    private void HandlePrimaryControlSelectionEnter(InteractionSession session)
    {
        session.ConfirmationPassCount = 0;
        var selectedGlobalControlIndex = selectionResolver.FindGlobalControlSelection();
        if (selectedGlobalControlIndex < 0)
        {
            if (!runtime.UseClassicInteractions)
            {
                TryHandleDefaultGoToSelectionEnter(session);
            }

            return;
        }

        if (InteractionSelectionResolver.TryResolveButton(selectedGlobalControlIndex) is not { } selectedButton)
        {
            return;
        }

        switch (selectedButton)
        {
            case InteractionButton.PreviousText:
                promptController.StepTransitionTextSelectionBackward();
                return;

            case InteractionButton.NextText:
                promptController.StepTransitionTextSelectionForward();
                return;

            case InteractionButton.Save:
                saveScreenController.StepSaveScreen();
                promptController.RefreshTextPanelAnimation();
                PromptController.ResetInteractionTextBuffer(session.TextBuffer);
                session.SelectionPhase = InteractiveProgramStatePhase.PrimaryControlSelection;
                return;
        }

        switch (selectedButton)
        {
            case >= InteractionButton.With and < InteractionButton.Back:
            case > InteractionButton.Empty:
                return;
        }

        session.PrimaryButton = selectedButton;

        if (selectedButton <= InteractionButton.SitStand)
        {
            session.SelectionPhase = InteractiveProgramStatePhase.PrimarySceneSelection;
        }

        promptTextResolver.CopyPrimaryActionPromptText(selectedButton, session.TextBuffer);
    }

    private void HandleSecondarySceneSelectionEnter(InteractionSession session)
    {
        if (selectionResolver.TryResolveCurrentStateOrTransitionSelection(
                runtime.State.RawDataBlock.Control.ProgramStateId,
                out var resolution,
                out var usedFallback))
        {
            CommitSecondarySceneSelection(session, resolution, usedFallback);
            return;
        }

        var selectedGlobalControlIndex = selectionResolver.FindGlobalControlSelection();
        if (InteractionSelectionResolver.TryResolveButton(selectedGlobalControlIndex) is not { } selectedButton)
        {
            return;
        }

        switch (selectedButton)
        {
            case InteractionButton.PreviousText:
                promptController.StepTransitionTextSelectionBackward();
                return;

            case InteractionButton.NextText:
                promptController.StepTransitionTextSelectionForward();
                return;

            case InteractionButton.Back:
                session.SelectionPhase = InteractiveProgramStatePhase.PrimaryControlSelection;
                session.ConfirmationPassCount = 0;
                PromptController.CopyTextToBuffer(session.TextBuffer,
                    runtime.Strings.GetCp437String(
                        GameStringCatalog.ReadInteractionResponseStringId((ushort)selectedGlobalControlIndex)));
                return;
        }
    }

    private void CommitSecondarySceneSelection(InteractionSession session,
        InteractionSelectionResolution resolution,
        bool usedFallback)
    {
        session.SecondarySelectedInteractionLocalIndex = resolution.SelectionLocalIndex;
        session.SecondarySelectedInteractionIndex = resolution.SelectedInteractionIndex;
        session.SecondaryDescriptorRef = resolution.DescriptorRef;
        session.SecondaryDescriptor = resolution.Descriptor;
        session.SecondaryRecord =
            runtime.State.RawDataBlock.SelectionTable[session.SecondaryDescriptor.SelectionEntryIndex];
        PromptController.AppendTextToBuffer(session.TextBuffer,
            runtime.Strings.GetCp437String(session.SecondaryDescriptor.SelectionText));
        session.SelectionPhase = InteractiveProgramStatePhase.ConfirmSecondary;
        GameLog.Write(LoggingChannel.Runtime,
            $"sub_14C83 {(usedFallback ? "fallback " : string.Empty)}secondary selection local=0x{session.SecondarySelectedInteractionLocalIndex:X4} global=0x{session.SecondarySelectedInteractionIndex:X4} text={runtime.Strings.FormatStringId(session.SecondaryDescriptor.SelectionText)} nextState={runtime.Scenes.FormatStateIdForDiagnostics(session.SecondaryDescriptor.NextProgramStateId)} selectionEntry=0x{session.SecondaryDescriptor.SelectionEntryIndex:X4}");
    }

    private void TryHandleDefaultGoToSelectionEnter(InteractionSession session)
    {
        if (!selectionResolver.TryResolveCurrentStateSelection(runtime.State.RawDataBlock.Control.ProgramStateId,
                out var resolution))
        {
            return;
        }

        session.PrimaryButton = InteractionButton.GoTo;
        session.SelectionPhase = InteractiveProgramStatePhase.PrimarySceneSelection;
        promptTextResolver.CopyPrimaryActionPromptText(InteractionButton.GoTo, session.TextBuffer);
        CommitPrimarySceneSelection(session, resolution);
    }

    private void HandleConfirmationSelectionEnter(InteractionSession session)
    {
        var currentSelectionLocalIndex = selectionResolver.ResolveConfirmationSelectionLocalIndex(session);
        if (currentSelectionLocalIndex == ushort.MaxValue)
        {
            return;
        }

        if (InteractionSelectionResolver.TryResolveButton(currentSelectionLocalIndex) is { } currentSelectionButton)
        {
            switch (currentSelectionButton)
            {
                case InteractionButton.With when session is
                {
                    SelectionPhase: InteractiveProgramStatePhase.ConfirmPrimary, PrimaryButton: InteractionButton.Use
                }:
                    PromptController.AppendTextToBuffer(session.TextBuffer,
                        runtime.Strings.GetCp437String(
                            GameStringCatalog.ReadInteractionResponseStringId(currentSelectionLocalIndex)));
                    session.SelectionPhase = InteractiveProgramStatePhase.SecondarySceneSelection;
                    session.ConfirmationPassCount++;
                    return;

                case InteractionButton.ConfirmNo:
                    PromptController.ResetInteractionTextBuffer(session.TextBuffer);
                    session.SelectionPhase = InteractiveProgramStatePhase.PrimaryControlSelection;
                    return;

                case InteractionButton.Empty when session.PrimarySelectedInteractionLocalIndex < 0x0080:
                    runtime.ProgramScene.AnimateBackdropSelection(session.PrimaryDescriptorRef);
                    break;
            }
        }

        ContinueConfirmationSelectionAfterBackdropAnimation(session);
    }

    private void HandleMissingHotspotAction(InteractionDescriptorRef descriptorRef,
        DksnFallbackBucket commandBucket,
        Action? legacyFallback = null)
    {
        if (runtime.DksnMode && dksnInteractionHandler.TryRunMissingHotspotAction(descriptorRef, commandBucket))
        {
            return;
        }

        legacyFallback?.Invoke();
    }

    private bool DispatchCurrentSelectionHandler(InteractionSession session,
        InteractionDescriptorRef descriptorRef,
        string selectionContext,
        ushort selectedInteractionIndex,
        int handlerByteOffset,
        DksnFallbackBucket commandBucket,
        InteractionHandlerId handlerId)
    {
        if (handlerId == InteractionHandlerId.None)
        {
            _ = session;
            HandleMissingHotspotAction(descriptorRef, commandBucket);
            return true;
        }

        handlerRouter.DispatchInteractionOrThrow(selectionContext,
            selectedInteractionIndex,
            handlerByteOffset,
            handlerId);
        return true;
    }
}
