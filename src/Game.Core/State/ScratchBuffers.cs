using Game.Shared.RE;

namespace Game.State;

/// <summary>
///     Session-owned scratch buffers reused by scene composition, backdrop drawing, and selection animation replay.
/// </summary>
/// <param name="sceneBuffer">Current-scene scratch buffer owned by the active program session.</param>
/// <param name="backdropBuffer">Backdrop scratch buffer owned by the active program session.</param>
internal sealed class ScratchBuffers(byte[] sceneBuffer, byte[] backdropBuffer)
{
    /// <summary>
    ///     Gets the current-scene scratch buffer consumed by scene composition and selection animations.
    /// </summary>
    [GlobalSymbol("word_1C46A", 0x1C46A, GlobalFlags.BufferOwner | GlobalFlags.CanonicalOwner)]
    internal byte[] Scene { get; } = sceneBuffer;

    /// <summary>
    ///     Gets the backdrop scratch buffer consumed by scaled backdrop draws and hero-portrait loads.
    /// </summary>
    [GlobalSymbol("word_1C46E", 0x1C46E, GlobalFlags.BufferOwner | GlobalFlags.CanonicalOwner)]
    internal byte[] Backdrop { get; } = backdropBuffer;
}
