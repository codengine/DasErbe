namespace Game.Shared.Resources.Inventory;

/// <summary>
///     Resolves the asset root from the configured host inputs.
/// </summary>
public static class AssetRootResolver
{
    /// <summary>
    ///     Resolves the asset root in command-line, environment, then fallback order.
    /// </summary>
    /// <remarks>
    ///     Resolution stops at the first explicit source that is present.
    /// </remarks>
    /// <param name="commandLineAssetRoot">Optional explicit asset root value from the command line.</param>
    /// <param name="assetRootEnvironmentVariable">The environment variable name to consult.</param>
    /// <param name="fallbackAssetRoot">Optional host-profile fallback asset root.</param>
    /// <param name="assetRootPath">Resolved asset root path when successful.</param>
    /// <param name="errorMessage">Failure reason when resolution fails.</param>
    /// <returns>True when an asset root was resolved.</returns>
    public static bool TryResolve(string? commandLineAssetRoot,
        string assetRootEnvironmentVariable,
        string? fallbackAssetRoot,
        out string assetRootPath,
        out string? errorMessage)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(assetRootEnvironmentVariable);

        if (!string.IsNullOrWhiteSpace(commandLineAssetRoot))
        {
            return TryResolveExplicit(commandLineAssetRoot, "--asset-root", out assetRootPath, out errorMessage);
        }

        var environmentAssetRoot = Environment.GetEnvironmentVariable(assetRootEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(environmentAssetRoot))
        {
            return TryResolveExplicit(environmentAssetRoot,
                assetRootEnvironmentVariable,
                out assetRootPath,
                out errorMessage);
        }

        if (!string.IsNullOrWhiteSpace(fallbackAssetRoot))
        {
            if (TryResolveFallback(fallbackAssetRoot, out assetRootPath, out errorMessage))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                return false;
            }
        }

        assetRootPath = string.Empty;
        errorMessage = CreateUnresolvedAssetRootMessage(assetRootEnvironmentVariable, fallbackAssetRoot);
        return false;
    }

    private static string CreateUnresolvedAssetRootMessage(string assetRootEnvironmentVariable, string? fallbackAssetRoot)
    {
        var message = $"Could not resolve asset root. Pass --asset-root <absolute-path> or set {assetRootEnvironmentVariable}.";
        return string.IsNullOrWhiteSpace(fallbackAssetRoot)
            ? message
            : $"{message} Also checked fallback asset root '{fallbackAssetRoot}'.";
    }

    private static bool TryResolveFallback(string fallbackAssetRoot, out string assetRootPath, out string? errorMessage)
    {
        if (!Path.IsPathFullyQualified(fallbackAssetRoot))
        {
            assetRootPath = string.Empty;
            errorMessage = $"Fallback asset root must be an absolute path: '{fallbackAssetRoot}'.";
            return false;
        }

        if (!Directory.Exists(fallbackAssetRoot))
        {
            assetRootPath = string.Empty;
            errorMessage = null;
            return false;
        }

        assetRootPath = Path.GetFullPath(fallbackAssetRoot);
        errorMessage = null;
        return true;
    }

    private static bool TryResolveExplicit(string assetRoot, string sourceName, out string assetRootPath, out string? errorMessage)
    {
        if (!Path.IsPathFullyQualified(assetRoot))
        {
            assetRootPath = string.Empty;
            errorMessage = $"{sourceName} must be an absolute path: '{assetRoot}'.";
            return false;
        }

        var normalizedPath = Path.GetFullPath(assetRoot);
        if (!Directory.Exists(normalizedPath))
        {
            assetRootPath = string.Empty;
            errorMessage = $"{sourceName} does not exist: '{normalizedPath}'.";
            return false;
        }

        assetRootPath = normalizedPath;
        errorMessage = null;
        return true;
    }
}
