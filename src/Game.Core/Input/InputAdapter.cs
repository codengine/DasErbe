using Game.Shared.Diagnostics;
using Game.Shared.Input;
using Game.Shared.RE;
using Game.State;

namespace Game.Input;

/// <summary>
///     Projects host input into the carried-over pointer and keyboard state consumed by the game runtime.
/// </summary>
/// <param name="input">Live input state.</param>
internal sealed class InputAdapter(InputState input)
{
    /// <summary>
    ///     Gets whether the current projected pointer state has the primary button pressed.
    /// </summary>
    private bool _isPrimaryButtonPressed;

    /// <summary>
    ///     Gets whether the previous projected pointer state had the primary button pressed.
    /// </summary>
    private bool _wasPrimaryButtonPressed;
    private InputFrame _currentInput = InputFrame.Empty;
    private readonly Queue<InputKeyStroke> _queuedKeyStrokes = [];
    private readonly Queue<InputFrame> _queuedMouseButtonFrames = [];

    /// <summary>
    ///     Captures one host input frame and queues keyboard and mouse-button edges for later polling.
    /// </summary>
    /// <param name="frame">Raw host input frame.</param>
    internal void CaptureHostInput(InputFrame frame)
    {
        if (frame.MouseButtons != _currentInput.MouseButtons)
        {
            _queuedMouseButtonFrames.Enqueue(frame);
        }

        _currentInput = frame;
        foreach (var queuedKeyStroke in frame.QueuedKeyStrokes)
        {
            _queuedKeyStrokes.Enqueue(queuedKeyStroke);
        }
    }

    /// <summary>
    ///     Refreshes the carried-over pointer and keyboard-backed input state.
    /// </summary>
    [FunctionSymbol("sub_11917", 0x11917)]
    internal void RefreshPointerInput()
    {
        var frame = _queuedMouseButtonFrames.TryDequeue(out var queuedMouseButtonFrame)
            ? queuedMouseButtonFrame
            : _currentInput;
        _wasPrimaryButtonPressed = _isPrimaryButtonPressed;

        // IDA 0x11922..0x1194F adapted: project the latest host pointer frame directly and keep semantic button-edge
        // ownership locally in this adapter rather than splitting between pointer and keyboard-steering modes.
        ProjectPointerState(frame, input);
    }

    /// <summary>
    ///     Polls the next semantic input event from keyboard or pointer state.
    /// </summary>
    [FunctionSymbol("sub_117F2", 0x117F2)]
    internal RuntimeInputEvent PollInputEvent()
    {
        var inputEvent = ResolveMouseInputEvent();
        if (inputEvent != RuntimeInputEvent.None)
        {
            LogInputEvent(inputEvent, input);
            return inputEvent;
        }

        // IDA 0x117F8..0x11842: poll one queued BIOS keyboard item when present. The managed runtime keeps the existing
        // runtime-owned queued-keystroke seam and forwards the host-derived MonoGame keyboard event semantically instead
        // of re-encoding it as a DOS byte/scan-code pair.
        if (_queuedKeyStrokes.TryDequeue(out var queuedKeyStroke))
        {
            inputEvent = RuntimeInputEvent.Keyboard(queuedKeyStroke);
            LogInputEvent(inputEvent, input);
            return inputEvent;
        }

        return RuntimeInputEvent.None;
    }

    private void ProjectPointerState(InputFrame frame, InputState state)
    {
        var pointer = frame.Pointer;
        var canProjectPointerCoordinates = pointer.IsAvailable || pointer is { BoundsWidth: > 0, BoundsHeight: > 0 };
        if (canProjectPointerCoordinates)
        {
            state.PointerColumn =
                ScaleToInclusiveRange(pointer.X, pointer.BoundsWidth, InputState.MinColumn, InputState.MaxColumn);
            state.PointerRow =
                ScaleToInclusiveRange(pointer.Y, pointer.BoundsHeight, InputState.MinRow, InputState.MaxRow);
        }

        _isPrimaryButtonPressed = frame.IsMouseButtonPressed(InputMouseButtons.Primary);
    }

    private RuntimeInputEvent ResolveMouseInputEvent()
    {
        return _isPrimaryButtonPressed && !_wasPrimaryButtonPressed
            ? RuntimeInputEvent.PrimaryClick
            : RuntimeInputEvent.None;
    }

    private static ushort ScaleToInclusiveRange(int coordinate, int bounds, ushort minValue, ushort maxValue)
    {
        if (maxValue < minValue)
        {
            throw new ArgumentOutOfRangeException(nameof(maxValue), "Inclusive pointer bounds must not be inverted.");
        }

        if (bounds <= 1)
        {
            return minValue;
        }

        var clampedCoordinate = Math.Clamp(coordinate, 0, bounds - 1);
        var range = maxValue - minValue;
        return (ushort)(minValue + clampedCoordinate * range / (bounds - 1));
    }

    private void LogInputEvent(RuntimeInputEvent inputEvent, InputState input)
    {
        if (inputEvent == RuntimeInputEvent.None)
        {
            return;
        }

        GameLog.Debug(LoggingChannel.Input,
            $"sub_117F2 input={inputEvent.FormatForDiagnostics()} pointer=({input.PointerColumn},{input.PointerRow}) primary={_isPrimaryButtonPressed}");
    }
}
