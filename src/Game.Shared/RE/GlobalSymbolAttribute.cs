using System.Diagnostics.CodeAnalysis;

namespace Game.Shared.RE;

/// <summary>
///     Marks a field, property, or owner type as the managed counterpart of one original global symbol.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Class)]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed class GlobalSymbolAttribute : Attribute
{
    /// <summary>
    ///     Stores the imported symbol name, original linear address, and any extra flags.
    /// </summary>
    /// <param name="importedName">
    ///     Imported original symbol name from the disassembly/KB, or an empty string when no imported name exists.
    /// </param>
    /// <param name="linearAddress">Original global linear address.</param>
    /// <param name="flags">Additional global metadata flags.</param>
    public GlobalSymbolAttribute(string importedName, uint linearAddress, GlobalFlags flags = GlobalFlags.None)
    {
        ImportedName = importedName;
        LinearAddress = linearAddress;
        Flags = flags;
    }

    /// <summary>
    ///     Imported original symbol name.
    /// </summary>
    public string ImportedName { get; }

    /// <summary>
    ///     Original linear address.
    /// </summary>
    public uint LinearAddress { get; }

    /// <summary>
    ///     Extra global tags.
    /// </summary>
    public GlobalFlags Flags { get; }
}
