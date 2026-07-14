using Game.Shared.Text;

namespace Game.Text;

/// <summary>
///     Shared CP437 text-encoding helpers for runtime-owned null-terminated text buffers.
/// </summary>
internal static class TextUtils
{
    /// <summary>
    ///     Encodes one managed string as a CP437 byte buffer with a trailing zero terminator.
    /// </summary>
    /// <param name="value">Managed text to encode.</param>
    /// <returns>Caller-owned CP437 byte buffer terminated with a trailing zero byte.</returns>
    internal static byte[] EncodeNullTerminated(string value)
    {
        var encoded = Cp437.Encode(value.Replace("ß", "ss"));
        var terminated = new byte[encoded.Length + 1];
        encoded.CopyTo(terminated, 0);
        return terminated;
    }

    /// <summary>
    ///     Encodes one ordered set of managed strings as null-terminated CP437 byte buffers.
    /// </summary>
    /// <param name="strings">Ordered managed strings to encode.</param>
    /// <returns>Caller-owned null-terminated CP437 byte buffers that preserve the source order.</returns>
    internal static byte[][] EncodeNullTerminated(params string[] strings)
    {
        return strings.Select(EncodeNullTerminated).ToArray();
    }
}
