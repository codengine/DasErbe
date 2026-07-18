using Game.Shared.Input;

namespace Game.Input;

/// <summary>
///     One semantic runtime input event forwarded from the MonoGame-backed host input seam.
/// </summary>
/// <param name="Kind">Semantic event kind.</param>
/// <param name="KeyStroke">Queued keyboard item when <see cref="Kind" /> is <see cref="RuntimeInputEventKind.Keyboard" />.</param>
internal readonly record struct RuntimeInputEvent(RuntimeInputEventKind Kind, InputKeyStroke KeyStroke)
{
    /// <summary>
    ///     Gets the empty input event.
    /// </summary>
    internal static RuntimeInputEvent None { get; } = new(RuntimeInputEventKind.None, default);

    /// <summary>
    ///     Gets the semantic primary-click event.
    /// </summary>
    internal static RuntimeInputEvent PrimaryClick { get; } = new(RuntimeInputEventKind.PrimaryClick, default);

    /// <summary>
    ///     Gets whether this event should be treated as the current primary confirm action.
    /// </summary>
    internal bool IsPrimaryConfirmAction => Kind == RuntimeInputEventKind.PrimaryClick;

    /// <summary>
    ///     Creates a keyboard-backed runtime input event.
    /// </summary>
    /// <param name="keyStroke">The queued keyboard item captured from the host.</param>
    /// <returns>The semantic keyboard event.</returns>
    internal static RuntimeInputEvent Keyboard(InputKeyStroke keyStroke)
    {
        return new RuntimeInputEvent(RuntimeInputEventKind.Keyboard, keyStroke);
    }

    /// <summary>
    ///     Gets whether this event is a keyboard event for one specific engine key.
    /// </summary>
    /// <param name="key">The key to compare.</param>
    /// <returns><see langword="true" /> when this event is a keyboard event for <paramref name="key" />.</returns>
    internal bool IsKeyboardKey(InputKey key)
    {
        return Kind == RuntimeInputEventKind.Keyboard && KeyStroke.Key == key;
    }

    /// <summary>
    ///     Formats the event for diagnostic snapshots.
    /// </summary>
    /// <returns>A semantic diagnostic label, or <see langword="null" /> when no input is present.</returns>
    internal string? FormatForDiagnostics()
    {
        return Kind switch
        {
            RuntimeInputEventKind.None => null,
            RuntimeInputEventKind.PrimaryClick => "PrimaryClick",
            RuntimeInputEventKind.Keyboard => FormatKeyboardEvent(),
            _ => throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unknown runtime input event kind.")
        };
    }

    private string FormatKeyboardEvent()
    {
        if (KeyStroke.Key != InputKey.None)
        {
            return KeyStroke.Key.ToString();
        }

        if (KeyStroke.Character is { } character)
        {
            return character.ToString();
        }

        return "Keyboard";
    }
}

/// <summary>
///     Semantic runtime input categories forwarded from the shared host input seam.
/// </summary>
internal enum RuntimeInputEventKind : byte
{
    /// <summary>
    ///     No input.
    /// </summary>
    None = 0,

    /// <summary>
    ///     One queued keyboard event.
    /// </summary>
    Keyboard = 1,

    /// <summary>
    ///     One rising primary mouse-button click.
    /// </summary>
    PrimaryClick = 2
}
