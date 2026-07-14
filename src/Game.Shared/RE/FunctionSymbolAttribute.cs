using System.Diagnostics.CodeAnalysis;

namespace Game.Shared.RE;

/// <summary>
///     Marks a method as the managed counterpart of one original function symbol.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed class FunctionSymbolAttribute : Attribute
{
    /// <summary>
    ///     Stores the imported symbol name, original linear address, and any extra flags.
    /// </summary>
    /// <param name="importedName">
    ///     Imported original symbol name from the disassembly/KB, or an empty string when no imported name exists.
    /// </param>
    /// <param name="linearAddress">Original function linear address.</param>
    /// <param name="flags">Additional function metadata flags.</param>
    public FunctionSymbolAttribute(string importedName, uint linearAddress, FunctionFlags flags = FunctionFlags.None)
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
    ///     Extra function tags.
    /// </summary>
    public FunctionFlags Flags { get; }
}
