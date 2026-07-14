namespace Game.Desktop;

/// <summary>
///     Parses desktop host command-line arguments without runtime reflection.
/// </summary>
internal static class GameHostCommandLineParser
{
    /// <summary>
    ///     Tries to parse desktop host launch options.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="options">The parsed launch options.</param>
    /// <param name="errorMessage">The parse failure message.</param>
    /// <returns>True when parsing succeeds.</returns>
    internal static bool TryParseRunOptions(string[] args, out GameHostLaunchOptions options, out string? errorMessage)
    {
        options = new GameHostLaunchOptions(null, null, [], [], null, false, null, false, false);
        string? assetRoot = null;
        string? logLevel = null;
        string? logFile = null;
        string? language = null;
        var noConsoleLog = false;
        var useClassicInteractions = false;
        var dksnMode = false;
        var muteLogChannels = new List<string>();
        var unmuteLogChannels = new List<string>();

        for (var index = 0; index < args.Length; index++)
        {
            var token = args[index];
            if (TryGetValueOption(token, out var valueOption))
            {
                if (!TryReadValue(args, ref index, token, out var value, out errorMessage))
                {
                    return false;
                }

                switch (valueOption)
                {
                    case ValueOption.AssetRoot:
                        assetRoot = value;
                        break;
                    case ValueOption.LogLevel:
                        logLevel = value;
                        break;
                    case ValueOption.LogFile:
                        logFile = value;
                        break;
                    case ValueOption.Language:
                        language = value;
                        break;
                    case ValueOption.MuteLogChannels:
                        muteLogChannels.Add(value);
                        break;
                    case ValueOption.UnmuteLogChannels:
                        unmuteLogChannels.Add(value);
                        break;
                    default:
                        throw new InvalidOperationException($"Unsupported value option '{valueOption}'.");
                }

                continue;
            }

            switch (token)
            {
                case "--no-console-log":
                    noConsoleLog = true;
                    break;
                case "--use-classic-interactions":
                    useClassicInteractions = true;
                    break;
                case "--dksn-mode":
                    dksnMode = true;
                    break;
                default:
                    errorMessage = token.StartsWith('-')
                        ? $"Unknown option '{token}'."
                        : $"Unexpected argument '{token}'.";
                    return false;
            }
        }

        options = new GameHostLaunchOptions(assetRoot,
            logLevel,
            muteLogChannels,
            unmuteLogChannels,
            logFile,
            noConsoleLog,
            language,
            useClassicInteractions,
            dksnMode);
        errorMessage = null;
        return true;
    }

    /// <summary>
    ///     Writes desktop host command-line help.
    /// </summary>
    /// <param name="writer">The destination writer.</param>
    /// <param name="errorMessage">The optional parse failure message.</param>
    internal static void WriteRunHelp(TextWriter writer, string? errorMessage)
    {
        if (!string.IsNullOrWhiteSpace(errorMessage))
        {
            writer.WriteLine(errorMessage);
            writer.WriteLine();
        }

        writer.WriteLine("Usage: Game.Desktop [options]");
        writer.WriteLine();
        writer.WriteLine("Options:");
        writer.WriteLine("  --asset-root <path>          Absolute path to the original game assets.");
        writer.WriteLine("  --log-level <level>          Minimum log level: trace, debug, info, warn, or error.");
        writer.WriteLine("  --log-file <path>            Write logs to the specified file.");
        writer.WriteLine("  --no-console-log             Disable console log output.");
        writer.WriteLine("  --language <name>            Optional language overlay file name without .txt.");
        writer.WriteLine("  --use-classic-interactions   Preserve the original double-confirmation interaction flow.");
        writer.WriteLine("  --dksn-mode                  Enable the DKSN missing-hotspot fallback mode.");
        writer.WriteLine("  --mute-log-channels <names>  Comma-separated logging channels to mute.");
        writer.WriteLine("  --unmute-log-channels <names> Comma-separated logging channels to unmute.");
    }

    private static bool TryReadValue(string[] args,
        ref int index,
        string optionName,
        out string value,
        out string? errorMessage)
    {
        if (index + 1 >= args.Length)
        {
            value = string.Empty;
            errorMessage = $"{optionName} requires a value.";
            return false;
        }

        value = args[++index];
        errorMessage = null;
        return true;
    }

    private static bool TryGetValueOption(string token, out ValueOption valueOption)
    {
        switch (token)
        {
            case "--asset-root":
                valueOption = ValueOption.AssetRoot;
                return true;
            case "--log-level":
                valueOption = ValueOption.LogLevel;
                return true;
            case "--log-file":
                valueOption = ValueOption.LogFile;
                return true;
            case "--language":
                valueOption = ValueOption.Language;
                return true;
            case "--mute-log-channels":
                valueOption = ValueOption.MuteLogChannels;
                return true;
            case "--unmute-log-channels":
                valueOption = ValueOption.UnmuteLogChannels;
                return true;
            default:
                valueOption = default;
                return false;
        }
    }

    private enum ValueOption
    {
        AssetRoot,
        LogLevel,
        LogFile,
        Language,
        MuteLogChannels,
        UnmuteLogChannels
    }
}
