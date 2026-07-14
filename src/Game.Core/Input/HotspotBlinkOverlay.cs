using System.Runtime.InteropServices;
using Game.Runtime;
using Game.Shared.Input;
using Game.Shared.Rendering;

namespace Game.Input;

/// <summary>
///     Owns the hotspot blink overlay triggered by the Space key.
/// </summary>
internal sealed class HotspotBlinkOverlay : IDisposable
{
    private static readonly TimeSpan BlinkPhaseDuration = TimeSpan.FromSeconds(0.3);
    private static readonly TimeSpan BlinkAnimationDuration = TimeSpan.FromTicks(BlinkPhaseDuration.Ticks * 6);
    private static readonly Rgba32 MarkerColor = new(byte.MaxValue, byte.MaxValue, byte.MaxValue);

    private static readonly (int DeltaX, int DeltaY)[] MarkerOffsets =
    [
        (0, -2),
        (0, -1),
        (-1, -1),
        (1, -1),
        (-2, 0),
        (-1, 0),
        (0, 0),
        (1, 0),
        (2, 0),
        (-1, 1),
        (1, 1),
        (0, 1),
        (0, 2)
    ];

    private readonly List<MarkerCenter> _markerCenters = [];

    private readonly List<PixelBackup> _pixelBackups = [];
    private readonly List<IntRect> _restoredMarkerBounds = [];
    private readonly HashSet<int> _savedPixelIndices = [];
    private bool _animationActive;
    private long _animationStartTimestamp;
    private Erbe? _runtime;
    private bool _spaceWasPressed;

    /// <summary>
    ///     Releases the active pause state, if any.
    /// </summary>
    public void Dispose()
    {
        StopAnimation();
    }

    /// <summary>
    ///     Updates overlay input state and starts a new blink animation on a Space-key press edge.
    /// </summary>
    /// <param name="input">The polled input for the current frame.</param>
    /// <param name="runtime">The active game runtime instance.</param>
    /// <param name="hotspots">Selectable-hotspot source for the active runtime state.</param>
    /// <returns>The input frame with the overlay hotkey removed from the game-facing input.</returns>
    internal InputFrame HandleInput(InputFrame input, Erbe runtime, SelectableHotspotSource hotspots)
    {
        UpdateAnimationState();
        var shouldConsumeSpace = _animationActive;
        var spacePressed = input.IsKeyPressed(InputKey.Space);
        if (!_spaceWasPressed && spacePressed && !_animationActive)
        {
            shouldConsumeSpace = TryStartAnimation(runtime, hotspots);
        }

        _spaceWasPressed = spacePressed;
        return shouldConsumeSpace ? FilterOverlayHotkey(input) : input;
    }

    /// <summary>
    ///     Clears the edge-detection state for the overlay hotkey without affecting an active animation.
    /// </summary>
    internal void ResetInputState()
    {
        _spaceWasPressed = false;
    }

    /// <summary>
    ///     Applies the visible animation phase to the supplied presentation snapshot.
    /// </summary>
    /// <param name="presentationSnapshot">The current presentation snapshot.</param>
    internal void Apply(Screen presentationSnapshot)
    {
        UpdateAnimationState();
        if (!TryGetCurrentPhaseVisibility(out var isVisible) || !isVisible)
        {
            return;
        }

        if (_pixelBackups.Count != 0)
        {
            Restore(presentationSnapshot);
        }

        foreach (var markerCenter in _markerCenters)
        {
            DrawMarker(presentationSnapshot.PresentSurface, markerCenter.X, markerCenter.Y);
            var markerBounds = GetMarkerBounds(presentationSnapshot, markerCenter.X, markerCenter.Y);
            if (!markerBounds.IsEmpty)
            {
                presentationSnapshot.Invalidate(markerBounds);
                _restoredMarkerBounds.Add(markerBounds);
            }
        }
    }

    /// <summary>
    ///     Restores any pixels overwritten by <see cref="Apply" /> and marks the restored regions dirty.
    /// </summary>
    /// <param name="presentationSnapshot">The current presentation snapshot.</param>
    internal void Restore(Screen presentationSnapshot)
    {
        if (_pixelBackups.Count == 0)
        {
            return;
        }

        foreach (var pixelBackup in _pixelBackups)
        {
            var row = MemoryMarshal.Cast<byte, Rgba32>(presentationSnapshot.PresentSurface.GetRowSpan(pixelBackup.Y));
            row[pixelBackup.X] = pixelBackup.Color;
        }

        foreach (var markerBounds in _restoredMarkerBounds)
        {
            presentationSnapshot.Invalidate(markerBounds);
        }

        _pixelBackups.Clear();
        _restoredMarkerBounds.Clear();
        _savedPixelIndices.Clear();
    }

