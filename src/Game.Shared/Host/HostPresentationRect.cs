namespace Game.Shared.Host;

/// <summary>
///     Active game-content rectangle in host pixels.
/// </summary>
/// <param name="ContentX">Left edge in host pixels.</param>
/// <param name="ContentY">Top edge in host pixels.</param>
/// <param name="ContentWidth">Width in pixels.</param>
/// <param name="ContentHeight">Height in pixels.</param>
public readonly record struct HostPresentationRect(
    int ContentX,
    int ContentY,
    int ContentWidth,
    int ContentHeight)
{
    /// <summary>
    ///     Minimal non-zero rect rooted at the origin.
    /// </summary>
    public static HostPresentationRect Empty { get; } = new(0, 0, 1, 1);
}
