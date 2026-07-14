using Game.Desktop.MonoGame;
using Game.Hosting;
using Game.Input;
using Game.Runtime;
using Game.Shared.Diagnostics;
using Game.Shared.Host;
using Game.Shared.Input;
using Game.Shared.Rendering;
using Microsoft.Xna.Framework;

namespace Game.Desktop;

/// <summary>
///     Runs the desktop host.
/// </summary>
internal sealed class DesktopGameHost : Microsoft.Xna.Framework.Game
{
    private const int MinimumClientWidth = 320;
    private const int MinimumClientHeight = 240;
    private const int PreferredBackBufferWidth = 640;
    private const int PreferredBackBufferHeight = 480;
    private readonly ManualResetEventSlim _bootCompleted = new(false);
    private readonly GraphicsDeviceManager _graphics;
    private readonly HotspotBlinkOverlay _hotspotBlinkOverlay = new();
    private readonly Erbe _runtime;
    private readonly Lock _runtimeGate = new();
    private readonly SelectableHotspotSource _selectableHotspotSource;
    private readonly ThreadPump _threadPump = new();
    private HostPresentationRect _cachedPresentationRect = HostPresentationRect.Empty;
    private Screen? _frontBuffer;
    private Screen? _inFlightBuffer;
    private bool _isFrontBufferFresh;
    private int _lastScreenHeight = -1;
    private int _lastScreenWidth = -1;
    private int _lastViewportHeight = -1;
    private int _lastViewportWidth = -1;
    private MonoGameSongPlayer? _musicPlayer;
    private Screen[]? _presentationBuffers;
    private MonoGameScreenPresenter? _renderBackend;
    private Exception? _runtimeLoopFailure;
    private Thread? _runtimeLoopThread;

    /// <summary>
    ///     Creates the host around an already composed runtime instance.
    /// </summary>
    /// <param name="runtime">Runtime instance driven by the host.</param>
    /// <param name="applicationName">Window title.</param>
    internal DesktopGameHost(Erbe runtime, string applicationName)
    {
        _runtime = runtime;
        _selectableHotspotSource = new SelectableHotspotSource(runtime.Scenes, runtime.State);
        _graphics = new GraphicsDeviceManager(this);
        IsFixedTimeStep = false;
        Content.RootDirectory = "Content";
        _graphics.PreferredBackBufferWidth = PreferredBackBufferWidth;
        _graphics.PreferredBackBufferHeight = PreferredBackBufferHeight;
        _graphics.ApplyChanges();
        Window.AllowUserResizing = true;
        Window.ClientSizeChanged += (_, _) => EnforceMinimumClientSize();
        Window.Title = applicationName;
    }

    protected override void Initialize()
    {
        GameLog.Debug(LoggingChannel.Program, "Initializing host state.");
        _runtime.InputBackend.Reset();
        IsMouseVisible = false;
        base.Initialize();
        GameLog.Debug(LoggingChannel.Program, "Host initialization completed.");
    }

    protected override void LoadContent()
    {
        GameLog.Debug(LoggingChannel.Program, "Loading host services.");
        _renderBackend = new MonoGameScreenPresenter(GraphicsDevice,
            _runtime.State.Presentation.Screen.PresentSurface.Format == PixelFormat.Rgba32);
        _musicPlayer = new MonoGameSongPlayer(Content);
        GameLog.Debug(LoggingChannel.Program, "Host services are ready.");
    }

    protected override void BeginRun()
    {
        GameLog.Write(LoggingChannel.Program, "Starting host bootstrapping.");
        var renderBackend = _renderBackend ?? throw new InvalidOperationException(
            "LoadContent must create the render backend before startup.");
        var musicPlayer = _musicPlayer ?? throw new InvalidOperationException(
            "LoadContent must create the music player before startup.");

        _runtime.AttachRenderBackend(renderBackend);
        _runtime.AttachMusicPlayer(musicPlayer);
        _renderBackend = null;
        _musicPlayer = null;
        GameLog.Debug(LoggingChannel.Program, "Attached host services to the runtime.");
        StartRuntimeLoop(PollInput(GetPresentationRect(_runtime.State.Presentation.Screen)));
        GameLog.Write(LoggingChannel.Program, "Host bootstrapping completed.");
        base.BeginRun();
    }