    private bool TryStartAnimation(Erbe runtime, SelectableHotspotSource hotspots)
    {
        var selectableHotspots = hotspots.CaptureSelectableHotspots();
        if (selectableHotspots.Count == 0)
        {
            return false;
        }

        _markerCenters.Clear();
        foreach (var hotspot in selectableHotspots)
        {
            if (hotspot.IsEmpty)
            {
                continue;
            }

            _markerCenters.Add(new MarkerCenter(hotspot.X + hotspot.Width / 2, hotspot.Y + hotspot.Height / 2));
        }

        if (_markerCenters.Count == 0)
        {
            return false;
        }

        _runtime = runtime;
        runtime.SetPaused(true);
        _animationStartTimestamp = TimeProvider.System.GetTimestamp();
        _animationActive = true;
        return true;
    }

    private void UpdateAnimationState()
    {
        if (_animationActive && !TryGetCurrentPhaseVisibility(out _))
        {
            StopAnimation();
        }
    }

    private bool TryGetCurrentPhaseVisibility(out bool isVisible)
    {
        isVisible = false;
        if (!_animationActive)
        {
            return false;
        }

        var elapsed = TimeProvider.System.GetElapsedTime(_animationStartTimestamp);
        if (elapsed >= BlinkAnimationDuration)
        {
            return false;
        }

        var phaseIndex = (int)(elapsed.Ticks / BlinkPhaseDuration.Ticks);
        isVisible = phaseIndex % 2 == 0;
        return true;
    }

    private void StopAnimation()
    {
        _animationActive = false;
        _markerCenters.Clear();
        _pixelBackups.Clear();
        _restoredMarkerBounds.Clear();
        _savedPixelIndices.Clear();
        _runtime?.SetPaused(false);
        _runtime = null;
    }

    private static InputFrame FilterOverlayHotkey(InputFrame input)
    {
        var filteredQueuedKeyStrokes = input.QueuedKeyStrokes
            .Where(static keyStroke => keyStroke is not { Key: InputKey.Space }).ToArray();
        var filteredPressedKeys = input.PressedKeys.Where(static key => key != InputKey.Space).ToArray();

        if (filteredQueuedKeyStrokes.Length == input.QueuedKeyStrokes.Count &&
            filteredPressedKeys.Length == input.PressedKeys.Count)
        {
            return input;
        }

        return new InputFrame(input.Pointer, input.MouseButtons, filteredQueuedKeyStrokes, filteredPressedKeys);
    }

    private void DrawMarker(Surface surface, int centerX, int centerY)
    {
        foreach (var (deltaX, deltaY) in MarkerOffsets)
        {
            var pixelX = centerX + deltaX;
            var pixelY = centerY + deltaY;
            if ((uint)pixelX >= surface.Width || (uint)pixelY >= surface.Height)
            {
                continue;
            }

            SaveOriginalPixel(surface, pixelX, pixelY);
            var row = MemoryMarshal.Cast<byte, Rgba32>(surface.GetRowSpan(pixelY));
            row[pixelX] = MarkerColor;
        }
    }

    private void SaveOriginalPixel(Surface surface, int x, int y)
    {
        var pixelIndex = y * surface.Width + x;
        if (!_savedPixelIndices.Add(pixelIndex))
        {
            return;
        }

        var row = MemoryMarshal.Cast<byte, Rgba32>(surface.GetRowSpan(y));
        _pixelBackups.Add(new PixelBackup(x, y, row[x]));
    }

    private static IntRect GetMarkerBounds(Screen screen, int centerX, int centerY)
    {
        const int markerRadius = 2;
        var left = Math.Max(0, centerX - markerRadius);
        var top = Math.Max(0, centerY - markerRadius);
        var right = Math.Min(screen.Width, centerX + markerRadius + 1);
        var bottom = Math.Min(screen.Height, centerY + markerRadius + 1);
        return new IntRect(left, top, right - left, bottom - top);
    }

    private readonly record struct MarkerCenter(int X, int Y);

    private readonly record struct PixelBackup(int X, int Y, Rgba32 Color);
}
