namespace Game.Shared.Input;

/// <summary>
///     Mouse buttons the shared input layer tracks.
/// </summary>
[Flags]
public enum InputMouseButtons : byte
{
    /// <summary>
    ///     No buttons.
    /// </summary>
    None = 0,

    /// <summary>
    ///     Primary mouse button.
    /// </summary>
    Primary = 1 << 0
}
