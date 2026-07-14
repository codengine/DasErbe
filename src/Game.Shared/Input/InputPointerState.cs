namespace Game.Shared.Input;

/// <summary>
///     Pointer state captured for one frame.
/// </summary>
/// <param name="IsAvailable"><see langword="true" /> when the pointer is inside the active content rect.</param>
/// <param name="X">Current X position in pointer-bounds coordinates.</param>
/// <param name="Y">Current Y position in pointer-bounds coordinates.</param>
/// <param name="BoundsWidth">Pointer-bounds width.</param>
/// <param name="BoundsHeight">Pointer-bounds height.</param>
public readonly record struct InputPointerState(bool IsAvailable, int X, int Y, int BoundsWidth, int BoundsHeight)
{
    /// <summary>
    ///     Empty pointer state with no available pointer.
    /// </summary>
    public static InputPointerState Empty { get; } = new(false, 0, 0, 0, 0);
}
