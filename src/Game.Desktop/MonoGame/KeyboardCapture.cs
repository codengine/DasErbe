using Game.Shared.Input;
using Microsoft.Xna.Framework.Input;

namespace Game.Desktop.MonoGame;

/// <summary>
///     Captures the keyboard state the game actually uses and applies repeat handling.
/// </summary>
internal sealed class KeyboardCapture
{
    private static readonly TimeSpan InitialRepeatDelay = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan RepeatInterval = TimeSpan.FromMilliseconds(33);

    private readonly Dictionary<Keys, KeyRepeatState> _heldKeys = [];

    /// <summary>
    ///     Captures the current keyboard state.
    /// </summary>
    /// <param name="pressedKeys">The keys currently pressed in MonoGame's key space.</param>
    /// <param name="timestamp">The timestamp used for key-repeat scheduling.</param>
    public KeyboardCaptureFrame Capture(IReadOnlyList<Keys> pressedKeys, TimeSpan timestamp)
    {
        if (pressedKeys.Count == 0)
        {
            _heldKeys.Clear();
            return KeyboardCaptureFrame.Empty;
        }

        var pressedInputKeys = new List<InputKey>(pressedKeys.Count);
        var queuedKeyStrokes = new List<InputKeyStroke>(pressedKeys.Count);
        var activeRepeatKeys = new HashSet<Keys>();
        foreach (var key in pressedKeys)
        {
            if (IsModifierKey(key))
            {
                continue;
            }

            var pressedInputKey = MapPressedKey(key);
            if (pressedInputKey != InputKey.None && !pressedInputKeys.Contains(pressedInputKey))
            {
                pressedInputKeys.Add(pressedInputKey);
            }

            if (!TryMapQueuedKeyStroke(key, out var stroke))
            {
                continue;
            }

            activeRepeatKeys.Add(key);
            if (!_heldKeys.TryGetValue(key, out var repeatState))
            {
                queuedKeyStrokes.Add(stroke);
                _heldKeys[key] = new KeyRepeatState(timestamp + InitialRepeatDelay);
                continue;
            }

            while (timestamp >= repeatState.NextRepeatAt)
            {
                queuedKeyStrokes.Add(stroke);
                repeatState = new KeyRepeatState(repeatState.NextRepeatAt + RepeatInterval);
            }

            _heldKeys[key] = repeatState;
        }

        foreach (var key in _heldKeys.Keys.ToArray())
        {
            if (!activeRepeatKeys.Contains(key))
            {
                _heldKeys.Remove(key);
            }
        }

        return new KeyboardCaptureFrame([.. pressedInputKeys], [.. queuedKeyStrokes]);
    }

    /// <summary>
    ///     Clears held-key repeat state.
    /// </summary>
    public void Reset()
    {
        _heldKeys.Clear();
    }

    private static bool IsModifierKey(Keys key)
    {
        return key is Keys.LeftShift or Keys.RightShift or Keys.LeftAlt or Keys.RightAlt or Keys.LeftControl
            or Keys.RightControl;
    }

    private static InputKey MapPressedKey(Keys key)
    {
        return key switch
        {
            Keys.Space => InputKey.Space,
            _ => InputKey.None
        };
    }

    private static bool TryMapQueuedKeyStroke(Keys key, out InputKeyStroke stroke)
    {
        if (TryMapNonTextKeyStroke(key, out stroke))
        {
            return true;
        }

        if (TryMapDigitCharacterKey(key, out var character))
        {
            stroke = new InputKeyStroke(InputKey.None, character);
            return true;
        }

        stroke = default;
        return false;
    }

    private static bool TryMapNonTextKeyStroke(Keys key, out InputKeyStroke stroke)
    {
        var mappedKey = key switch
        {
            Keys.Enter => InputKey.Enter,
            Keys.Back => InputKey.Backspace,
            _ => InputKey.None
        };

        if (mappedKey == InputKey.None)
        {
            stroke = default;
            return false;
        }

        stroke = new InputKeyStroke(mappedKey, null);
        return true;
    }

    private static bool TryMapDigitCharacterKey(Keys key, out char character)
    {
        switch (key)
        {
            case >= Keys.D0 and <= Keys.D9:
                character = (char)('0' + (key - Keys.D0));
                return true;

            case >= Keys.NumPad0 and <= Keys.NumPad9:
                character = (char)('0' + (key - Keys.NumPad0));
                return true;

            default:
                character = '\0';
                return false;
        }
    }

    private readonly record struct KeyRepeatState(TimeSpan NextRepeatAt);
}
