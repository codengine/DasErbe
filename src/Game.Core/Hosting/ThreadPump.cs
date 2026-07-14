using Game.Input;
using Game.Shared.Input;
using Game.State;

namespace Game.Hosting;

/// <summary>
///     Coordinates host-delivered input and elapsed time with the dedicated game thread's blocking wait semantics.
/// </summary>
public sealed class ThreadPump : IDisposable
{
    private static readonly TimeSpan NativeFrameDuration = TimeSpan.FromSeconds(1.0 / 70.0);
    private readonly Lock _gate = new();
    private readonly AutoResetEvent _hostPulse = new(false);
    private readonly Queue<InputFrame> _pendingInputs = [];
    private readonly ManualResetEventSlim _runtimeIdle = new(true);
    private readonly CancellationTokenSource _stopSource = new();
    private Action? _framePreparedCallback;
    private TimeSpan _pendingDosTickElapsed;
    private TimeSpan _pendingFrameElapsed;
    private TimeSpan _pendingHostElapsed;

    /// <summary>
    ///     Gets the cancellation token that stops thread-pump waits during host shutdown.
    /// </summary>
    public CancellationToken StopToken => _stopSource.Token;

    /// <summary>
    ///     Releases the wait handles owned by the thread pump.
    /// </summary>
    public void Dispose()
    {
        _stopSource.Dispose();
        _hostPulse.Dispose();
        _runtimeIdle.Dispose();
    }

    /// <summary>
    ///     Registers the callback that is invoked whenever the runtime publishes a newly prepared frame.
    /// </summary>
    /// <param name="framePreparedCallback">Callback that marks a prepared frame available to the host.</param>
    public void ConfigureFramePreparedCallback(Action framePreparedCallback)
    {
        ArgumentNullException.ThrowIfNull(framePreparedCallback);
        lock (_gate)
        {
            _framePreparedCallback = framePreparedCallback;
        }
    }

    /// <summary>
    ///     Publishes one host pacing slice to the background game thread.
    /// </summary>
    /// <param name="input">The latest host input snapshot.</param>
    /// <param name="elapsed">The elapsed host time to accumulate.</param>
    public void SubmitHostSlice(InputFrame input, TimeSpan elapsed)
    {
        if (elapsed < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(elapsed), "Elapsed host time must be non-negative.");
        }

        lock (_gate)
        {
            _pendingInputs.Enqueue(input);
            _pendingHostElapsed += elapsed;
            _pendingFrameElapsed += elapsed;
            _pendingDosTickElapsed += elapsed;
        }

        _runtimeIdle.Reset();
        _hostPulse.Set();
    }

    /// <summary>
    ///     Requests that the background game thread stop waiting for future pacing slices.
    /// </summary>
    public void RequestStop()
    {
        _stopSource.Cancel();
        _runtimeIdle.Set();
        _hostPulse.Set();
    }

    /// <summary>
    ///     Marks the background thread as stopped so waiting hosts do not deadlock on failure or shutdown.
    /// </summary>
    public void NotifyRuntimeLoopStopped()
    {
        _runtimeIdle.Set();
        _hostPulse.Set();
    }

    internal void PublishInitialInput(InputAdapter inputAdapter, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            PublishPendingInput(inputAdapter);
        }
    }

    internal void WaitFrame(ProgramState program,
        InputAdapter inputAdapter,
        Action renderFrame,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(renderFrame);
        PublishPreparedFrame(renderFrame);
        WaitForQuantum(program, inputAdapter, NativeFrameDuration, WaitClock.Frame, cancellationToken);
    }

    private void PublishPreparedFrame(Action renderFrame)
    {
        ArgumentNullException.ThrowIfNull(renderFrame);
        renderFrame();

        Action? framePreparedCallback;
        lock (_gate)
        {
            framePreparedCallback = _framePreparedCallback;
        }

        framePreparedCallback?.Invoke();
    }

    private void WaitForQuantum(ProgramState program,
        InputAdapter inputAdapter,
        TimeSpan quantum,
        WaitClock waitClock,
        CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var quantumConsumed = false;
            lock (_gate)
            {
                if (program.IsPaused)
                {
                    DiscardPendingHostWork();
                }
                else
                {
                    PublishPendingInput(inputAdapter);
                    switch (waitClock)
                    {
                        case WaitClock.Frame when _pendingFrameElapsed >= quantum:
                            _pendingFrameElapsed -= quantum;
                            quantumConsumed = true;
                            break;
                        case WaitClock.DosTick when _pendingDosTickElapsed >= quantum:
                            _pendingDosTickElapsed -= quantum;
                            quantumConsumed = true;
                            break;
                    }
                }
            }

            if (quantumConsumed)
            {
                break;
            }

            _runtimeIdle.Set();
            WaitForHostPulse(cancellationToken);
        }
    }

    private void DiscardPendingHostWork()
    {
        _pendingInputs.Clear();
        _pendingHostElapsed = TimeSpan.Zero;
        _pendingFrameElapsed = TimeSpan.Zero;
        _pendingDosTickElapsed = TimeSpan.Zero;
    }

    private void PublishPendingInput(InputAdapter inputAdapter)
    {
        while (_pendingInputs.TryDequeue(out var pendingInput))
        {
            inputAdapter.CaptureHostInput(pendingInput);
        }
    }

    private void WaitForHostPulse(CancellationToken cancellationToken)
    {
        var completedHandleIndex = WaitHandle.WaitAny([_hostPulse, cancellationToken.WaitHandle]);
        if (completedHandleIndex == 1)
        {
            cancellationToken.ThrowIfCancellationRequested();
        }
    }

    private enum WaitClock : byte
    {
        Frame = 1,
        DosTick = 2
    }
}
