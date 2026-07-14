using System.Globalization;
using Game.Shared.IO;

namespace Game.Shared.Executable;

/// <summary>
///     Small parsed view of the DOS MZ header.
/// </summary>
/// <param name="BytesInLastPage">Bytes used in the last 512-byte page.</param>
/// <param name="TotalPages">Page count from the DOS header.</param>
/// <param name="HeaderParagraphs">Header size in 16-byte paragraphs.</param>
/// <param name="NewHeaderOffset">Offset of the new-header pointer.</param>
/// <param name="HeaderByteCount">Header size in bytes.</param>
/// <param name="DeclaredFileSize">Declared file size, clamped to the available bytes.</param>
public readonly record struct MzHeader(
    ushort BytesInLastPage,
    ushort TotalPages,
    ushort HeaderParagraphs,
    uint NewHeaderOffset,
    int HeaderByteCount,
    int DeclaredFileSize)
{
    /// <summary>
    ///     Byte offset where the executable image starts.
    /// </summary>
    public int ImageOffset => HeaderByteCount;

    /// <summary>
    ///     Parses the DOS header at the start of an executable file.
    /// </summary>
    /// <param name="bytes">Full executable bytes.</param>
    /// <exception cref="InvalidDataException">The bytes do not start with a usable MZ header.</exception>
    public static MzHeader Parse(ReadOnlyMemory<byte> bytes)
    {
        var reader = new BinarySpanReader(bytes);
        if (reader.Length < 0x40)
        {
            throw new InvalidDataException("DOS header too small for an MZ executable.");
        }

        if (!string.Equals(reader.ReadFixedAscii(2), "MZ", StringComparison.Ordinal))
        {
            throw new InvalidDataException("Expected an MZ executable signature.");
        }

        reader.Seek(0x02);
        var bytesInLastPage = reader.ReadUInt16LE();
        var totalPages = reader.ReadUInt16LE();
        reader.Seek(0x08);
        var headerParagraphs = reader.ReadUInt16LE();
        reader.Seek(0x3C);
        var newHeaderOffset = reader.ReadUInt32LE();
        var headerByteCount = checked(headerParagraphs * 16);
        if (headerByteCount <= 0 || headerByteCount > bytes.Length)
        {
            throw new InvalidDataException(string.Create(CultureInfo.InvariantCulture,
                $"Invalid MZ header size {headerByteCount} for a {bytes.Length}-byte executable."));
        }

        var declaredFileSize = ComputeDeclaredFileSize(bytes.Length, bytesInLastPage, totalPages);
        return new MzHeader(bytesInLastPage,
            totalPages,
            headerParagraphs,
            newHeaderOffset,
            headerByteCount,
            declaredFileSize);
    }

    private static int ComputeDeclaredFileSize(int actualLength, ushort bytesInLastPage, ushort totalPages)
    {
        if (totalPages == 0)
        {
            return actualLength;
        }

        var declaredSize = bytesInLastPage == 0 ? totalPages * 512 : (totalPages - 1) * 512 + bytesInLastPage;
        return Math.Min(actualLength, declaredSize);
    }
}
