namespace Game.Desktop;

/// <summary>
///     Parses command-line arguments for the desktop host.
/// </summary>
internal static class GameHostCommandLine
{
    /// <summary>
    ///     Tries to parse desktop host launch options.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="options">The parsed launch options.</param>
    /// <returns>True when parsing succeeds.</returns>
    internal static bool TryParse(string[] args, out GameHostLaunchOptions options)
    {
        if (GameHostCommandLineParser.TryParseRunOptions(args, out options, out var errorMessage))
        {
            return true;
        }

        GameHostCommandLineParser.WriteRunHelp(Console.Error, errorMessage);
        return false;
    }
}
