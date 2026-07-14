namespace Game.DataBlock.Interaction;

/// <summary>
///     Interactive-selection projection over an <see cref="InteractionDescriptorRecord" />.
/// </summary>
/// <remarks>
///     This view describes the durable interactive descriptor fields that feed the
///     managed selection/runtime seam. It is still a projection over raw storage,
///     not the owning executable/runtime descriptor type.
/// </remarks>
internal readonly record struct InteractiveSelectionDescriptorView
{
    /// <summary>
    ///     Creates an interactive-selection projection over one raw interaction-descriptor record.
    /// </summary>
    /// <param name="selectionEntryIndex">
    ///     Persistent selection-entry index that owns the interactive state for this descriptor.
    /// </param>
    /// <param name="selectionStateId">
    ///     Raw descriptor word <c>+0x02</c> interpreted as the interactive selection state id.
    /// </param>
    /// <param name="widthPixels">Interactive hotspot width in pixels.</param>
    /// <param name="heightRows">Interactive hotspot height in rows.</param>
    /// <param name="leftColumn">Interactive hotspot left column.</param>
    /// <param name="topRow">Interactive hotspot top row.</param>
    /// <param name="selectionTextSourceOffset">
    ///     Raw source offset for the published selection text from descriptor word <c>+0x10</c>.
    /// </param>
    /// <param name="nextProgramStateId">Program state reached when the interaction completes.</param>
    /// <param name="resultBackdropColumn">Signed backdrop column used for the interaction result.</param>
    /// <param name="resultBackdropThresholdRow">
    ///     Signed backdrop threshold row used for the interaction result.
    /// </param>
    /// <param name="resultBackdropSelectionRow">
    ///     Signed backdrop selection row used for the interaction result.
    /// </param>
    /// <param name="animationTargetBackdropColumn">
    ///     Signed target backdrop column used by follow-up backdrop animation.
    /// </param>
    /// <param name="animationTargetBackdropThresholdRow">
    ///     Signed target backdrop threshold row used by follow-up backdrop animation.
    /// </param>
    /// <param name="animationTargetBackdropSelectionRow">
    ///     Signed target backdrop selection row used by follow-up backdrop animation.
    /// </param>
    internal InteractiveSelectionDescriptorView(ushort selectionEntryIndex,
        ushort selectionStateId,
        ushort widthPixels,
        ushort heightRows,
        ushort leftColumn,
        ushort topRow,
        ushort selectionTextSourceOffset,
        ushort nextProgramStateId,
        short resultBackdropColumn,
        short resultBackdropThresholdRow,
        short resultBackdropSelectionRow,
        short animationTargetBackdropColumn,
        short animationTargetBackdropThresholdRow,
        short animationTargetBackdropSelectionRow)
    {
        SelectionEntryIndex = selectionEntryIndex;
        SelectionStateId = selectionStateId;
        WidthPixels = widthPixels;
        HeightRows = heightRows;
        LeftColumn = leftColumn;
        TopRow = topRow;
        SelectionTextSourceOffset = selectionTextSourceOffset;
        NextProgramStateId = nextProgramStateId;
        ResultBackdropColumn = resultBackdropColumn;
        ResultBackdropThresholdRow = resultBackdropThresholdRow;
        ResultBackdropSelectionRow = resultBackdropSelectionRow;
        AnimationTargetBackdropColumn = animationTargetBackdropColumn;
        AnimationTargetBackdropThresholdRow = animationTargetBackdropThresholdRow;
        AnimationTargetBackdropSelectionRow = animationTargetBackdropSelectionRow;
    }

    /// <summary>
    ///     Persistent selection-entry index that owns the interactive state for this descriptor.
    /// </summary>
    internal ushort SelectionEntryIndex { get; }

    /// <summary>
    ///     Raw descriptor word <c>+0x02</c> interpreted as the interactive selection state id.
    /// </summary>
    internal ushort SelectionStateId { get; }

    /// <summary>
    ///     Interactive hotspot width in pixels.
    /// </summary>
    internal ushort WidthPixels { get; }

    /// <summary>
    ///     Interactive hotspot height in rows.
    /// </summary>
    internal ushort HeightRows { get; }

    /// <summary>
    ///     Interactive hotspot left column.
    /// </summary>
    internal ushort LeftColumn { get; }

    /// <summary>
    ///     Interactive hotspot top row.
    /// </summary>
    internal ushort TopRow { get; }

    /// <summary>
    ///     Raw source offset for the published selection text from descriptor word <c>+0x10</c>.
    /// </summary>
    internal ushort SelectionTextSourceOffset { get; }

    /// <summary>
    ///     Program state reached when the interaction completes.
    /// </summary>
    internal ushort NextProgramStateId { get; }

    /// <summary>
    ///     Signed backdrop column used for the interaction result.
    /// </summary>
    internal short ResultBackdropColumn { get; }

    /// <summary>
    ///     Signed backdrop threshold row used for the interaction result.
    /// </summary>
    internal short ResultBackdropThresholdRow { get; }

    /// <summary>
    ///     Signed backdrop selection row used for the interaction result.
    /// </summary>
    internal short ResultBackdropSelectionRow { get; }

    /// <summary>
    ///     Signed target backdrop column used by follow-up backdrop animation.
    /// </summary>
    internal short AnimationTargetBackdropColumn { get; }

    /// <summary>
    ///     Signed target backdrop threshold row used by follow-up backdrop animation.
    /// </summary>
    internal short AnimationTargetBackdropThresholdRow { get; }

    /// <summary>
    ///     Signed target backdrop selection row used by follow-up backdrop animation.
    /// </summary>
    internal short AnimationTargetBackdropSelectionRow { get; }
}
