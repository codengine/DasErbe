using Game.Shared.Host;
using Game.Shared.Host.Input;
using Game.Shared.Input;
using Microsoft.Xna.Framework.Input;

namespace Game.Desktop.MonoGame;

/// <summary>
///     Polls MonoGame input into host-neutral frames.
/// </summary>
public sealed class MonoGameInputBackend : IInputBackend
{
    private readonly KeyboardCapture _keyboardCapture;
    private readonly TimeProvider _timeProvider;
    private bool _suppressEnterUntilRelease;

    /// <summary>
    ///     Creates the input backend.
    /// </summary>
    public MonoGameInputBackend()
    {
        _timeProvider = TimeProvider.System;
        _keyboardCapture = new KeyboardCapture();
    }

    /// <summary>
    ///     Resets host-shortcut suppression and queued keyboard repeat state.
    /// </summary>
    public void Reset()
    {
        _suppressEnterUntilRelease = false;
        _keyboardCapture.Reset();
    }

    /// <summary>
    ///     Polls keyboard and mouse state and maps it into one input frame.
    /// </summary>
    /// <param name="rect">Current presentation rect used to map window coordinates into content coordinates.</param>
    public InputFrame Poll(HostPresentationRect rect)
    {
        var keyboard = Keyboard.GetState();
        var pressedKeys = keyboard.GetPressedKeys();
        if (keyboard.IsKeyDown(Keys.Enter) && (keyboard.IsKeyDown(Keys.LeftAlt) || keyboard.IsKeyDown(Keys.RightAlt)))
        {
            _suppressEnterUntilRelease = true;
        }

        if (_suppressEnterUntilRelease)
        {
            if (keyboard.IsKeyUp(Keys.Enter))
            {
                _suppressEnterUntilRelease = false;
            }
            else
            {
                pressedKeys = [.. pressedKeys.Where(key => key != Keys.Enter)];
            }
        }

        var keyboardCaptureFrame = _keyboardCapture.Capture(pressedKeys,
            _timeProvider.GetElapsedTime(0, _timeProvider.GetTimestamp()));

        var mouse = Mouse.GetState();
        return CreateSnapshot(rect,
            keyboardCaptureFrame.PressedKeys,
            mouse.X,
            mouse.Y,
            mouse.LeftButton == ButtonState.Pressed,
            keyboardCaptureFrame.QueuedKeyStrokes);
    }

    /// <summary>
    ///     Creates an input frame from already-sampled pointer and keyboard state.
    /// </summary>
    /// <param name="rect">Current presentation rect.</param>
    /// <param name="pressedKeys">Keys currently pressed.</param>
    /// <param name="mouseX">The sampled mouse X coordinate in window space.</param>
    /// <param name="mouseY">The sampled mouse Y coordinate in window space.</param>
    /// <param name="isLeftPressed">Whether the left pointer button is pressed.</param>
    /// <param name="queuedKeyStrokes">Optional queued keystrokes captured during this sample.</param>
    private static InputFrame CreateSnapshot(HostPresentationRect rect,
        IEnumerable<InputKey> pressedKeys,
        int mouseX,
        int mouseY,
        bool isLeftPressed,
        IReadOnlyList<InputKeyStroke>? queuedKeyStrokes = null)
    {
        var boundsWidth = Math.Max(1, rect.ContentWidth);
        var boundsHeight = Math.Max(1, rect.ContentHeight);
        var localMouseX = mouseX - rect.ContentX;
        var localMouseY = mouseY - rect.ContentY;
        var isPointerInside = localMouseX >= 0 && localMouseY >= 0 && localMouseX < boundsWidth &&
                              localMouseY < boundsHeight;

        var mouseButtons = InputMouseButtons.None;
        if (isPointerInside && isLeftPressed)
        {
            mouseButtons |= InputMouseButtons.Primary;
        }

        var pointer = new InputPointerState(isPointerInside,
            Math.Clamp(localMouseX, 0, boundsWidth - 1),
            Math.Clamp(localMouseY, 0, boundsHeight - 1),
            boundsWidth,
            boundsHeight);

        return new InputFrame(pointer, mouseButtons, queuedKeyStrokes ?? [], [.. pressedKeys]);
    }
}
