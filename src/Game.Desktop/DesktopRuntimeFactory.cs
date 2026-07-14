using Game.Desktop.MonoGame;
using Game.Runtime;
using Game.Shared.Diagnostics;
using Game.Shared.Resources.Inventory;
using Game.Shared.Resources.Management;

namespace Game.Desktop;

/// <summary>
///     Creates the runtime used by the desktop host.
/// </summary>
internal static class DesktopRuntimeFactory
{
    private const string AssetRootEnvironmentVariableName = "GAME_ASSET_ROOT";

    /// <summary>
    ///     Tries to create the runtime.
    /// </summary>
    /// <param name="options">Parsed launch options.</param>
    /// <param name="runtime">Created runtime on success.</param>
    /// <param name="errorMessage">Resolution or validation error on failure.</param>
    /// <returns><see langword="true" /> when the game was created successfully.</returns>
    internal static bool TryCreate(GameHostLaunchOptions options, out Erbe runtime, out string? errorMessage)
    {
        if (!AssetRootResolver.TryResolve(options.AssetRoot,
                AssetRootEnvironmentVariableName,
                Path.Combine(Environment.CurrentDirectory, "Game"),
                out var assetRoot,
                out errorMessage))
        {
            GameLog.Error(LoggingChannel.Program, $"Asset root resolution failed. {errorMessage}");
            runtime = null!;
            return false;
        }

        GameLog.Write(LoggingChannel.Program, $"Resolved asset root '{assetRoot}'.");
        var resources = new GameResourceManager(assetRoot);
        try
        {
            GameInstallation.ValidateResources(resources);
        }
        catch (Exception ex) when (ex is FileNotFoundException or InvalidDataException)
        {
            GameLog.Error(LoggingChannel.Program, "Runtime validation failed.", ex);
            runtime = null!;
            errorMessage = ex.Message;
            return false;
        }

        GameLog.Debug(LoggingChannel.Program,
            $"Runtime options language={(options.Language ?? "default")} classicInteractions={options.UseClassicInteractions} dksnMode={options.DksnMode}.");
        var inputBackend = new MonoGameInputBackend();
        runtime = ErbeFactory.Create(resources,
            GameInstallation.ExeName,
            inputBackend,
            options.Language,
            options.UseClassicInteractions,
            options.DksnMode);
        errorMessage = null;
        return true;
    }
}
