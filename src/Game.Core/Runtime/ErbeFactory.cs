using Game.Shared.Host.Input;
using Game.Shared.Resources.Management;

namespace Game.Runtime;

/// <summary>
///     Creates game runtime instances through one controlled composition seam.
/// </summary>
internal static class ErbeFactory
{
    /// <summary>
    ///     Creates one game runtime instance with its boot data loaded once.
    /// </summary>
    /// <param name="resources">Resource manager used to resolve files and executable images.</param>
    /// <param name="exePath">Canonical path of the EXE used for boot-data loading.</param>
    /// <param name="inputBackend">Host input backend.</param>
    /// <param name="language">Optional language overlay identifier.</param>
    /// <param name="useClassicInteractions">
    ///     True to preserve the original double-confirmation interaction flow; false to use the modernized flow.
    /// </param>
    /// <param name="dksnMode">True to enable the DKSN missing-hotspot fallback mode.</param>
    internal static Erbe Create(GameResourceManager resources,
        string exePath,
        IInputBackend inputBackend,
        string? language,
        bool useClassicInteractions = false,
        bool dksnMode = false)
    {
        return new Erbe(resources,
            exePath,
            inputBackend,
            language,
            useClassicInteractions,
            dksnMode);
    }
}
