using Game.Shared.Diagnostics;

namespace Game.Desktop;

/// <summary>
///     Resolves logging options for the desktop host.
/// </summary>
internal static class DesktopLogOptionsResolver
{
    /// <summary>
    ///     Builds logging options from the parsed launch settings.
    /// </summary>
    /// <param name="options">Parsed launch options.</param>
    /// <param name="logOptions">Resolved logging options on success.</param>
    /// <param name="errorMessage">Validation error when resolution fails.</param>
    /// <returns><see langword="true" /> when the supplied settings were valid.</returns>
    internal static bool TryCreate(GameHostLaunchOptions options,
        out GameLogOptions logOptions,
        out string? errorMessage)
    {
        var defaults = GameLogOptions.FromEnvironment();
        var minimumLevel = defaults.MinimumLevel;
        var channelSelection = defaults.ChannelSelection;
        var consoleEnabled = defaults.ConsoleEnabled;

        if (!string.IsNullOrWhiteSpace(options.LogLevel) &&
            !GameLogOptions.TryParseMinimumLevel(options.LogLevel, out minimumLevel))
        {
            logOptions = defaults;
            errorMessage =
                $"Invalid --log-level '{options.LogLevel}'. Expected one of: trace, debug, info, warn, error.";
            return false;
        }

        foreach (var muteChannels in options.MuteLogChannels)
        {
            if (channelSelection.TryMuteCsv(muteChannels, out channelSelection, out errorMessage))
            {
                continue;
            }

            logOptions = defaults;
            errorMessage = $"Invalid --mute-log-channels value. {errorMessage}";
            return false;
        }

        foreach (var unmuteChannels in options.UnmuteLogChannels)
        {
            if (channelSelection.TryUnmuteCsv(unmuteChannels, out channelSelection, out errorMessage))
            {
                continue;
            }

            logOptions = defaults;
            errorMessage = $"Invalid --unmute-log-channels value. {errorMessage}";
            return false;
        }

        if (options.NoConsoleLog)
        {
            consoleEnabled = false;
        }

        var filePath = !string.IsNullOrWhiteSpace(options.LogFile) ? options.LogFile.Trim() : Path.Combine(Environment.CurrentDirectory, "logs", "game.log");

        logOptions = new GameLogOptions(minimumLevel, channelSelection, consoleEnabled, filePath);
        errorMessage = null;
        return true;
    }
}
