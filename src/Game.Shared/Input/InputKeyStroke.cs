namespace Game.Shared.Input;

/// <summary>
///     One queued keyboard event from the host.
/// </summary>
/// <param name="Key">Explicit non-text key, or <see cref="InputKey.None" /> when this is character-only input.</param>
/// <param name="Character">Translated character when the keystroke carries text or an ASCII control character.</param>
public readonly record struct InputKeyStroke(InputKey Key, char? Character);
