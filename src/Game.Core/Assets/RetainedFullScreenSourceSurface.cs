using Game.Catalogs;
using Game.Shared.RE;
using Game.State;

namespace Game.Assets;

/// <summary>
///     Keeps the shared decoded full-screen source surface used by startup, panel, save, and animation flows.
/// </summary>
/// <param name="contentFileLoader">Content-file loader.</param>
/// <param name="lbmDecoder">FORM/PBM decoder.</param>
internal sealed class RetainedFullScreenSourceSurface(ContentFileLoader contentFileLoader, LbmDecoder lbmDecoder)
{
    /// <summary>
    ///     Shared decode surface reused by startup, panel, save, second-scene, and ozone animation flows.
    /// </summary>
    [GlobalSymbol("word_1C466", 0x1C466, GlobalFlags.BufferOwner | GlobalFlags.CanonicalOwner)]
    internal byte[] Buffer { get; } = new byte[RuntimeState.FrameByteCount];

    /// <summary>
    ///     Reloads the retained source surface from one asset.
    /// </summary>
    /// <param name="assetId">Asset to decode into the retained surface.</param>
    internal void Reload(AssetId assetId)
    {
        var bytes = contentFileLoader.LoadOrThrow(assetId);
        lbmDecoder.DecodeIntoBuffer(bytes, Buffer, DisplayCompatibilityState.StrideBytes);
    }
}
