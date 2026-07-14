using System.Buffers.Binary;

namespace Game.DataBlock.Interaction;

/// <summary>
///     Raw 32-byte interaction-descriptor storage record from the data block.
/// </summary>
/// <remarks>
///     The same stored words are reinterpreted by multiple higher-level contracts.
///     This type intentionally stays storage-shaped so higher-level code does not accidentally
///     treat one projection as the record's universal meaning.
/// </remarks>
internal sealed class InteractionDescriptorRecord
{
    /// <summary>
    ///     Size of one interaction-descriptor record in bytes.
    /// </summary>
    internal const int Size = 0x20;

    private ushort _selectionEntryIndex;

    /// <summary>
    ///     Shared descriptor word <c>+0x02</c>, interpreted by higher-level projections.
    /// </summary>
    internal ushort StateWord { get; set; }

    /// <summary>
    ///     Raw descriptor word <c>+0x04</c>.
    /// </summary>
    /// <remarks>
    ///     This word intentionally stays unresolved in the raw storage model. In the scene projection it is used as
    ///     <c>SourceColumn</c>. In the reviewed living-room table-placement use of record <c>0x52</c>, it currently
    ///     carries the source placement column. Outside those proven uses, the raw record keeps it ambiguous on
    ///     purpose.
    /// </remarks>
    internal ushort SourceColumn { get; set; }

    /// <summary>
    ///     Raw descriptor word <c>+0x06</c>.
    /// </summary>
    /// <remarks>
    ///     This word intentionally stays unresolved in the raw storage model. In the scene projection it is used as
    ///     <c>SourceRow</c>. In the reviewed living-room table-placement use of record <c>0x52</c>, it currently
    ///     carries the source placement row. Outside those proven uses, the raw record keeps it ambiguous on purpose.
    /// </remarks>
    internal ushort SourceRow { get; set; }

    /// <summary>
    ///     Shared descriptor word <c>+0x08</c>.
    /// </summary>
    internal ushort WidthPixels { get; set; }

    /// <summary>
    ///     Shared descriptor word <c>+0x0A</c>.
    /// </summary>
    internal ushort HeightRows { get; set; }

    /// <summary>
    ///     Shared descriptor word <c>+0x0C</c>, interpreted by higher-level projections.
    /// </summary>
    internal ushort PositionColumnWord { get; set; }

    /// <summary>
    ///     Shared descriptor word <c>+0x0E</c>, interpreted by higher-level projections.
    /// </summary>
    internal ushort PositionRowWord { get; set; }

    /// <summary>
    ///     Raw source offset for the published selection text from descriptor word <c>+0x10</c>.
    /// </summary>
    internal ushort SelectionTextSourceOffset { get; set; }

    private ushort _nextProgramStateId;

    private ushort _resultBackdropColumn;

    private ushort _resultBackdropThresholdRow;

    private ushort _resultBackdropSelectionRow;

    private ushort _animationTargetBackdropColumn;

    private ushort _animationTargetBackdropThresholdRow;

    private ushort _animationTargetBackdropSelectionRow;

    /// <summary>
    ///     Projects this raw record into the scene-entry contract.
    /// </summary>
    /// <remarks>
    ///     In this view, the shared state and position words are interpreted as the
    ///     scene reader expects them, including signed coordinate semantics.
    /// </remarks>
    internal SceneEntryDescriptorView AsSceneEntryView()
    {
        return new SceneEntryDescriptorView(_selectionEntryIndex,
            StateWord,
            unchecked((short)SourceColumn),
            unchecked((short)SourceRow),
            unchecked((short)WidthPixels),
            unchecked((short)HeightRows),
            unchecked((short)PositionColumnWord),
            unchecked((short)PositionRowWord));
    }

