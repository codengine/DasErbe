using Serilog;
using Serilog.Events;

namespace Game.Shared.Diagnostics;

/// <summary>
///     Builds the Serilog logger used by <see cref="GameLog" />.
/// </summary>
public static class SerilogGameLogFactory
{
    private const string OutputTemplate =
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{Channel}] {Message:lj}{NewLine}{Exception}";

    /// <summary>
    ///     Creates a logger from resolved options. Adds the configured console and file sinks, and
    ///     creates the file directory when needed.
    /// </summary>
    /// <param name="applicationName">The <c>Application</c> log property value.</param>
    /// <param name="options">The resolved logging settings.</param>
    /// <param name="configure">Optional hook for extra sinks or enrichers before the logger is created.</param>
    /// <returns>The configured logger.</returns>
    /// <exception cref="ArgumentException"><paramref name="applicationName" /> is <see langword="null" /> or whitespace.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="options" /> is <see langword="null" />.</exception>
    public static ILogger CreateLogger(string applicationName,
        GameLogOptions options,
        Action<LoggerConfiguration>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationName);
        ArgumentNullException.ThrowIfNull(options);

        var configuration = new LoggerConfiguration().MinimumLevel.Is(MapLevel(options.MinimumLevel)).Enrich
            .WithProperty("Application", applicationName).Enrich.WithProperty("Channel", nameof(LoggingChannel.Logging))
            .Filter.ByExcluding(logEvent => !IsChannelEnabled(logEvent, options.ChannelSelection));

        if (options.ConsoleEnabled)
        {
            configuration = configuration.WriteTo.Console(outputTemplate: OutputTemplate);
        }

        if (!string.IsNullOrWhiteSpace(options.FilePath))
        {
            var filePath = options.FilePath!.Trim();
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            configuration = configuration.WriteTo.File(filePath, outputTemplate: OutputTemplate, shared: true);
        }

        configure?.Invoke(configuration);
        return configuration.CreateLogger();
    }

    /// <summary>
    ///     Maps a <see cref="GameLogLevel" /> to the matching Serilog level.
    /// </summary>
    /// <param name="level">The level to map.</param>
    internal static LogEventLevel MapLevel(GameLogLevel level)
    {
        return level switch
        {
            GameLogLevel.Trace => LogEventLevel.Verbose,
            GameLogLevel.Debug => LogEventLevel.Debug,
            GameLogLevel.Information => LogEventLevel.Information,
            GameLogLevel.Warning => LogEventLevel.Warning,
            GameLogLevel.Error => LogEventLevel.Error,
            _ => LogEventLevel.Information
        };
    }

    private static bool IsChannelEnabled(LogEvent logEvent, ChannelSelection selection)
    {
        if (!logEvent.Properties.TryGetValue("Channel", out var rawValue))
        {
            return true;
        }

        if (rawValue is ScalarValue { Value: string channelName } &&
            Enum.TryParse<LoggingChannel>(channelName, true, out var channel))
        {
            return selection.IsEnabled(channel);
        }

        return true;
    }
}
