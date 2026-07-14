using Game.Catalogs;
using Game.Shared.RE;
using Game.Shared.Resources.Management;

namespace Game.Assets;

/// <summary>
///     Writes exported content files by asset id.
/// </summary>
/// <param name="assets">Canonical asset-path catalog.</param>
/// <param name="resources">Game resource manager.</param>
internal sealed class ContentFilePersistenceController(AssetCatalog assets, GameResourceManager resources)
{
    /// <summary>
    ///     Writes a buffer slice to one content file.
    /// </summary>
    /// <param name="assetId">Asset to write.</param>
    /// <param name="sourceBuffer">Source bytes written to the target file.</param>
    /// <param name="byteCount">Number of bytes to write from the start of <paramref name="sourceBuffer" />.</param>
    /// <returns><c>0</c> on success; <c>-1</c> on failure.</returns>
    [FunctionSymbol("sub_11761", 0x11761)]
    internal int WriteBufferToContentFile(AssetId assetId, byte[] sourceBuffer, uint byteCount)
    {
        var sourceBytes = sourceBuffer.AsSpan(0, checked((int)byteCount));
        if (sourceBytes.IsEmpty)
        {
            return -1;
        }

        var canonicalPath = assets.ResolveOrThrow(assetId);
        var physicalPath = Path.Combine(resources.AssetRootPath, canonicalPath.Replace('/', Path.DirectorySeparatorChar));

        try
        {
            var parentDirectory = Path.GetDirectoryName(physicalPath);
            if (!string.IsNullOrEmpty(parentDirectory))
            {
                Directory.CreateDirectory(parentDirectory);
            }

            File.WriteAllBytes(physicalPath, sourceBytes);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException
                                       or ArgumentException)
        {
            return -1;
        }

        return 0;
    }
}
