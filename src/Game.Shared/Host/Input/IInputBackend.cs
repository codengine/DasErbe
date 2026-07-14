using Game.Shared.Input;

namespace Game.Shared.Host.Input;

/// <summary>
///     Supplies one raw input frame per host tick.
/// </summary>
public interface IInputBackend
{
    /// <summary>
    ///     Clears any host-side capture state before a new session starts.
    /// </summary>
    void Reset()
    {
    }

    /// <summary>
    ///     Captures the next raw input frame.
    /// </summary>
    /// <param name="rect">Current active content rectangle in host pixels.</param>
    /// <returns>Captured input frame.</returns>
    InputFrame Poll(HostPresentationRect rect);
}
