namespace Game.Shared.Rendering;

/// <summary>
///     Fixed-size 2D pixel buffer.
/// </summary>
/// <remarks>
///     Pixels are stored in one contiguous row-major byte buffer. <see cref="Stride" /> is the row width in bytes.
/// </remarks>
public sealed class Surface
{
    private readonly byte[] _buffer;

    /// <summary>
    ///     Creates a surface with the specified size and pixel format.
    /// </summary>
    /// <param name="width">The surface width in pixels.</param>
    /// <param name="height">The surface height in pixels.</param>
    /// <param name="format">The pixel format.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="width" /> or <paramref name="height" /> is not positive.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="format" /> is not supported.</exception>
    /// <exception cref="OverflowException">The buffer size calculation overflows.</exception>
    public Surface(int width, int height, PixelFormat format)
    {
        if (width <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(width), "Surface width must be positive.");
        }

        if (height <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(height), "Surface height must be positive.");
        }

        Width = width;
        Height = height;
        Format = format;
        BytesPerPixel = GetBytesPerPixel(format);
        Stride = checked(width * BytesPerPixel);
        _buffer = new byte[checked(Stride * height)];
    }

    /// <summary>
    ///     Bytes per pixel for <see cref="Format" />.
    /// </summary>
    public int BytesPerPixel { get; }

    /// <summary>
    ///     Pixel format.
    /// </summary>
    public PixelFormat Format { get; }

    /// <summary>
    ///     Surface height in pixels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    ///     Bytes per row.
    /// </summary>
    public int Stride { get; }

    /// <summary>
    ///     Surface width in pixels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    ///     Returns the whole pixel buffer as a writable span.
    /// </summary>
    /// <returns>A span containing <c>Stride * Height</c> bytes.</returns>
    public Span<byte> GetPixelSpan()
    {
        return _buffer;
    }

    /// <summary>
    ///     Returns the whole pixel buffer as a read-only span.
    /// </summary>
    /// <returns>A span containing <c>Stride * Height</c> bytes.</returns>
    public ReadOnlySpan<byte> GetReadOnlyPixelSpan()
    {
        return _buffer;
    }

    /// <summary>
    ///     Returns one writable pixel row.
    /// </summary>
    /// <param name="y">The row index.</param>
    /// <returns>A span of length <see cref="Stride" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="y" /> is outside [0, <see cref="Height" />).</exception>
    public Span<byte> GetRowSpan(int y)
    {
        return (uint)y >= Height
            ? throw new ArgumentOutOfRangeException(nameof(y))
            : _buffer.AsSpan(y * Stride, Stride);
    }

    /// <summary>
    ///     Returns one read-only pixel row.
    /// </summary>
    /// <param name="y">The row index.</param>
    /// <returns>A span of length <see cref="Stride" />.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="y" /> is outside [0, <see cref="Height" />).</exception>
    public ReadOnlySpan<byte> GetReadOnlyRowSpan(int y)
    {
        return (uint)y >= Height
            ? throw new ArgumentOutOfRangeException(nameof(y))
            : _buffer.AsSpan(y * Stride, Stride);
    }

    private static int GetBytesPerPixel(PixelFormat format)
    {
        return format switch
        {
            PixelFormat.Indexed8 => 1,
            PixelFormat.Rgba32 => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Unsupported pixel format.")
        };
    }
}