    /// <summary>
    ///     Projects this raw record into the interactive-selection contract.
    /// </summary>
    /// <remarks>
    ///     This is the persistent-data view used by the interactive executable and
    ///     managed runtime seams. It intentionally leaves <c>+0x04</c> and
    ///     <c>+0x06</c> unresolved because those words are only proven in the scene
    ///     projection so far.
    /// </remarks>
    internal InteractiveSelectionDescriptorView AsInteractiveSelectionView()
    {
        return new InteractiveSelectionDescriptorView(_selectionEntryIndex,
            StateWord,
            WidthPixels,
            HeightRows,
            PositionColumnWord,
            PositionRowWord,
            SelectionTextSourceOffset,
            _nextProgramStateId,
            unchecked((short)_resultBackdropColumn),
            unchecked((short)_resultBackdropThresholdRow),
            unchecked((short)_resultBackdropSelectionRow),
            unchecked((short)_animationTargetBackdropColumn),
            unchecked((short)_animationTargetBackdropThresholdRow),
            unchecked((short)_animationTargetBackdropSelectionRow));
    }

    /// <summary>
    ///     Reads this record from raw data-block bytes.
    /// </summary>
    /// <param name="source">Source span that starts at the record boundary.</param>
    internal void ReadFrom(ReadOnlySpan<byte> source)
    {
        if (source.Length < Size)
        {
            throw new InvalidOperationException(
                $"Interaction descriptor record source must contain at least 0x{Size:X} bytes.");
        }

        _selectionEntryIndex = BinaryPrimitives.ReadUInt16LittleEndian(source[..sizeof(ushort)]);
        StateWord = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x02, sizeof(ushort)));
        SourceColumn = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x04, sizeof(ushort)));
        SourceRow = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x06, sizeof(ushort)));
        WidthPixels = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x08, sizeof(ushort)));
        HeightRows = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x0A, sizeof(ushort)));
        PositionColumnWord = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x0C, sizeof(ushort)));
        PositionRowWord = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x0E, sizeof(ushort)));
        SelectionTextSourceOffset = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x10, sizeof(ushort)));
        _nextProgramStateId = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x12, sizeof(ushort)));
        _resultBackdropColumn = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x14, sizeof(ushort)));
        _resultBackdropThresholdRow = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x16, sizeof(ushort)));
        _resultBackdropSelectionRow = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x18, sizeof(ushort)));
        _animationTargetBackdropColumn = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x1A, sizeof(ushort)));
        _animationTargetBackdropThresholdRow =
            BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x1C, sizeof(ushort)));
        _animationTargetBackdropSelectionRow =
            BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x1E, sizeof(ushort)));
    }

    /// <summary>
    ///     Writes this record to raw data-block bytes.
    /// </summary>
    /// <param name="destination">Destination span that starts at the record boundary.</param>
    internal void WriteTo(Span<byte> destination)
    {
        if (destination.Length < Size)
        {
            throw new InvalidOperationException(
                $"Interaction descriptor record destination must provide at least 0x{Size:X} bytes.");
        }

        BinaryPrimitives.WriteUInt16LittleEndian(destination[..sizeof(ushort)], _selectionEntryIndex);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x02, sizeof(ushort)), StateWord);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x04, sizeof(ushort)), SourceColumn);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x06, sizeof(ushort)), SourceRow);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x08, sizeof(ushort)), WidthPixels);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x0A, sizeof(ushort)), HeightRows);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x0C, sizeof(ushort)), PositionColumnWord);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x0E, sizeof(ushort)), PositionRowWord);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x10, sizeof(ushort)), SelectionTextSourceOffset);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x12, sizeof(ushort)), _nextProgramStateId);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x14, sizeof(ushort)), _resultBackdropColumn);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x16, sizeof(ushort)), _resultBackdropThresholdRow);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x18, sizeof(ushort)), _resultBackdropSelectionRow);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x1A, sizeof(ushort)),
            _animationTargetBackdropColumn);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x1C, sizeof(ushort)),
            _animationTargetBackdropThresholdRow);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x1E, sizeof(ushort)),
            _animationTargetBackdropSelectionRow);
    }
}
