using Game.Shared.RE;
using Game.Shared.Rendering;

namespace Game.State;

/// <summary>
///     Session-owned software framebuffer state for the carried-over display buffer contract.
/// </summary>
internal sealed class DisplayCompatibilityState
{
    private const int DisplayBufferWidth = RuntimeState.FrameWidth;
    private const int DisplayBufferHeight = RuntimeState.FrameHeight;

    /// <summary>
    ///     Byte stride of one retained display row.
    /// </summary>
    [GlobalSymbol("word_1D0E0", 0x1D0E0)] internal const ushort StrideBytes = RuntimeState.FrameWidth;

    /// <summary>
    ///     Current indexed draw color consumed by the primitive pixel and line helpers.
    /// </summary>
    [GlobalSymbol("byte_1D3F2", 0x1D3F2)] internal byte CurrentDrawColorIndex;

    /// <summary>
    ///     Text ink palette index consumed by the published FONT_VGA text renderer slice.
    /// </summary>
    [GlobalSymbol("byte_1D0EA", 0x1D0EA)] internal byte TextInkColorIndex;

    /// <summary>
    ///     Persistent indexed software framebuffers retained across publication.
    /// </summary>
    internal Surface[] RetainedDisplayBuffers { get; } =
    [
        new(DisplayBufferWidth, DisplayBufferHeight, PixelFormat.Indexed8),
        new(DisplayBufferWidth, DisplayBufferHeight, PixelFormat.Indexed8),
        new(DisplayBufferWidth, DisplayBufferHeight, PixelFormat.Indexed8),
        new(DisplayBufferWidth, DisplayBufferHeight, PixelFormat.Indexed8)
    ];

    /// <summary>
    ///     Index of the software framebuffer currently being prepared by game drawing helpers.
    /// </summary>
    internal int WorkBufferIndex { get; set; } = 1;

    /// <summary>
    ///     Index of the software framebuffer consumed by presentation composition.
    /// </summary>
    internal int PublishedBufferIndex { get; set; }

    /// <summary>
    ///     Index of the software framebuffer used by restore helpers.
    /// </summary>
    internal int SnapshotBufferIndex { get; set; } = 2;

    /// <summary>
    ///     Runtime DAC backing table populated from LBM <c>CMAP</c> chunks.
    /// </summary>
    [GlobalSymbol("byte_1C473", 0x1C473, GlobalFlags.BufferOwner | GlobalFlags.CanonicalOwner)]
    internal byte[] PaletteDacTable { get; } = new byte[0x300];

    /// <summary>
    ///     Initializes the software framebuffer roles for the current session.
    /// </summary>
    /// <param name="publishedBufferIndex">Initial published framebuffer index.</param>
    /// <param name="workBufferIndex">Initial work framebuffer index.</param>
    /// <param name="snapshotBufferIndex">Initial snapshot framebuffer index.</param>
    internal void InitializeBufferRoles(int publishedBufferIndex, int workBufferIndex, int snapshotBufferIndex)
    {
        EnsureValidBufferIndex(publishedBufferIndex, nameof(publishedBufferIndex));
        EnsureValidBufferIndex(workBufferIndex, nameof(workBufferIndex));
        EnsureValidBufferIndex(snapshotBufferIndex, nameof(snapshotBufferIndex));
        PublishedBufferIndex = publishedBufferIndex;
        WorkBufferIndex = workBufferIndex;
        SnapshotBufferIndex = snapshotBufferIndex;
    }

    /// <summary>
    ///     Publishes the prepared software framebuffer and makes the previous published buffer the next work buffer.
    /// </summary>
    internal void SwapWorkAndPublishedBuffers()
    {
        (WorkBufferIndex, PublishedBufferIndex) = (PublishedBufferIndex, WorkBufferIndex);
    }

    /// <summary>
    ///     Gets the software framebuffer currently being prepared by game drawing helpers.
    /// </summary>
    /// <returns>The current work framebuffer.</returns>
    internal Surface GetWorkBuffer()
    {
        return GetDisplayBuffer(WorkBufferIndex, nameof(WorkBufferIndex));
    }

    /// <summary>
    ///     Gets the software framebuffer currently consumed by presentation composition.
    /// </summary>
    /// <returns>The current published framebuffer.</returns>
    internal Surface GetPublishedBuffer()
    {
        return GetDisplayBuffer(PublishedBufferIndex, nameof(PublishedBufferIndex));
    }

    /// <summary>
    ///     Gets the software framebuffer used by restore helpers.
    /// </summary>
    /// <returns>The current snapshot framebuffer.</returns>
    internal Surface GetSnapshotBuffer()
    {
        return GetDisplayBuffer(SnapshotBufferIndex, nameof(SnapshotBufferIndex));
    }

    private Surface GetDisplayBuffer(int bufferIndex, string bufferIndexName)
    {
        EnsureValidBufferIndex(bufferIndex, bufferIndexName);
        return RetainedDisplayBuffers[bufferIndex];
    }

    private void EnsureValidBufferIndex(int bufferIndex, string bufferIndexName)
    {
        if ((uint)bufferIndex >= (uint)RetainedDisplayBuffers.Length)
        {
            throw new InvalidOperationException(
                $"{bufferIndexName} {bufferIndex} does not map to a retained display buffer.");
        }
    }
}
