namespace Game.Shared.Rendering;

/// <summary>
///     Integer axis-aligned rectangle.
/// </summary>
/// <param name="X">The left coordinate.</param>
/// <param name="Y">The top coordinate.</param>
/// <param name="Width">The rectangle width.</param>
/// <param name="Height">The rectangle height.</param>
public readonly record struct IntRect(int X, int Y, int Width, int Height)
{
    /// <summary>
    ///     Bottom edge (<c>Y + Height</c>).
    /// </summary>
    public int Bottom => Y + Height;

    /// <summary>
    ///     True when width or height is not positive.
    /// </summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>
    ///     Right edge (<c>X + Width</c>).
    /// </summary>
    public int Right => X + Width;
}
