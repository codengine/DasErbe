namespace Game.Shared.Diagnostics;

/// <summary>
///     Levels understood by <see cref="GameLog" />.
/// </summary>
public enum GameLogLevel
{
    /// <summary>
    ///     Most verbose diagnostics.
    /// </summary>
    Trace = 0,

    /// <summary>
    ///     Debug output.
    /// </summary>
    Debug = 1,

    /// <summary>
    ///     Normal runtime information.
    /// </summary>
    Information = 2,

    /// <summary>
    ///     Warning output.
    /// </summary>
    Warning = 3,

    /// <summary>
    ///     Error output.
    /// </summary>
    Error = 4
}
