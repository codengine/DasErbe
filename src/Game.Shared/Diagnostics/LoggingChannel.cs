namespace Game.Shared.Diagnostics;

/// <summary>
///     Log channels.
/// </summary>
public enum LoggingChannel
{
    /// <summary>
    ///     Logger setup and lifecycle.
    /// </summary>
    Logging = 0,

    /// <summary>
    ///     Startup and host integration.
    /// </summary>
    Program = 1,

    /// <summary>
    ///     Runtime state and execution.
    /// </summary>
    Runtime = 2,

    /// <summary>
    ///     Input processing.
    /// </summary>
    Input = 3,

    /// <summary>
    ///     Resource and file access.
    /// </summary>
    Files = 4
}
