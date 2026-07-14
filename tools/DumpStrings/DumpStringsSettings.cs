using System.ComponentModel;
using Spectre.Console.Cli;

/// <summary>
///     Command-line settings for the string overlay dump tool.
/// </summary>
internal sealed class DumpStringsSettings : CommandSettings
{
    /// <summary>
    ///     Gets the path to the source executable.
    /// </summary>
    [CommandOption("--exe <PATH>")]
    [Description("Path to the game EXE.")]
    public string ExecutablePath { get; init; } = string.Empty;

    /// <summary>
    ///     Gets the optional output path. Standard output is used when omitted.
    /// </summary>
    [CommandOption("--out <PATH>")]
    [Description("Optional output .txt path. Writes to stdout when omitted.")]
    public string? OutputPath { get; init; }

    /// <inheritdoc />
    public override Spectre.Console.ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(ExecutablePath))
        {
            return Spectre.Console.ValidationResult.Error("--exe is required.");
        }

        if (!File.Exists(ExecutablePath))
        {
            return Spectre.Console.ValidationResult.Error($"Executable does not exist: {ExecutablePath}");
        }

        return Spectre.Console.ValidationResult.Success();
    }
}
