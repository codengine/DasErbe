using Game.Runtime.Execution.Interaction;
using Game.Shared.Diagnostics;
using Game.Shared.RE;
using Game.State;
using Game.Text;

namespace Game.Runtime.Execution;

internal sealed class InteractionLoop
{
    private const ushort InteractiveTextRow = 0x0083;

    private readonly byte[] _displayTextBuffer = new byte[0x100];
    private readonly InteractionEnterHandler _enterHandler;
    private readonly PromptController _promptController;
    private readonly InteractionPromptTextResolver _promptTextResolver;
    private readonly Erbe _runtime;
    private readonly InteractionSession _session = new();

    internal InteractionLoop(Erbe runtime,
        PromptController promptController,
        SaveScreenController saveScreenController,
        InteractionHandlerRouter handlerRouter)
    {
        _runtime = runtime;
        _promptController = promptController;

        var selectionResolver = new InteractionSelectionResolver(runtime, promptController);
        var dksnInteractionHandler = new DksnInteractionHandler(runtime);
        _promptTextResolver = new InteractionPromptTextResolver(runtime, selectionResolver);
        _enterHandler = new InteractionEnterHandler(runtime,
            promptController,
            saveScreenController,
            handlerRouter,
            dksnInteractionHandler,
            selectionResolver,
            _promptTextResolver);
    }

    /// <summary>
    ///     Runs the interactive selection flow for the current state to completion.
    /// </summary>
    [FunctionSymbol("sub_14C83", 0x14C83)]
    internal ushort RunInteractiveLoop()
    {
        var control = _runtime.State.RawDataBlock.Control;
        _session.Begin();
        var interactiveResult = (ushort)0x0011;
        GameLog.Write(LoggingChannel.Runtime,
            $"sub_14C83 enter state={_runtime.Scenes.FormatStateIdForDiagnostics(control.ProgramStateId)} errorCount={control.ErrorCount}");

        while (true)
        {
            var programState = _runtime.State.Program;
            if (programState.QueuedTransitionText == StringId.None)
            {
                RunInteractiveIteration();
            }

            RunQueuedProgramStateTransitionFollowUp(programState);
            _runtime.PointerOverlay.AdvancePointerOverlayFrame();
            if (_session.LastInput.IsCancelAction)
            {
                interactiveResult = 0x0012;
                LogInteractiveResult("escape", interactiveResult, _session.SelectionPhase, _session.PrimaryDescriptor);
                _session.Reset();
                return interactiveResult;
            }

            if (control is { AdvanceRequestedFlag: 0, ErrorCount: < 5 } &&
                _session.SelectionPhase != InteractiveProgramStatePhase.Completed)
            {
                continue;
            }

            if (control.ErrorCount < 5)
            {
                if (_session.SelectionPhase == InteractiveProgramStatePhase.Completed)
                {
                    control.BackdropColumn = _session.PrimaryDescriptor.ResultBackdropColumn;
                    control.BackdropThresholdRow = _session.PrimaryDescriptor.ResultBackdropThresholdRow;
                    control.BackdropSelectionRow = _session.PrimaryDescriptor.ResultBackdropSelectionRow;
                    interactiveResult = _session.PrimaryDescriptor.NextProgramStateId;
                    LogInteractiveResult("completed",
                        interactiveResult,
                        _session.SelectionPhase,
                        _session.PrimaryDescriptor);
                    _session.Reset();
                    return interactiveResult;
                }

                if (control.AdvanceRequestedFlag != 0)
                {
                    interactiveResult = 0x0002;
                    LogInteractiveResult("advance-flag",
                        interactiveResult,
                        _session.SelectionPhase,
                        _session.PrimaryDescriptor);
                    _session.Reset();
                    return interactiveResult;
                }
            }

            LogInteractiveResult("default", interactiveResult, _session.SelectionPhase, _session.PrimaryDescriptor);
            _session.Reset();
            return interactiveResult;
        }
    }

    private void RunQueuedProgramStateTransitionFollowUp(ProgramState programState)
    {
        if (programState.QueuedTransitionText == StringId.None)
        {
            return;
        }

        if (programState.OzoneAlternateTransitionQueuedFlag != 0)
        {
            _promptController.ShowOzoneAlternateScene();
            return;
        }

        if (programState.OzoneTransitionQueuedFlag != 0)
        {
            _promptController.ShowOzoneScene();
            return;
        }

        _promptController.ShowQueuedTextAnimation();
    }

    private void RunInteractiveIteration()
    {
        _session.LastInput = _runtime.InputAdapter.PollInputEvent();

        _runtime.ProgramScene.RenderCurrentScene();
        _runtime.TransitionEffect.AdvanceTransitionEffect();

        var displayedText = _promptTextResolver.ResolveDisplayedText(_session, _displayTextBuffer);
        var textWidthPixels = _runtime.TextRenderer.MeasureTextWidthPixels(displayedText);
        _runtime.TextCursor.SeedTextBlockCursor((ushort)((RuntimeState.FrameWidth - textWidthPixels) >> 1),
            InteractiveTextRow);
        _runtime.TextRenderer.RenderTextBlock(displayedText);

        if (_session.LastInput.IsPrimaryConfirmAction)
        {
            _enterHandler.HandleEnterInputForCurrentSelectionPhase(_session);
        }
    }

    private void LogInteractiveResult(string reason,
        ushort nextState,
        InteractiveProgramStatePhase selectionPhase,
        InteractiveSelectionDescriptor primaryDescriptor)
    {
        GameLog.Write(LoggingChannel.Runtime,
            $"sub_14C83 exit reason={reason} nextState={_runtime.Scenes.FormatStateIdForDiagnostics(nextState)} phase={selectionPhase} descriptorNext={_runtime.Scenes.FormatStateIdForDiagnostics(primaryDescriptor.NextProgramStateId)} backdrop=({primaryDescriptor.ResultBackdropColumn},{primaryDescriptor.ResultBackdropThresholdRow},{primaryDescriptor.ResultBackdropSelectionRow})");
    }
}
