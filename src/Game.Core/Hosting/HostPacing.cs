using Game.Input;
using Game.State;

namespace Game.Hosting;

/// <summary>
///     Exposes the host-owned pacing surface to blocking gameplay loops for one live game runtime instance.
/// </summary>
/// <param name="program">Live program state.</param>
/// <param name="inputAdapter">Game-owned input projection seam.</param>
internal sealed class HostPacing(ProgramState program, InputAdapter inputAdapter)
{
    private Action? _renderFrame;
    private CancellationToken _stopToken;
    private ThreadPump? _threadPump;

    /// <summary>
    ///     Attaches the active host thread pump for one background game loop.
    /// </summary>
    /// <param name="threadPump">Host-owned thread pump that publishes frame and tick availability.</param>
    /// <param name="renderFrame">Frame composition callback used before visible frame waits.</param>
    /// <param name="stopToken">Cancellation token that stops blocking waits during host shutdown.</param>
    internal void Attach(ThreadPump threadPump, Action renderFrame, CancellationToken stopToken)
    {
        ArgumentNullException.ThrowIfNull(threadPump);
        ArgumentNullException.ThrowIfNull(renderFrame);
        _threadPump = threadPump;
        _renderFrame = renderFrame;
        _stopToken = stopToken;
    }

    /// <summary>
    ///     Detaches the active host pacing session after the background game loop stops.
    /// </summary>
    internal void Detach()
    {
        _threadPump = null;
        _renderFrame = null;
        _stopToken = CancellationToken.None;
    }

    /// <summary>
    ///     Publishes the latest host input snapshot before the first blocking wait.
    /// </summary>
    internal void PublishInitialInput()
    {
        _threadPump?.PublishInitialInput(inputAdapter, _stopToken);
    }

    /// <summary>
    ///     Publishes the current frame and blocks until one native frame quantum is available.
    /// </summary>
    internal void WaitFrame()
    {
        _threadPump?.WaitFrame(program,
            inputAdapter,
            _renderFrame ?? throw new InvalidOperationException("Host pacing is missing its frame composition callback."),
            _stopToken);
    }

    /// <summary>
    ///     Publishes and waits for the specified number of native frame quanta in sequence.
    /// </summary>
    /// <param name="frameCount">Number of native frame waits to execute.</param>
    internal void WaitFrames(int frameCount)
    {
        for (var index = 0; index < frameCount; index++)
        {
            WaitFrame();
        }
    }
}
