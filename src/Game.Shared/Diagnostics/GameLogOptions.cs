namespace Game.Shared.Diagnostics;

/// <summary>
///     Logging settings for the current process.
/// </summary>
/// <param name="minimumLevel">The lowest log level to write.</param>
/// <param name="channelSelection">The enabled channels.</param>
/// <param name="consoleEnabled">Whether console logging is enabled.</param>
/// <param name="filePath">Optional log file path.</param>
public sealed class GameLogOptions(
    GameLogLevel minimumLevel,
    ChannelSelection channelSelection,
    bool consoleEnabled = true,
    string? filePath = null)
{
    private const string MinimumLevelEnvironmentVariable = "LOG_MIN_LEVEL";
    private const string ChannelSelectionEnvironmentVariable = "LOG_CHANNELS";
    private const string FilePathEnvironmentVariable = "LOG_FILE";
    private const string ConsoleEnvironmentVariable = "LOG_CONSOLE";

    /// <summary>
    ///     Lowest level that will be written.
    /// </summary>
    public GameLogLevel MinimumLevel { get; } = minimumLevel;

    /// <summary>
    ///     Enabled channels.
    /// </summary>
    public ChannelSelection ChannelSelection { get; } = channelSelection;

    /// <summary>
    ///     Whether console logging is enabled.
    /// </summary>
    public bool ConsoleEnabled { get; } = consoleEnabled;

    /// <summary>
    ///     Optional log file path.
    /// </summary>
    public string? FilePath { get; } = string.IsNullOrWhiteSpace(filePath) ? null : filePath.Trim();

    /// <summary>
    ///     Reads the settings from the process environment. Uses <c>LOG_MIN_LEVEL</c>,
    ///     <c>LOG_CHANNELS</c>, <c>LOG_CONSOLE</c>, and <c>LOG_FILE</c>.
    /// </summary>
    /// <returns>The resolved options.</returns>
    public static GameLogOptions FromEnvironment()
    {
        return new GameLogOptions(ResolveMinimumLevel(),
            ChannelSelection.Parse(Environment.GetEnvironmentVariable(ChannelSelectionEnvironmentVariable)),
            ResolveConsoleEnabled(),
            ResolveFilePath());
    }

    private static GameLogLevel ResolveMinimumLevel()
    {
        var rawValue = Environment.GetEnvironmentVariable(MinimumLevelEnvironmentVariable);
        return TryParseMinimumLevel(rawValue, out var minimumLevel) ? minimumLevel : GameLogLevel.Information;
    }

    /// <summary>
    ///     Parses a log level name from configuration text. Accepts <see cref="GameLogLevel" /> names
    ///     plus the aliases <c>verbose</c>, <c>info</c>, and <c>warn</c>. When parsing fails, returns
    ///     <see langword="false" /> and sets <paramref name="minimumLevel" /> to
    ///     <see cref="GameLogLevel.Information" />.
    /// </summary>
    /// <param name="rawValue">The text to parse.</param>
    /// <param name="minimumLevel">The parsed level when parsing succeeds.</param>
    /// <returns><see langword="true" /> if parsing succeeded; otherwise <see langword="false" />.</returns>
    public static bool TryParseMinimumLevel(string? rawValue, out GameLogLevel minimumLevel)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            minimumLevel = GameLogLevel.Information;
            return false;
        }

        switch (rawValue.Trim().ToLowerInvariant())
        {
            case "trace":
            case "verbose":
                minimumLevel = GameLogLevel.Trace;
                return true;
            case "debug":
                minimumLevel = GameLogLevel.Debug;
                return true;
            case "info":
            case "information":
                minimumLevel = GameLogLevel.Information;
                return true;
            case "warn":
            case "warning":
                minimumLevel = GameLogLevel.Warning;
                return true;
            case "error":
                minimumLevel = GameLogLevel.Error;
                return true;
            default:
                minimumLevel = GameLogLevel.Information;
                return false;
        }
    }

    private static bool ResolveConsoleEnabled()
    {
        return !TryParseBoolean(Environment.GetEnvironmentVariable(ConsoleEnvironmentVariable),
            out var consoleEnabled) || consoleEnabled;
    }

    private static string? ResolveFilePath()
    {
        var rawValue = Environment.GetEnvironmentVariable(FilePathEnvironmentVariable);
        return string.IsNullOrWhiteSpace(rawValue) ? null : rawValue.Trim();
    }

    private static bool TryParseBoolean(string? rawValue, out bool value)
    {
        switch (rawValue?.Trim().ToLowerInvariant())
        {
            case "1":
            case "true":
            case "yes":
            case "on":
                value = true;
                return true;
            case "0":
            case "false":
            case "no":
            case "off":
                value = false;
                return true;
            default:
                value = false;
                return false;
        }
    }
}
