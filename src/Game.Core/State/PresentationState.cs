using Game.Shared.Palette;
using Game.Shared.Rendering;

namespace Game.State;

internal sealed class PresentationState
{
    internal DisplayCompatibilityState Display { get; } = new();
    internal FontCompatibilityState Font { get; } = new();
    internal TextCompatibilityState Text { get; } = new();
    internal ColorPalette Palette { get; } = new();
    internal Screen Screen { get; } = new(RuntimeState.FrameWidth, RuntimeState.FrameHeight);
}
