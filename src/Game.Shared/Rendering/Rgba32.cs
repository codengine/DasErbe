using System.Runtime.InteropServices;

namespace Game.Shared.Rendering;

/// <summary>
///     One 32-bit RGBA color.
/// </summary>
/// <remarks>
///     Channel order is R, G, B, A. Sequential layout keeps bulk copies and interop straightforward.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Rgba32 : IEquatable<Rgba32>
{
    /// <summary>
    ///     Creates an RGBA color from channel values.
    /// </summary>
    /// <param name="red">The red channel value.</param>
    /// <param name="green">The green channel value.</param>
    /// <param name="blue">The blue channel value.</param>
    /// <param name="alpha">The alpha channel value. Defaults to 255 (opaque).</param>
    public Rgba32(byte red, byte green, byte blue, byte alpha = byte.MaxValue)
    {
        R = red;
        G = green;
        B = blue;
        A = alpha;
    }

    /// <summary>
    ///     The red channel value.
    /// </summary>
    public readonly byte R;

    /// <summary>
    ///     The green channel value.
    /// </summary>
    public readonly byte G;

    /// <summary>
    ///     The blue channel value.
    /// </summary>
    public readonly byte B;

    /// <summary>
    ///     The alpha channel value.
    /// </summary>
    public readonly byte A;

    /// <inheritdoc />
    public bool Equals(Rgba32 other)
    {
        return R == other.R && G == other.G && B == other.B && A == other.A;
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        return obj is Rgba32 other && Equals(other);
    }

    /// <inheritdoc />
    public override int GetHashCode()
    {
        return HashCode.Combine(R, G, B, A);
    }

    /// <summary>
    ///     Returns whether two colors are equal.
    /// </summary>
    /// <param name="left">The left color.</param>
    /// <param name="right">The right color.</param>
    public static bool operator ==(Rgba32 left, Rgba32 right)
    {
        return left.Equals(right);
    }

    /// <summary>
    ///     Returns whether two colors are not equal.
    /// </summary>
    /// <param name="left">The left color.</param>
    /// <param name="right">The right color.</param>
    public static bool operator !=(Rgba32 left, Rgba32 right)
    {
        return !left.Equals(right);
    }
}
