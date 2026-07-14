using Game.Catalogs;
using Game.Shared.Resources.Management;

namespace Game.Assets;

/// <summary>
///     Loads content files by asset id.
/// </summary>
/// <param name="assets">Canonical asset-path catalog.</param>
/// <param name="resources">Game resource manager.</param>
internal sealed class ContentFileLoader(AssetCatalog assets, GameResourceManager resources)
{
    /// <summary>
    ///     Loads one content file into a new byte array.
    /// </summary>
    /// <param name="assetId">Asset to load.</param>
    /// <returns>Loaded file bytes.</returns>
    internal byte[] LoadOrThrow(AssetId assetId)
    {
        var canonicalPath = assets.ResolveOrThrow(assetId);
        return resources.ResolveOrThrow(canonicalPath).ReadAll().ToArray();
    }
}
