using Game.Shared.RE;
using Game.State;

namespace Game.Display;

/// <summary>
///     Session-owned software framebuffer publication helper for the carried-over publication seam.
/// </summary>
/// <param name="state">Live runtime state.</param>
internal sealed class DisplayBufferPublisher(RuntimeState state)
{
    /// <summary>
    ///     Publishes the prepared software framebuffer for presentation composition.
    /// </summary>
    [FunctionSymbol("sub_1052D", 0x1052D, FunctionFlags.AdaptedForMonoGame)]
    internal void PublishPreparedBuffer()
    {
        var display = state.Presentation.Display;

        // IDA 0x10533..0x10544: the original exchanged the draw/published start offsets. The managed runtime preserves
        // the observable software-renderer contract by swapping the work and published framebuffer roles.
        display.SwapWorkAndPublishedBuffers();

        // The published framebuffer is the opaque source consumed by the managed composer. Publishing a different
        // framebuffer can change any pixel, independent of the draw operations that prepared that buffer.
        state.Presentation.Screen.InvalidateAll();

        // IDA 0x10557..0x10571: host frame pacing owns hardware presentation timing in the port.
    }
}
