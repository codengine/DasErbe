using System.Diagnostics.CodeAnalysis;
using Game.Shared.Executable;

namespace Game.Shared.Resources.Management;

/// <summary>
///     Resolves canonical resource paths from one indexed asset root.
/// </summary>
public sealed class GameResourceManager
{
    private readonly Dictionary<string, string> _physicalPathsByCanonicalPath;

    /// <summary>
    ///     Indexes the supplied asset root for later resource lookups.
    /// </summary>
    /// <param name="assetRootPath">The resolved asset root path.</param>
    public GameResourceManager(string assetRootPath)
    {
        AssetRootPath = Path.GetFullPath(assetRootPath);
        _physicalPathsByCanonicalPath = IndexAssetRoot();
    }

    /// <summary>
    ///     Indexed asset root path.
    /// </summary>
    public string AssetRootPath { get; }

    /// <summary>
    ///     Loads one DOS executable resource as an MZ image.
    /// </summary>
    /// <param name="path">The resource path to load.</param>
    public MzExecutableImage GetExecutableImage(string path)
    {
        var entry = ResolveOrThrow(path);
        return MzExecutableImage.From(entry);
    }

    /// <summary>
    ///     Resolves one canonical resource path or throws when it is missing.
    /// </summary>
    /// <param name="path">The resource path to resolve.</param>
    /// <exception cref="FileNotFoundException">Thrown when the resource cannot be resolved.</exception>
    public ResourceEntry ResolveOrThrow(string path)
    {
        return TryResolve(path, out var entry)
            ? entry
            : throw new FileNotFoundException($"Could not resolve resource path '{path}' under '{AssetRootPath}'.",
                path);
    }

    /// <summary>
    ///     Resolves one canonical resource path.
    /// </summary>
    /// <remarks>
    ///     The lookup goes through the indexed asset-root map, so separator-normalized resource paths resolve
    ///     case-insensitively even on case-sensitive filesystems.
    /// </remarks>
    /// <param name="path">The resource path to resolve.</param>
    /// <param name="entry">The resolved entry when successful.</param>
    public bool TryResolve(string path, [NotNullWhen(true)] out ResourceEntry? entry)
    {
        var canonicalPath = path.Replace('\\', '/');
        if (!_physicalPathsByCanonicalPath.TryGetValue(canonicalPath, out var physicalPath))
        {
            entry = null;
            return false;
        }

        var fileInfo = new FileInfo(physicalPath);
        entry = ResourceEntry.FromFile(fileInfo.FullName, fileInfo.Length);
        return true;
    }

    private Dictionary<string, string> IndexAssetRoot()
    {
        var physicalPathsByCanonicalPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var physicalPath in Directory.EnumerateFiles(AssetRootPath, "*", SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(AssetRootPath, physicalPath).Replace('\\', '/');
            physicalPathsByCanonicalPath[relativePath] = physicalPath;
        }

        return physicalPathsByCanonicalPath;
    }
}
