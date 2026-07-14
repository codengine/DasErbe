using Game.Shared.Text;

namespace Game.Shared.Assets;

/// <summary>
///     Enum-keyed string table with cached null-terminated CP437 projections.
/// </summary>
/// <typeparam name="TKey">Enum key type used to address strings.</typeparam>
public sealed class StringTable<TKey> where TKey : struct, Enum
{
    private readonly Dictionary<TKey, byte[]> _cp437Entries = [];

    /// <summary>
    ///     Creates a string table from the supplied entries.
    /// </summary>
    /// <param name="entries">The key/value entries to populate the table.</param>
    public StringTable(IEnumerable<KeyValuePair<TKey, string>> entries)
    {
        foreach (var (key, value) in entries)
        {
            var encoded = Cp437.Encode(value);
            var bytes = new byte[encoded.Length + 1];
            encoded.AsSpan().CopyTo(bytes);
            _cp437Entries.Add(key, bytes);
        }
    }

    /// <summary>
    ///     Gets the stored string encoded as null-terminated CP437 bytes.
    /// </summary>
    /// <param name="key">The string key.</param>
    /// <returns>The null-terminated CP437 byte stream.</returns>
    /// <exception cref="KeyNotFoundException">The key does not exist.</exception>
    public ReadOnlySpan<byte> GetCp437String(TKey key)
    {
        return _cp437Entries[key];
    }
}
