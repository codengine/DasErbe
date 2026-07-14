using System.Text;

namespace Game.Shared.Text;

/// <summary>
///     Encodes and decodes DOS code page 437 strings.
/// </summary>
public static class Cp437
{
    private static readonly Encoding Cp437Encoding;

    static Cp437()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Cp437Encoding = Encoding.GetEncoding(437);
    }

    /// <summary>
    ///     Decodes CP437 bytes to a string.
    /// </summary>
    /// <param name="bytes">The bytes to decode.</param>
    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        return Cp437Encoding.GetString(bytes);
    }

    /// <summary>
    ///     Encodes a string as CP437 bytes.
    /// </summary>
    /// <param name="value">The text to encode.</param>
    /// <exception cref="ArgumentNullException"><paramref name="value" /> is <see langword="null" />.</exception>
    public static byte[] Encode(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Cp437Encoding.GetBytes(value);
    }
}