    protected override void Update(GameTime gameTime)
    {
        ThrowIfRuntimeLoopFailed();
        var presentationRect = GetPresentationRect(GetLayoutScreen());
        var input = PollInput(presentationRect);
        if (_runtime.IsPaused)
        {
            _threadPump.SubmitHostSlice(InputFrame.Empty, TimeSpan.Zero);
            return;
        }

        _threadPump.SubmitHostSlice(input, gameTime.ElapsedGameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);
        if (_runtime.RenderBackend is null)
        {
            return;
        }

        ThrowIfRuntimeLoopFailed();

        Screen? presentationSnapshot;
        lock (_runtimeGate)
        {
            presentationSnapshot = _frontBuffer;
            _inFlightBuffer = presentationSnapshot;
        }

        if (presentationSnapshot is null)
        {
            return;
        }

        var presentationRect = GetPresentationRect(presentationSnapshot);
        _hotspotBlinkOverlay.Apply(presentationSnapshot);
        try
        {
            _runtime.RenderBackend.Present(presentationSnapshot, presentationRect);
        }
        finally
        {
            lock (_runtimeGate)
            {
                if (ReferenceEquals(presentationSnapshot, _frontBuffer) && _isFrontBufferFresh)
                {
                    presentationSnapshot.ClearDirty();
                    _isFrontBufferFresh = false;
                }

                _hotspotBlinkOverlay.Restore(presentationSnapshot);
                if (ReferenceEquals(presentationSnapshot, _inFlightBuffer))
                {
                    _inFlightBuffer = null;
                }
            }
        }

        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            StopRuntimeLoop();
            _hotspotBlinkOverlay.Dispose();
            _threadPump.Dispose();
            _runtime.Music.Dispose();
            _runtime.RenderBackend?.Dispose();
            _musicPlayer?.Dispose();
            _renderBackend?.Dispose();
            _bootCompleted.Dispose();
        }

