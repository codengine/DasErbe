using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Shared.RE;
using Game.State;

namespace Game.Input;

/// <summary>
///     Session-owned interactive-region helpers for executable-backed pointer hit-testing symbols.
/// </summary>
/// <param name="interactions">Interaction catalog used for region descriptors.</param>
/// <param name="state">Live runtime state.</param>
internal sealed class InteractiveRegionController(InteractionCatalog interactions, RuntimeState state)
{
    /// <summary>
    ///     Finds the first selectable panel region currently under the pointer.
    /// </summary>
    /// <param name="firstRegionIndex">First executable interactive-region record to inspect.</param>
    /// <param name="regionCount">Number of region records to scan from the starting index.</param>
    [FunctionSymbol("sub_14A51", 0x14A51)]
    internal int FindFirstSelectablePanelRegionAtPointer(ushort firstRegionIndex, ushort regionCount)
    {
        var input = state.Input;
        var selectedLocalIndex = -1;

        // IDA 0x14A57..0x14AE1: scan executable interactive-region records until the first region under the current
        // pointer survives both the per-region disable states (5/9) and the linked persistent-record mask state 5.
        for (ushort localIndex = 0; localIndex < regionCount && selectedLocalIndex == -1; localIndex++)
        {
            var region = ReadInteractiveRegion(unchecked((ushort)(firstRegionIndex + localIndex)));
            var deltaColumn = input.PointerColumn - region.LeftColumn;
            if (input.PointerColumn < region.LeftColumn || deltaColumn >= region.WidthPixels)
            {
                continue;
            }

            var deltaRow = input.PointerRow - region.TopRow;
            if (input.PointerRow < region.TopRow || deltaRow >= region.HeightRows)
            {
                continue;
            }

            if (region.SelectionStateId is StateId.Disabled or StateId.Hidden)
            {
                continue;
            }

            if (ReadPersistentRecordStateMask(region.SelectionEntryIndex) == StateId.Disabled)
            {
                continue;
            }

            selectedLocalIndex = localIndex;
        }

        return selectedLocalIndex;
    }

    private InteractiveRegionRecord ReadInteractiveRegion(ushort regionIndex)
    {
        var descriptor = interactions.ReadInteractiveSelectionDescriptor(InteractionDescriptorRef.InteractiveRegion(regionIndex));
        return new InteractiveRegionRecord(descriptor.SelectionEntryIndex,
            descriptor.SelectionStateId,
            descriptor.WidthPixels,
            descriptor.HeightRows,
            descriptor.LeftColumn,
            descriptor.TopRow);
    }

    private ushort ReadPersistentRecordStateMask(ushort persistentRecordIndex)
    {
        if (persistentRecordIndex >= SelectionTableSection.Count)
        {
            throw new InvalidOperationException(
                $"Selection entry index 0x{persistentRecordIndex:X4} must fall within 0x0000..0x{SelectionTableSection.Count - 1:X4}.");
        }

        return state.RawDataBlock.SelectionTable[persistentRecordIndex].State;
    }

    private readonly record struct InteractiveRegionRecord(
        ushort SelectionEntryIndex,
        ushort SelectionStateId,
        ushort WidthPixels,
        ushort HeightRows,
        ushort LeftColumn,
        ushort TopRow);
}
