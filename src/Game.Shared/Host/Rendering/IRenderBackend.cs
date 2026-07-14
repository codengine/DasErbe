using Game.Shared.Rendering;

namespace Game.Shared.Host.Rendering;

/// <summary>
///     Presents the composed <see cref="Screen" /> the game hands to the host.
/// </summary>
public interface IRenderBackend : IDisposable
{
    /// <summary>
    ///     Presents one frame.
    /// </summary>
    /// <param name="screen">Screen to present.</param>
    /// <param name="rect">Active content rectangle for the current presentation.</param>
    void Present(Screen screen, HostPresentationRect rect);
}