        base.Dispose(disposing);
    }

    private void EnforceMinimumClientSize()
    {
        var currentWidth = Window.ClientBounds.Width;
        var currentHeight = Window.ClientBounds.Height;
        if (currentWidth >= MinimumClientWidth && currentHeight >= MinimumClientHeight)
        {
            return;
        }

        var clampedSize = new Point(Math.Max(MinimumClientWidth, currentWidth),
            Math.Max(MinimumClientHeight, currentHeight));
        _graphics.PreferredBackBufferWidth = clampedSize.X;
        _graphics.PreferredBackBufferHeight = clampedSize.Y;
        _graphics.ApplyChanges();
        GameLog.Debug(LoggingChannel.Program,
            $"Clamped client size to {clampedSize.X}x{clampedSize.Y}.");
    }

    private InputFrame PollInput(HostPresentationRect rect)
    {
        if (IsActive)
        {
            var inputFrame = _runtime.InputBackend.Poll(rect);
            return _hotspotBlinkOverlay.HandleInput(inputFrame, _runtime, _selectableHotspotSource);
        }

        _runtime.InputBackend.Reset();
        _hotspotBlinkOverlay.ResetInputState();
        return InputFrame.Empty;
    }

    private HostPresentationRect GetPresentationRect(Screen screen)
    {
        var viewport = GraphicsDevice.Viewport;
        if (_lastViewportWidth == viewport.Width && _lastViewportHeight == viewport.Height &&
            _lastScreenWidth == screen.Width && _lastScreenHeight == screen.Height)
        {
            return _cachedPresentationRect;
        }

        _cachedPresentationRect = MonoGameScreenPresenter.ComputePresentationRect(viewport, screen);
        _lastViewportWidth = viewport.Width;
        _lastViewportHeight = viewport.Height;
        _lastScreenWidth = screen.Width;
        _lastScreenHeight = screen.Height;
        return _cachedPresentationRect;
    }

    private void StartRuntimeLoop(InputFrame initialInput)
    {
        lock (_runtimeGate)
        {
            _runtimeLoopFailure = null;
            _presentationBuffers = null;
            _isFrontBufferFresh = false;
            _inFlightBuffer = null;
        }

        _bootCompleted.Reset();
        _threadPump.ConfigureFramePreparedCallback(OnRuntimeFramePrepared);
        _threadPump.SubmitHostSlice(initialInput, TimeSpan.Zero);
        GameLog.Debug(LoggingChannel.Program, "Starting runtime boot thread.");
        _runtimeLoopThread = new Thread(RunRuntimeLoop)
        {
            IsBackground = true,
            Name = "GameRuntimeLoop"
        };
        _runtimeLoopThread.Start();
        _bootCompleted.Wait();
        ThrowIfRuntimeLoopFailed();
    }

    private void RunRuntimeLoop()
    {
        try
        {
            GameLog.Write(LoggingChannel.Program, "Booting game runtime.");
            _runtime.HostPacing.Attach(_threadPump, RenderFrame, _threadPump.StopToken);
            try
            {
                _runtime.HostPacing.PublishInitialInput();
                _runtime.Bootstrap.Run();
            }
            catch (OperationCanceledException) when (_threadPump.StopToken.IsCancellationRequested)
            {
            }
            finally
            {
                _runtime.HostPacing.Detach();
                _threadPump.NotifyRuntimeLoopStopped();
            }
        }
        catch (Exception ex)
        {
            lock (_runtimeGate)
            {
                _runtimeLoopFailure = ex;
            }

            GameLog.Error(LoggingChannel.Program, "Game runtime boot failed.", ex);
            _bootCompleted.Set();
        }
    }

    private void StopRuntimeLoop()
    {
        var runtimeLoopThread = _runtimeLoopThread;
        if (runtimeLoopThread is null)
        {
            return;
        }

        GameLog.Debug(LoggingChannel.Program, "Stopping runtime thread.");
        _threadPump.RequestStop();
        runtimeLoopThread.Join();
        _runtimeLoopThread = null;
        GameLog.Debug(LoggingChannel.Program, "Runtime thread stopped.");
    }

    private void RenderFrame()
    {
        _runtime.ScreenComposer.ComposeFrame();
    }

    private void OnRuntimeFramePrepared()
    {
        lock (_runtimeGate)
        {
            var liveScreen = _runtime.State.Presentation.Screen;
            var snapshotRecreated = EnsurePresentationBuffers(liveScreen);
            var backBuffer = ResolvePresentationWriteBuffer();
            backBuffer.ClearDirty();
            if (snapshotRecreated || liveScreen.DirtyRegions.IsFull)
            {
                liveScreen.PresentSurface.GetReadOnlyPixelSpan().CopyTo(backBuffer.PresentSurface.GetPixelSpan());
                backBuffer.InvalidateAll();
            }
            else
            {
                foreach (var region in liveScreen.DirtyRegions.Regions)
                {
                    Blitter.Copy(liveScreen.PresentSurface, region, backBuffer.PresentSurface, region.X, region.Y);
                    backBuffer.Invalidate(region);
                }
            }

            liveScreen.ClearDirty();
            _frontBuffer = backBuffer;
            _isFrontBufferFresh = true;
        }

        if (!_bootCompleted.IsSet)
        {
            GameLog.Write(LoggingChannel.Program, "Runtime boot published the first presentation frame.");
        }

        _bootCompleted.Set();
    }

    private Screen GetLayoutScreen()
    {
        lock (_runtimeGate)
        {
            return _frontBuffer ?? _runtime.State.Presentation.Screen;
        }
    }

    private bool EnsurePresentationBuffers(Screen liveScreen)
    {
        if (_presentationBuffers is not null && _frontBuffer is not null && _frontBuffer.Width == liveScreen.Width &&
            _frontBuffer.Height == liveScreen.Height)
        {
            return false;
        }

        _presentationBuffers =
        [
            new Screen(liveScreen.Width, liveScreen.Height),
            new Screen(liveScreen.Width, liveScreen.Height),
            new Screen(liveScreen.Width, liveScreen.Height)
        ];
        _frontBuffer = _presentationBuffers[0];
        _inFlightBuffer = null;
        _isFrontBufferFresh = false;
        GameLog.Debug(LoggingChannel.Program,
            $"Recreated presentation buffers for {liveScreen.Width}x{liveScreen.Height}.");
        return true;
    }

    private Screen ResolvePresentationWriteBuffer()
    {
        foreach (var presentationBuffer in _presentationBuffers!)
        {
            if (!ReferenceEquals(presentationBuffer, _frontBuffer) &&
                !ReferenceEquals(presentationBuffer, _inFlightBuffer))
            {
                return presentationBuffer;
            }
        }

        throw new InvalidOperationException("No available presentation buffer remained for the runtime handoff.");
    }

    private void ThrowIfRuntimeLoopFailed()
    {
        if (_runtimeLoopFailure is not null)
        {
            throw new InvalidOperationException("The background runtime loop failed.", _runtimeLoopFailure);
        }
    }
}
