using Game.DataBlock.Interaction;
using Game.Runtime;
using Game.State;
using Game.Text;

namespace Game.Catalogs;

/// <summary>
///     Reads and mutates runtime-owned interaction descriptors through runtime-owned override layers where needed.
/// </summary>
/// <param name="runtime">Runtime that owns boot data and session state.</param>
internal sealed class InteractionCatalog(Erbe runtime)
{
    private const int DescriptorSize = 0x20;
    private const int HandlerRecordSize = 0x10;
    private const ushort InteractiveRegionTableOffset = 0x0AFA;
    private const ushort TransitionSelectionDescriptorTableOffset = 0x0CBA;

    private const ushort TransitionSelectionOwnerIndexBase =
        (TransitionSelectionDescriptorTableOffset - InteractiveRegionTableOffset) / DescriptorSize;

    private readonly Dictionary<HandlerOverrideKey, InteractionHandlerId> _handlerOverrides = new();

    /// <summary>
    ///     Reads one interaction descriptor as an interactive selection descriptor.
    /// </summary>
    /// <param name="descriptorRef">Descriptor reference to read.</param>
    /// <returns>The resolved interactive selection descriptor.</returns>
    internal InteractiveSelectionDescriptor ReadInteractiveSelectionDescriptor(InteractionDescriptorRef descriptorRef)
    {
        if (descriptorRef.IsEmpty)
        {
            throw new InvalidOperationException("A non-empty interaction descriptor reference is required.");
        }

        var record = ReadDescriptorRecord(descriptorRef);
        var view = record.AsInteractiveSelectionView();
        var selectionStateId = ResolveSelectionStateId(view.SelectionStateId);

        return new InteractiveSelectionDescriptor(view.SelectionEntryIndex,
            selectionStateId,
            runtime.Strings.ResolveSourceOffset(view.SelectionTextSourceOffset),
            view.NextProgramStateId,
            view.ResultBackdropColumn,
            view.ResultBackdropThresholdRow,
            view.ResultBackdropSelectionRow,
            view.AnimationTargetBackdropColumn,
            view.AnimationTargetBackdropThresholdRow,
            view.AnimationTargetBackdropSelectionRow,
            view.WidthPixels,
            view.HeightRows,
            view.LeftColumn,
            view.TopRow);
    }

    /// <summary>
    ///     Reads the managed handler id for one selection callback slot.
    /// </summary>
    /// <param name="interactionSlotIndex">Selected interaction index.</param>
    /// <param name="handlerByteOffset">Handler-table byte offset within the selection handler record.</param>
    /// <returns>The resolved interaction handler id.</returns>
    internal InteractionHandlerId ReadHandlerId(ushort interactionSlotIndex, int handlerByteOffset)
    {
        if (_handlerOverrides.TryGetValue(new HandlerOverrideKey(interactionSlotIndex, handlerByteOffset),
                out var overrideHandlerId))
        {
            // Original handler-table writes target the non-saved callback table at 0x18840. Managed runtime
            // mutations therefore live in runtime-owned overrides instead of patching the boot snapshot.
            return overrideHandlerId;
        }

        var handlerWordIndex = interactionSlotIndex * HandlerRecordSize / sizeof(ushort) +
                               handlerByteOffset / sizeof(ushort);
        if ((uint)handlerWordIndex >= runtime.BootData.InteractionHandlerCount)
        {
            throw new InvalidOperationException(
                $"Interaction handler entry {interactionSlotIndex}:0x{handlerByteOffset:X2} exceeds the boot handler table.");
        }

        return runtime.BootData.ReadInteractionHandlerId(handlerWordIndex);
    }

    /// <summary>
    ///     Overrides one selection callback slot for the current runtime session.
    /// </summary>
    /// <param name="interactionSlotIndex">Selected interaction index.</param>
    /// <param name="handlerByteOffset">Handler-table byte offset within the selection handler record.</param>
    /// <param name="handlerId">Replacement handler id.</param>
    internal void SetHandlerId(ushort interactionSlotIndex, int handlerByteOffset, InteractionHandlerId handlerId)
    {
        // Original callers can clear or replace callback slots in the non-saved handler table at runtime. The
        // managed runtime preserves that contract through a runtime-owned override layer because the boot bytes are not
        // part of the persisted data files.
        _handlerOverrides[new HandlerOverrideKey(interactionSlotIndex, handlerByteOffset)] = handlerId;
    }

    /// <summary>
    ///     Updates a descriptor selection state in the active session owner.
    /// </summary>
    /// <param name="descriptorRef">Descriptor reference to update.</param>
    /// <param name="selectionStateId">Selection state id to publish.</param>
    internal void SetSelectionState(InteractionDescriptorRef descriptorRef, ushort selectionStateId)
    {
        if (descriptorRef.IsEmpty)
        {
            throw new InvalidOperationException("A non-empty interaction descriptor reference is required.");
        }

        EnsureDataBlockInitialized("Interaction descriptor state mutations");

        // IDA 0x15026..0x1504A and 0x151DF..0x1520F: descriptor selection-state mutations target the persistent
        // gameplay descriptor tables. Once the runtime-owned data block is seeded, the authoritative owner is the
        // in-memory interaction-descriptor record, not a raw data-block byte slice.
        runtime.State.RawDataBlock.InteractionDescriptors[ResolveOwnerIndex(descriptorRef)].StateWord =
            selectionStateId;
    }

    /// <summary>
    ///     Updates the selection text for one interactive descriptor in the active session owner.
    /// </summary>
    /// <param name="descriptorRef">Descriptor reference to update.</param>
    /// <param name="stringId">Replacement selection text id.</param>
    internal void SetInteractiveSelectionText(InteractionDescriptorRef descriptorRef, StringId stringId)
    {
        if (descriptorRef.IsEmpty)
        {
            throw new InvalidOperationException("A non-empty interaction descriptor reference is required.");
        }

        EnsureDataBlockInitialized("Interaction descriptor text mutations");

        runtime.State.RawDataBlock.InteractionDescriptors[ResolveOwnerIndex(descriptorRef)].SelectionTextSourceOffset =
            runtime.Strings.GetSourceOffset(stringId);
    }

    /// <summary>
    ///     Resolves the effective selection state id from the initialized data-block owner.
    /// </summary>
    /// <param name="rawSelectionStateId">Selection state id read from the descriptor record.</param>
    /// <returns>The effective selection state id.</returns>
    internal ushort ResolveSelectionStateId(ushort rawSelectionStateId)
    {
        EnsureDataBlockInitialized("Interaction descriptor state reads");
        return rawSelectionStateId;
    }

    private InteractionDescriptorRecord ReadDescriptorRecord(InteractionDescriptorRef descriptorRef)
    {
        EnsureDataBlockInitialized("Interaction descriptor reads");
        return runtime.State.RawDataBlock.InteractionDescriptors[ResolveOwnerIndex(descriptorRef)];
    }

    private static ushort ResolveOwnerIndex(InteractionDescriptorRef descriptorRef)
    {
        return descriptorRef.Table == InteractionDescriptorTable.TransitionSelection
            ? checked((ushort)(TransitionSelectionOwnerIndexBase + descriptorRef.Index))
            : descriptorRef.Index;
    }

    private void EnsureDataBlockInitialized(string context)
    {
        if (!runtime.State.IsInitialized)
        {
            throw new InvalidOperationException($"{context} require an initialized data block.");
        }
    }

    private readonly record struct HandlerOverrideKey(ushort SelectedInteractionIndex, int HandlerByteOffset);
}
