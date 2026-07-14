using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Game.Shared.IO;

/// <summary>
///     Reads binary data from an in-memory byte buffer with a movable cursor.
/// </summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public ref struct BinarySpanReader
{
    private readonly ReadOnlyMemory<byte> _bytes;
    private int _position;

    /// <summary>
    ///     Starts a reader over a byte array.
    /// </summary>
    /// <param name="bytes">The buffer to read from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BinarySpanReader(byte[] bytes) : this((ReadOnlyMemory<byte>)bytes)
    {
    }

    /// <summary>
    ///     Starts a reader over a memory buffer.
    /// </summary>
    /// <param name="bytes">The buffer to read from.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public BinarySpanReader(ReadOnlyMemory<byte> bytes)
    {
        _bytes = bytes;
        _position = 0;
    }

    /// <summary>
    ///     Total buffer length in bytes.
    /// </summary>
    public readonly int Length => _bytes.Length;

    /// <summary>
    ///     Bytes left between the current cursor and the end of the buffer.
    /// </summary>
    public readonly int RemainingLength => _bytes.Length - _position;

    /// <summary>
    ///     Creates an independent reader over one absolute slice of this buffer.
    /// </summary>
    /// <param name="offset">Absolute offset of the slice within this buffer.</param>
    /// <param name="length">The number of bytes to include in the sub-reader.</param>
    /// <returns>A reader positioned at the start of the requested slice.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     <paramref name="offset" /> or <paramref name="length" /> is negative.
    /// </exception>
    /// <exception cref="InvalidDataException">
    ///     The requested slice falls outside the underlying buffer.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly BinarySpanReader CreateSubreader(int offset, int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (offset > _bytes.Length - length)
        {
            throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture,
                $"Subreader at offset 0x{offset:X} with length {length} falls outside the {Length}-byte buffer."));
        }

        return new BinarySpanReader(_bytes.Slice(offset, length));
    }

    /// <summary>
    ///     Reads <paramref name="length" /> bytes and returns them as memory.
    /// </summary>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>The requested bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length" /> is negative.</exception>
    /// <exception cref="InvalidDataException">The buffer is truncated and does not contain <paramref name="length" /> bytes.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ReadOnlyMemory<byte> ReadMemory(int length)
    {
        EnsureAvailable(length);
        var position = _position;
        _position += length;
        return _bytes.Slice(position, length);
    }

    /// <summary>
    ///     Reads a fixed-length ASCII string.
    /// </summary>
    /// <param name="length">The number of bytes to read and decode as ASCII.</param>
    /// <returns>The decoded string.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="length" /> is negative.</exception>
    /// <exception cref="InvalidDataException">The buffer is truncated and does not contain <paramref name="length" /> bytes.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public string ReadFixedAscii(int length)
    {
        EnsureAvailable(length);
        var value = Encoding.ASCII.GetString(_bytes.Span.Slice(_position, length));
        _position += length;
        return value;
    }

    /// <summary>
    ///     Reads one big-endian 16-bit unsigned integer.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="InvalidDataException">The buffer is truncated.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16BE()
    {
        EnsureAvailable(sizeof(ushort));
        var value = BinaryPrimitives.ReadUInt16BigEndian(_bytes.Span.Slice(_position, sizeof(ushort)));
        _position += sizeof(ushort);
        return value;
    }

    /// <summary>
    ///     Reads one little-endian 16-bit unsigned integer.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="InvalidDataException">The buffer is truncated.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ushort ReadUInt16LE()
    {
        EnsureAvailable(sizeof(ushort));
        var value = BinaryPrimitives.ReadUInt16LittleEndian(_bytes.Span.Slice(_position, sizeof(ushort)));
        _position += sizeof(ushort);
        return value;
    }

    /// <summary>
    ///     Reads one big-endian 32-bit unsigned integer.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="InvalidDataException">The buffer is truncated.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32BE()
    {
        EnsureAvailable(sizeof(uint));
        var value = BinaryPrimitives.ReadUInt32BigEndian(_bytes.Span.Slice(_position, sizeof(uint)));
        _position += sizeof(uint);
        return value;
    }

    /// <summary>
    ///     Reads one little-endian 32-bit unsigned integer.
    /// </summary>
    /// <returns>The decoded value.</returns>
    /// <exception cref="InvalidDataException">The buffer is truncated.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public uint ReadUInt32LE()
    {
        EnsureAvailable(sizeof(uint));
        var value = BinaryPrimitives.ReadUInt32LittleEndian(_bytes.Span.Slice(_position, sizeof(uint)));
        _position += sizeof(uint);
        return value;
    }

    /// <summary>
    ///     Moves the cursor to an absolute byte offset.
    /// </summary>
    /// <param name="position">New cursor position in bytes from the start of the buffer.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="position" /> is negative.</exception>
    /// <exception cref="InvalidDataException"><paramref name="position" /> is greater than <see cref="Length" />.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Seek(int position)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(position);
        if (position > _bytes.Length)
        {
            throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture,
                $"Seek to offset 0x{position:X} falls outside the {Length}-byte buffer."));
        }

        _position = position;
    }

    /// <summary>
    ///     Advances the cursor by <paramref name="byteCount" /> bytes.
    /// </summary>
    /// <param name="byteCount">The number of bytes to skip.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="byteCount" /> is negative.</exception>
    /// <exception cref="InvalidDataException">
    ///     The buffer is truncated and does not contain <paramref name="byteCount" />
    ///     bytes.
    /// </exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Skip(int byteCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(byteCount);
        EnsureAvailable(byteCount);
        _position += byteCount;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureAvailable(int length)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(length);

        if (_position + length > _bytes.Length)
        {
            throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture,
                $"Buffer is truncated at offset 0x{_position:X}. Needed {length} byte(s), but only {RemainingLength} remain."));
        }
    }
}
