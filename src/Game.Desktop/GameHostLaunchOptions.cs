namespace Game.Desktop;

/// <summary>
///     Launch options for the desktop host.
/// </summary>
/// <param name="AssetRoot">Absolute asset-root override.</param>
/// <param name="LogLevel">Minimum log-level override.</param>
/// <param name="MuteLogChannels">Logging channels to mute.</param>
/// <param name="UnmuteLogChannels">Logging channels to unmute.</param>
/// <param name="LogFile">Optional log-file path.</param>
/// <param name="NoConsoleLog">Whether console logging should be disabled.</param>
/// <param name="Language">Optional language overlay identifier.</param>
/// <param name="UseClassicInteractions">Whether the classic double-confirmation interaction flow should be preserved.</param>
/// <param name="DksnMode">Whether the DKSN missing-hotspot fallback mode should be enabled.</param>
/// <param name="IntegerScaling">Whether presentation scaling should be restricted to whole-number factors.</param>
internal sealed record GameHostLaunchOptions(
    string? AssetRoot,
    string? LogLevel,
    IReadOnlyList<string> MuteLogChannels,
    IReadOnlyList<string> UnmuteLogChannels,
    string? LogFile,
    bool NoConsoleLog,
    string? Language,
    bool UseClassicInteractions,
    bool DksnMode,
    bool IntegerScaling);
