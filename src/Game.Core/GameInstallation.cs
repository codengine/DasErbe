using System.Security.Cryptography;
using Game.Shared.Resources.Management;

namespace Game;

/// <summary>
///     Defines the supported Das Erbe installation contract.
/// </summary>
internal static class GameInstallation
{
    /// <summary>
    ///     Gets the canonical path of the EXE used for boot-data extraction.
    /// </summary>
    internal const string ExeName = "ERBE.EXE";

    private const long ExeSizeBytes = 50096;
    private const string ExeMd5 = "6ceee7395fd9965853e9fcfc0b227480";

    /// <summary>
    ///     Validates that the supported EXE is present under the resolved asset root.
    /// </summary>
    /// <param name="resources">Resolved resource manager.</param>
    internal static void ValidateResources(GameResourceManager resources)
    {
        if (!resources.TryResolve(ExeName, out var exeEntry))
        {
            throw new FileNotFoundException($"Missing required asset '{ExeName}'.",
                Path.Combine(resources.AssetRootPath, ExeName));
        }

        var actualLength = exeEntry.Length ?? exeEntry.ReadAll().Length;
        if (actualLength != ExeSizeBytes)
        {
            throw new InvalidDataException(
                $"Asset '{ExeName}' size mismatch. Expected {ExeSizeBytes} but found {actualLength}.");
        }

        using var stream = exeEntry.OpenRead();
        var actualMd5 = Convert.ToHexString(MD5.HashData(stream)).ToLowerInvariant();
        if (!string.Equals(actualMd5, ExeMd5, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Asset '{ExeName}' hash mismatch. Expected {ExeMd5} but found {actualMd5}.");
        }
    }
}
