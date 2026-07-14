using System.Buffers.Binary;
using Game.DataBlock.Interaction;
using Game.DataBlock.Scene;
using Game.Runtime;
using Game.Runtime.ProgramScene;
using Game.Text;

namespace Game.Catalogs;

/// <summary>
///     Provides runtime scene descriptors and boot-projected scene lookup tables.
/// </summary>
/// <param name="runtime">Runtime that owns boot data and session state.</param>
internal sealed class ProgramSceneCatalog(Erbe runtime)
{
    /// <summary>
    ///     Decodes a boot-projected RLE word stream into the supplied table.
    /// </summary>
    /// <param name="streamOffset">Original source offset of the RLE stream.</param>
    /// <param name="destinationTable">Destination word table to fill.</param>
    internal void DecodeWordRunsIntoTable(ushort streamOffset, ushort[] destinationTable)
    {
        var bytes = runtime.BootData.GetWordRunStream(streamOffset);
        var sourceOffset = 0;
        var destinationIndex = 0;

        while (destinationIndex < destinationTable.Length)
        {
            if (sourceOffset + 4 > bytes.Length)
            {
                throw new InvalidOperationException(
                    "sub_1455D RLE word stream exceeds the boot-projected stream data.");
            }

            var runLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(sourceOffset, 2));
            var value = BinaryPrimitives.ReadUInt16LittleEndian(bytes.Slice(sourceOffset + 2, 2));
            sourceOffset += 4;

            for (var runIndex = 0; runIndex < runLength; runIndex++)
            {
                if (destinationIndex >= destinationTable.Length)
                {
                    throw new InvalidOperationException(
                        "sub_1455D RLE word stream exceeds the destination table length.");
                }

                destinationTable[destinationIndex++] = value;
            }
        }
    }

    /// <summary>
    ///     Reads one backdrop base-width entry.
    /// </summary>
    /// <param name="descriptorEntryIndex">Backdrop descriptor entry index.</param>
    internal ushort ReadBackdropBaseWidth(ushort descriptorEntryIndex)
    {
        return runtime.BootData.ReadBackdropBaseWidth(descriptorEntryIndex);
    }

    /// <summary>
    ///     Reads one backdrop descriptor-selection entry.
    /// </summary>
    /// <param name="selectionTableIndex">Backdrop selection table index.</param>
    internal ushort ReadBackdropDescriptorSelection(ushort selectionTableIndex)
    {
        return runtime.BootData.ReadBackdropDescriptorSelection(selectionTableIndex);
    }

    /// <summary>
    ///     Reads one backdrop source-column entry.
    /// </summary>
    /// <param name="descriptorEntryIndex">Backdrop descriptor entry index.</param>
    internal ushort ReadBackdropSourceColumn(ushort descriptorEntryIndex)
    {
        return runtime.BootData.ReadBackdropSourceColumn(descriptorEntryIndex);
    }

    /// <summary>
    ///     Reads one backdrop source-row entry.
    /// </summary>
    /// <param name="descriptorEntryIndex">Backdrop descriptor entry index.</param>
    internal ushort ReadBackdropSourceRow(ushort descriptorEntryIndex)
    {
        return runtime.BootData.ReadBackdropSourceRow(descriptorEntryIndex);
    }

    /// <summary>
    ///     Reads one scene-entry descriptor as a managed program scene entry.
    /// </summary>
    /// <param name="entryIndex">Scene-entry descriptor index.</param>
    internal ProgramSceneEntry ReadProgramSceneEntry(ushort entryIndex)
    {
        var sceneDescriptor = ReadSceneEntryDescriptor(entryIndex);
        // IDA 0x147B4..0x147F6: the scene-entry owner projects the raw +0x02 word into both the selection-state slot
        // and the data-block mask surface. Once the runtime-owned data block is seeded, those bytes must be
        // read from that durable state so session selection-state mutations survive save/export and reload.
        var selectionStateId = runtime.Interactions.ResolveSelectionStateId(sceneDescriptor.RequiredPersistentDataMask);
        return new ProgramSceneEntry(sceneDescriptor.SelectionEntryIndex,
            selectionStateId,
            selectionStateId,
            sceneDescriptor.SourceColumn,
            sceneDescriptor.SourceRow,
            sceneDescriptor.WidthPixels,
            sceneDescriptor.HeightRows,
            sceneDescriptor.DestinationColumn,
            sceneDescriptor.DestinationRow);
    }

    /// <summary>
    ///     Reads the selection animation frame script projected during boot.
    /// </summary>
    internal byte[] ReadSelectionAnimationFrameScript()
    {
        return runtime.BootData.CopySelectionAnimationFrameScript();
    }

    /// <summary>
    ///     Attempts to describe a scene state by its asset names.
    /// </summary>
    /// <param name="stateId">Scene state id to inspect.</param>
    internal string? TryDescribeSceneHint(ushort stateId)
    {
        return TryReadStateDescriptor(stateId, out var descriptor)
            ? runtime.Assets.FormatSceneHint(descriptor.FirstAssetId, descriptor.SecondAssetId)
            : null;
    }

    /// <summary>
    ///     Formats a state id with a scene hint when available.
    /// </summary>
    /// <param name="stateId">Scene state id to format.</param>
    internal string FormatStateIdForDiagnostics(ushort stateId)
    {
        return TryReadStateDescriptor(stateId, out var descriptor)
            ? FormatStateIdForDiagnostics(stateId, descriptor)
            : $"0x{stateId:X4}";
    }

    /// <summary>
    ///     Formats a known state descriptor with a scene hint when available.
    /// </summary>
    /// <param name="stateId">Scene state id to format.</param>
    /// <param name="descriptor">Resolved state descriptor.</param>
    internal string FormatStateIdForDiagnostics(ushort stateId, ProgramStateDescriptor descriptor)
    {
        var sceneHint = runtime.Assets.FormatSceneHint(descriptor.FirstAssetId, descriptor.SecondAssetId);
        return sceneHint is null ? $"0x{stateId:X4}" : $"0x{stateId:X4} ({sceneHint})";
    }

    /// <summary>
    ///     Reads one initialized scene descriptor.
    /// </summary>
    /// <param name="stateId">Scene descriptor index.</param>
    internal ProgramStateDescriptor ReadStateDescriptor(ushort stateId)
    {
        return !TryReadStateDescriptor(stateId, out var descriptor)
            ? throw new InvalidOperationException(
                $"Program state descriptor {stateId} exceeds the runtime-owned scene descriptor table.")
            : descriptor;
    }

    /// <summary>
    ///     Attempts to read one initialized scene descriptor.
    /// </summary>
    /// <param name="stateId">Scene descriptor index.</param>
    /// <param name="descriptor">Resolved descriptor when the state id is valid.</param>
    internal bool TryReadStateDescriptor(ushort stateId, out ProgramStateDescriptor descriptor)
    {
        try
        {
            EnsureDataBlockInitialized("Scene descriptor reads");
            if (stateId >= SceneDescriptorSection.Count)
            {
                descriptor = default;
                return false;
            }

            var record = runtime.State.RawDataBlock.SceneDescriptors[stateId];
            descriptor = new ProgramStateDescriptor(ResolveAssetId(record.SecondAssetId),
                ResolveAssetId(record.FirstAssetId),
                ResolveTransitionText(record.OptionalTransitionTextSourceOffset),
                record.SceneEntryStartIndex,
                record.SceneEntryCount,
                record.RleWordStreamOffset,
                record.BackdropEnabledFlag,
                record.VisibleRowsBase);
            return true;
        }
        catch (InvalidOperationException)
        {
            descriptor = default;
            return false;
        }
    }

    /// <summary>
    ///     Updates the optional transition text for one scene descriptor in the active session owner.
    /// </summary>
    /// <param name="stateId">Scene descriptor index to update.</param>
    /// <param name="stringId">Replacement optional transition text id.</param>
    internal void SetOptionalTransitionText(ushort stateId, StringId stringId)
    {
        if (stateId >= SceneDescriptorSection.Count)
        {
            throw new InvalidOperationException(
                $"Program state descriptor {stateId} exceeds the runtime-owned scene descriptor table.");
        }

        EnsureDataBlockInitialized("Scene descriptor text mutations");

        runtime.State.RawDataBlock.SceneDescriptors[stateId].OptionalTransitionTextSourceOffset =
            runtime.Strings.GetSourceOffset(stringId);
    }

    private SceneEntryDescriptorView ReadSceneEntryDescriptor(ushort entryIndex)
    {
        EnsureDataBlockInitialized("Scene-entry descriptor reads");
        return runtime.State.RawDataBlock.InteractionDescriptors[entryIndex].AsSceneEntryView();
    }

    private static AssetId ResolveAssetId(ushort value)
    {
        return value == 0 ? AssetId.None : (AssetId)value;
    }

    private StringId ResolveTransitionText(ushort value)
    {
        return runtime.Strings.ResolveSourceOffset(value);
    }

    private void EnsureDataBlockInitialized(string context)
    {
        if (!runtime.State.IsInitialized)
        {
            throw new InvalidOperationException($"{context} require an initialized data block.");
        }
    }
}
