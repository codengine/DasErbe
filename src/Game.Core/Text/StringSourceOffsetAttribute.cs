namespace Game.Text;

/// <summary>
///     Marks a game string id with the original source DGROUP offset used while bootstrapping the string table.
/// </summary>
/// <param name="offset">Original source DGROUP offset.</param>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class StringSourceOffsetAttribute(ushort offset) : Attribute
{
    /// <summary>
    ///     Gets the original source DGROUP offset.
    /// </summary>
    internal ushort Offset { get; } = offset;
}
