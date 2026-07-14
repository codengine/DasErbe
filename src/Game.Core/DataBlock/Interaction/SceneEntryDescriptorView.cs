namespace Game.DataBlock.Interaction;

/// <summary>
///     Scene-specific projection over an <see cref="InteractionDescriptorRecord" />.
/// </summary>
/// <remarks>
///     This view is owned by the scene reader seam. It interprets the overlapping
///     state and coordinate words as scene-entry data, not as interactive hotspot
///     state.
/// </remarks>
internal readonly record struct SceneEntryDescriptorView
{
    /// <summary>
    ///     Creates a scene-entry projection over one raw interaction-descriptor record.
    /// </summary>
    /// <param name="selectionEntryIndex">Persistent selection-entry index referenced by the scene entry.</param>
    /// <param name="requiredPersistentDataMask">
    ///     Raw descriptor word <c>+0x02</c> interpreted as the scene-entry visibility mask.
    /// </param>
    /// <param name="sourceColumn">Signed source column within the scene asset.</param>
    /// <param name="sourceRow">Signed source row within the scene asset.</param>
    /// <param name="widthPixels">Signed scene fragment width in pixels.</param>
    /// <param name="heightRows">Signed scene fragment height in rows.</param>
    /// <param name="destinationColumn">Signed destination column on the rendered scene.</param>
    /// <param name="destinationRow">Signed destination row on the rendered scene.</param>
    internal SceneEntryDescriptorView(ushort selectionEntryIndex,
        ushort requiredPersistentDataMask,
        short sourceColumn,
        short sourceRow,
        short widthPixels,
        short heightRows,
        short destinationColumn,
        short destinationRow)
    {
        SelectionEntryIndex = selectionEntryIndex;
        RequiredPersistentDataMask = requiredPersistentDataMask;
        SourceColumn = sourceColumn;
        SourceRow = sourceRow;
        WidthPixels = widthPixels;
        HeightRows = heightRows;
        DestinationColumn = destinationColumn;
        DestinationRow = destinationRow;
    }

    /// <summary>
    ///     Persistent selection-entry index referenced by the scene entry.
    /// </summary>
    internal ushort SelectionEntryIndex { get; }

    /// <summary>
    ///     Raw descriptor word <c>+0x02</c> interpreted as the scene-entry visibility mask.
    /// </summary>
    internal ushort RequiredPersistentDataMask { get; }

    /// <summary>
    ///     Signed source column within the scene asset.
    /// </summary>
    internal short SourceColumn { get; }

    /// <summary>
    ///     Signed source row within the scene asset.
    /// </summary>
    internal short SourceRow { get; }

    /// <summary>
    ///     Signed scene fragment width in pixels.
    /// </summary>
    internal short WidthPixels { get; }

    /// <summary>
    ///     Signed scene fragment height in rows.
    /// </summary>
    internal short HeightRows { get; }

    /// <summary>
    ///     Signed destination column on the rendered scene.
    /// </summary>
    internal short DestinationColumn { get; }

    /// <summary>
    ///     Signed destination row on the rendered scene.
    /// </summary>
    internal short DestinationRow { get; }
}
