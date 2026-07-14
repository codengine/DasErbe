using Game.Shared.RE;
using Game.Text;

namespace Game.State;

/// <summary>
///     Session-owned state for carried-over program globals that are consumed by the managed runtime loop.
/// </summary>
internal sealed class ProgramState
{
    /// <summary>
    ///     Gets or sets whether the live session is currently paused.
    /// </summary>
    internal bool IsPaused { get; set; }

    /// <summary>
    ///     Scratch frame buffers owned by the active program session.
    /// </summary>
    internal readonly ScratchBuffers ScratchBuffers = new(
        new byte[RuntimeState.FrameByteCount],
        new byte[RuntimeState.FrameByteCount]);

    /// <summary>
    ///     Queued alternate-ozone-scene selector consumed by the common post-action tail of
    ///     <c>RunInteractiveLoop</c>.
    /// </summary>
    [GlobalSymbol("byte_168F5", 0x168F5)] internal byte OzoneAlternateTransitionQueuedFlag;

    /// <summary>
    ///     Queued ozone-scene selector consumed by the common post-action tail of <c>RunInteractiveLoop</c>.
    /// </summary>
    [GlobalSymbol("byte_168F4", 0x168F4)] internal byte OzoneTransitionQueuedFlag;

    /// <summary>
    ///     Queued text animation consumed by the common post-action tail of <c>RunInteractiveLoop</c>.
    /// </summary>
    /// <remarks>
    ///     Adapted from the original queued transition-text storage at <c>word_16976:word_16978</c>. The managed
    ///     runtime intentionally carries the semantic <see cref="StringId" /> instead of preserving the original
    ///     offset/segment words as session state.
    /// </remarks>
    internal StringId QueuedTransitionText { get; private set; } = StringId.None;

    /// <summary>
    ///     0x140-word table populated by <c>prepareSceneTransition</c> and later consumed by
    ///     <c>CanPlaceBackdropAt</c> as the per-column minimum backdrop threshold row.
    /// </summary>
    [GlobalSymbol("dseg:5A74", 0x1C1E4, GlobalFlags.BufferOwner | GlobalFlags.CanonicalOwner)]
    internal ushort[] BackdropMinimumThresholdRowTable { get; } = new ushort[0x140];

    /// <summary>
    ///     Four signed backdrop-animation deltas consumed for left, right, up, and down motion.
    /// </summary>
    [GlobalSymbol("", 0x1BDB4, GlobalFlags.BufferOwner | GlobalFlags.CanonicalOwner)]
    internal short[] BackdropStepTable { get; } = [-2, 2, -1, 1];

    /// <summary>
    ///     Clears the queued text animation.
    /// </summary>
    internal void ClearQueuedTextAnimation()
    {
        QueueTextAnimation(StringId.None);
    }

    /// <summary>
    ///     Publishes the queued text animation for the common post-action tail.
    /// </summary>
    /// <param name="textId">Runtime string id consumed by the managed runtime loop.</param>
    internal void QueueTextAnimation(StringId textId)
    {
        QueuedTransitionText = textId;
    }
}
