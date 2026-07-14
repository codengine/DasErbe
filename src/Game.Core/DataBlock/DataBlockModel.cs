using Game.DataBlock.Control;
using Game.DataBlock.Interaction;
using Game.DataBlock.Scene;
using Game.DataBlock.Selection;
using Game.Shared.RE;

namespace Game.DataBlock;

/// <summary>
///     In-memory representation of the loaded game data block.
/// </summary>
[GlobalSymbol("byte_1700C", 0x1700C, GlobalFlags.BufferOwner | GlobalFlags.CanonicalOwner)]
internal sealed class DataBlockModel
{
    /// <summary>
    ///     Total data-block length in bytes.
    /// </summary>
    internal const int BlockLength = SelectionTableSection.Length + InteractionDescriptorSection.Length +
                                     SceneDescriptorSection.Length + ControlSectionData.Length;

    /// <summary>
    ///     Selection-entry data-block section.
    /// </summary>
    internal SelectionTableSection SelectionTable { get; } = new();

    /// <summary>
    ///     Interaction-descriptor data-block section.
    /// </summary>
    [GlobalSymbol("transitionSelectionDescriptorTable", 0x1742A, GlobalFlags.BufferOwner | GlobalFlags.CanonicalOwner)]
    internal InteractionDescriptorSection InteractionDescriptors { get; } = new();

    /// <summary>
    ///     Scene-descriptor data-block section.
    /// </summary>
    [GlobalSymbol("sceneEntryDescriptorTable", 0x1726A, GlobalFlags.BufferOwner | GlobalFlags.CanonicalOwner)]
    internal SceneDescriptorSection SceneDescriptors { get; } = new();

    /// <summary>
    ///     Control/footer data-block section.
    /// </summary>
    internal ControlSectionData Control { get; } = new();

    internal void Initialize(ReadOnlySpan<byte> source)
    {
        if (source.Length != BlockLength)
        {
            throw new InvalidOperationException($"Data block source must contain exactly 0x{BlockLength:X} bytes.");
        }

        SelectionTable.ReadFromBlock(source);
        InteractionDescriptors.ReadFromBlock(source);
        SceneDescriptors.ReadFromBlock(source);
        Control.ReadFromBlock(source);
    }
}
