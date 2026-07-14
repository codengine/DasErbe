using Serilog;

namespace Game.Shared.Diagnostics;

/// <summary>
///     Shared logger entry point.
/// </summary>
public static class GameLog
{
    private static ILogger? _logger;

    /// <summary>
    ///     Sets the shared logger and writes the session start entry once. After the first successful
    ///     call, later calls leave the existing logger in place.
    /// </summary>
    /// <param name="logger">The logger instance to use.</param>
    /// <param name="options">Optional settings to include in the startup entry.</param>
    public static void Initialize(ILogger logger, GameLogOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(logger);

        var existingLogger = Interlocked.CompareExchange(ref _logger, logger, null);
        if (existingLogger is not null)
        {
            if (!ReferenceEquals(existingLogger, logger) && logger is IDisposable disposableLogger)
            {
                disposableLogger.Dispose();
            }

            return;
        }

        var resolvedOptions = options ?? GameLogOptions.FromEnvironment();
        WriteCore(logger,
            GameLogLevel.Information,
            LoggingChannel.Logging,
            $"Session Start minLevel={resolvedOptions.MinimumLevel} channels={resolvedOptions.ChannelSelection.Describe()}");
    }

    /// <summary>
    ///     Writes the session end entry and disposes the shared logger.
    /// </summary>
    public static void Shutdown()
    {
        var logger = Interlocked.Exchange(ref _logger, null);
        if (logger is null)
        {
            return;
        }

        WriteCore(logger, GameLogLevel.Information, LoggingChannel.Logging, "Session End");
        if (logger is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    ///     Logs an informational message.
    /// </summary>
    /// <param name="channel">The log channel.</param>
    /// <param name="message">The log message.</param>
    public static void Write(LoggingChannel channel, string message)
    {
        Log(GameLogLevel.Information, channel, message);
    }

    /// <summary>
    ///     Logs a debug message.
    /// </summary>
    /// <param name="channel">The log channel.</param>
    /// <param name="message">The log message.</param>
    public static void Debug(LoggingChannel channel, string message)
    {
        Log(GameLogLevel.Debug, channel, message);
    }

    /// <summary>
    ///     Logs a warning message.
    /// </summary>
    /// <param name="channel">The log channel.</param>
    /// <param name="message">The log message.</param>
    public static void Warning(LoggingChannel channel, string message)
    {
        Log(GameLogLevel.Warning, channel, message);
    }

    /// <summary>
    ///     Logs an error message and optionally includes exception details.
    /// </summary>
    /// <param name="channel">The log channel.</param>
    /// <param name="message">The log message.</param>
    /// <param name="exception">Optional exception to include.</param>
    public static void Error(LoggingChannel channel, string message, Exception? exception = null)
    {
        Log(GameLogLevel.Error, channel, message, exception);
    }

    private static void Log(GameLogLevel level, LoggingChannel channel, string message, Exception? exception = null)
    {
        var logger = EnsureLogger();
        WriteCore(logger, level, channel, message, exception);
    }

    private static ILogger EnsureLogger()
    {
        var logger = Volatile.Read(ref _logger);
        if (logger is not null)
        {
            return logger;
        }

        var options = GameLogOptions.FromEnvironment();
        var createdLogger = SerilogGameLogFactory.CreateLogger("Engine", options);
        var existingLogger = Interlocked.CompareExchange(ref _logger, createdLogger, null);
        if (existingLogger is not null)
        {
            if (createdLogger is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return existingLogger;
        }

        WriteCore(createdLogger,
            GameLogLevel.Information,
            LoggingChannel.Logging,
            $"Session Start minLevel={options.MinimumLevel} channels={options.ChannelSelection.Describe()}");
        return createdLogger;
    }

    private static void WriteCore(ILogger logger,
        GameLogLevel level,
        LoggingChannel channel,
        string message,
        Exception? exception = null)
    {
        var channelLogger = logger.ForContext("Channel", channel.ToString());
        if (exception is null)
        {
            channelLogger.Write(SerilogGameLogFactory.MapLevel(level), "{Message}", message);
        }
        else
        {
            channelLogger.Write(SerilogGameLogFactory.MapLevel(level), exception, "{Message}", message);
        }
    }
}
