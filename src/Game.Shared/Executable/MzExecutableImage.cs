using System.Buffers.Binary;
using Game.Shared.Resources;

namespace Game.Shared.Executable;

/// <summary>
///     Wraps an MZ executable image and the game-specific DGROUP reads the loader still needs from it.
/// </summary>
public sealed class MzExecutableImage
{
    private const uint LinearLoadBase = 0x10000;

    private const uint DgroupLinearBase = 0x16770;

    private MzExecutableImage(ReadOnlyMemory<byte> image)
    {
        _image = image;
    }

    private readonly ReadOnlyMemory<byte> _image;

    /// <summary>
    ///     Loads the executable image from a resolved resource entry.
    /// </summary>
    /// <param name="entry">Resolved executable resource entry.</param>
    public static MzExecutableImage From(ResourceEntry entry)
    {
        var bytes = entry.ReadAll();
        var header = MzHeader.Parse(bytes);
        var imageLength = Math.Max(0, header.DeclaredFileSize - header.ImageOffset);
        return new MzExecutableImage(bytes.Slice(header.ImageOffset, imageLength));
    }

    private static int LinearToImageOffset(uint linearAddress)
    {
        if (linearAddress < LinearLoadBase)
        {
            throw new ArgumentOutOfRangeException(nameof(linearAddress),
                $"Expected a linear address >= 0x{LinearLoadBase:X}.");
        }

        return checked((int)(linearAddress - LinearLoadBase));
    }

    private ReadOnlyMemory<byte> ReadMemoryByDgroupOffset(ushort dgroupOffset, int length, string context)
    {
        var imageOffset = ResolveDgroupImageOffset(dgroupOffset, length, context);
        return _image.Slice(imageOffset, length);
    }

    /// <summary>
    ///     Returns one inclusive DGROUP-backed byte range without copying.
    /// </summary>
    /// <param name="startDgroupOffset">DGROUP offset of the first byte.</param>
    /// <param name="endDgroupOffset">DGROUP offset of the last byte.</param>
    /// <param name="context">Used in bounds-check failures.</param>
    public ReadOnlyMemory<byte> ReadMemoryByDgroupRange(ushort startDgroupOffset, ushort endDgroupOffset, string context)
    {
        var length = checked(endDgroupOffset - startDgroupOffset + 1);
        return ReadMemoryByDgroupOffset(startDgroupOffset, length, context);
    }

    /// <summary>
    ///     Copies one DGROUP-backed byte range into a new array.
    /// </summary>
    /// <param name="dgroupOffset">DGROUP offset of the first byte.</param>
    /// <param name="length">Number of bytes to read.</param>
    /// <param name="context">Used in bounds-check failures.</param>
    public byte[] ReadBytesByDgroupOffset(ushort dgroupOffset, int length, string context)
    {
        return ReadMemoryByDgroupOffset(dgroupOffset, length, context).ToArray();
    }

    /// <summary>
    ///     Reads a little-endian word table from DGROUP.
    /// </summary>
    /// <param name="dgroupOffset">DGROUP offset of the first table word.</param>
    /// <param name="count">Number of words to read.</param>
    /// <param name="context">Used in bounds-check failures.</param>
    public ushort[] ReadWordTable(ushort dgroupOffset, int count, string context)
    {
        var bytes = ReadMemoryByDgroupOffset(dgroupOffset, count * sizeof(ushort), context).Span;
        var table = new ushort[count];
        for (var index = 0; index < table.Length; index++)
        {
            table[index] =
                BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(index * sizeof(ushort), sizeof(ushort)));
        }

        return table;
    }

    private int ResolveDgroupImageOffset(ushort dgroupOffset, int length, string context)
    {
        var linearAddress = DgroupLinearBase + dgroupOffset;
        var imageOffset = LinearToImageOffset(linearAddress);
        if (imageOffset + length > _image.Length)
        {
            throw new InvalidOperationException(
                $"{context} at DGROUP offset 0x{dgroupOffset:X4} exceeds the primary executable image.");
        }

        return imageOffset;
    }
}
