namespace Game.Shared.Input;

/// <summary>
///     Raw input captured for one frame.
/// </summary>
public sealed class InputFrame
{
    private readonly InputKey[] _pressedKeys;
    private readonly InputKeyStroke[] _queuedKeyStrokes;

    /// <summary>
    ///     Creates one frame of pointer, mouse-button, and keyboard state.
    /// </summary>
    /// <param name="pointer">Pointer state for the frame.</param>
    /// <param name="mouseButtons">Pressed mouse buttons.</param>
    /// <param name="queuedKeyStrokes">Queued keystrokes captured for the frame.</param>
    /// <param name="pressedKeys">Pressed keys.</param>
    /// <exception cref="ArgumentNullException">
    ///     <paramref name="queuedKeyStrokes" /> or <paramref name="pressedKeys" /> is
    ///     <see langword="null" />.
    /// </exception>
    public InputFrame(InputPointerState pointer,
        InputMouseButtons mouseButtons,
        IReadOnlyList<InputKeyStroke> queuedKeyStrokes,
        params InputKey[] pressedKeys)
    {
        Pointer = pointer;
        MouseButtons = mouseButtons;
        _queuedKeyStrokes = NormalizeQueuedKeyStrokes(
            queuedKeyStrokes ?? throw new ArgumentNullException(nameof(queuedKeyStrokes)));
        _pressedKeys = NormalizeKeys(pressedKeys ?? throw new ArgumentNullException(nameof(pressedKeys)));
    }

    /// <summary>
    ///     Empty input frame.
    /// </summary>
    public static InputFrame Empty { get; } = new(InputPointerState.Empty, InputMouseButtons.None, []);

    /// <summary>
    ///     Pointer state for the frame.
    /// </summary>
    public InputPointerState Pointer { get; }

    /// <summary>
    ///     Pressed mouse buttons for the frame.
    /// </summary>
    public InputMouseButtons MouseButtons { get; }

    /// <summary>
    ///     Queued keystrokes in capture order.
    /// </summary>
    public IReadOnlyList<InputKeyStroke> QueuedKeyStrokes => _queuedKeyStrokes;

    /// <summary>
    ///     Pressed keys in stable sorted order.
    /// </summary>
    public IReadOnlyList<InputKey> PressedKeys => _pressedKeys;

    /// <summary>
    ///     Returns whether a key is pressed in this frame.
    /// </summary>
    /// <param name="key">Key to query.</param>
    /// <returns><see langword="true" /> if the key is pressed; otherwise <see langword="false" />.</returns>
    public bool IsKeyPressed(InputKey key)
    {
        return key != InputKey.None && Array.BinarySearch(_pressedKeys, key) >= 0;
    }

    /// <summary>
    ///     Returns whether all requested mouse buttons are pressed in this frame.
    /// </summary>
    /// <param name="buttons">Mouse buttons to query.</param>
    /// <returns><see langword="true" /> if all requested buttons are pressed; otherwise <see langword="false" />.</returns>
    public bool IsMouseButtonPressed(InputMouseButtons buttons)
    {
        return buttons != InputMouseButtons.None && (MouseButtons & buttons) == buttons;
    }

    private static InputKeyStroke[] NormalizeQueuedKeyStrokes(IReadOnlyList<InputKeyStroke> queuedKeyStrokes)
    {
        if (queuedKeyStrokes.Count == 0)
        {
            return [];
        }

        var normalizedKeyStrokes = new InputKeyStroke[queuedKeyStrokes.Count];
        for (var index = 0; index < queuedKeyStrokes.Count; index++)
        {
            normalizedKeyStrokes[index] = queuedKeyStrokes[index];
        }

        return normalizedKeyStrokes;
    }

    private static InputKey[] NormalizeKeys(InputKey[] pressedKeys)
    {
        if (pressedKeys.Length == 0)
        {
            return [];
        }

        var filteredKeys = new HashSet<InputKey>();

        foreach (var key in pressedKeys)
        {
            if (key != InputKey.None)
            {
                filteredKeys.Add(key);
            }
        }

        if (filteredKeys.Count == 0)
        {
            return [];
        }

        var normalizedKeys = filteredKeys.ToArray();
        Array.Sort(normalizedKeys);

        return normalizedKeys;
    }
}
