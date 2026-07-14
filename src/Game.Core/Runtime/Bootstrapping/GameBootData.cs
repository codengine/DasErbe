using System.Collections.Frozen;
using Game.Catalogs;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Bootstrapping;

/// <summary>
///     Immutable data projected from boot assets before runtime catalogs are constructed.
/// </summary>
internal sealed class GameBootData
{
    private readonly ushort[] _backdropBaseWidths;
    private readonly ushort[] _backdropDescriptorSelections;
    private readonly ushort[] _backdropSourceColumns;
    private readonly ushort[] _backdropSourceRows;
    private readonly byte[] _dataBlockSeed;
    private readonly InteractionHandlerId[] _interactionHandlers;
    private readonly byte[] _selectionAnimationFrameScript;
    private readonly FrozenDictionary<ushort, byte[]> _wordRunStreamsBySourceOffset;

    /// <summary>
    ///     Creates the boot data snapshot used by runtime-owned catalogs and bootstrap seeders.
    /// </summary>
    /// <param name="originalStrings">Original decoded string text keyed by runtime string id.</param>
    /// <param name="sourceOffsetsByStringId">Original source offsets keyed by runtime string id.</param>
    /// <param name="stringIdsBySourceOffset">Runtime string ids keyed by original source offset.</param>
    /// <param name="dataBlockSeed">Boot data bytes for the data block.</param>
    /// <param name="interactionHandlers">Original interaction handler table entries.</param>
    /// <param name="selectionAnimationFrameScript">Selection animation frame script bytes.</param>
    /// <param name="backdropDescriptorSelections">Backdrop descriptor-selection table entries.</param>
    /// <param name="backdropSourceColumns">Backdrop source-column table entries.</param>
    /// <param name="backdropSourceRows">Backdrop source-row table entries.</param>
    /// <param name="backdropBaseWidths">Backdrop base-width table entries.</param>
    /// <param name="wordRunStreamsBySourceOffset">RLE word-stream bytes keyed by original source offset.</param>
    /// <param name="carTrafficJamFrameTables">Car traffic-jam cutscene frame tables.</param>
    internal GameBootData(IReadOnlyDictionary<StringId, string> originalStrings,
        IReadOnlyDictionary<StringId, ushort> sourceOffsetsByStringId,
        IReadOnlyDictionary<ushort, StringId> stringIdsBySourceOffset,
        byte[] dataBlockSeed,
        InteractionHandlerId[] interactionHandlers,
        byte[] selectionAnimationFrameScript,
        ushort[] backdropDescriptorSelections,
        ushort[] backdropSourceColumns,
        ushort[] backdropSourceRows,
        ushort[] backdropBaseWidths,
        IReadOnlyDictionary<ushort, byte[]> wordRunStreamsBySourceOffset,
        CarTrafficJamFrameTables carTrafficJamFrameTables)
    {
        OriginalStrings = originalStrings.ToFrozenDictionary();
        SourceOffsetsByStringId = sourceOffsetsByStringId.ToFrozenDictionary();
        StringIdsBySourceOffset = stringIdsBySourceOffset.ToFrozenDictionary();
        _dataBlockSeed = [.. dataBlockSeed];
        _interactionHandlers = [.. interactionHandlers];
        _selectionAnimationFrameScript = [.. selectionAnimationFrameScript];
        _backdropDescriptorSelections = [.. backdropDescriptorSelections];
        _backdropSourceColumns = [.. backdropSourceColumns];
        _backdropSourceRows = [.. backdropSourceRows];
        _backdropBaseWidths = [.. backdropBaseWidths];
        _wordRunStreamsBySourceOffset = wordRunStreamsBySourceOffset.ToFrozenDictionary(static entry => entry.Key,
            static entry => (byte[])[.. entry.Value]);
        CarTrafficJamFrameTables = carTrafficJamFrameTables;
    }

    /// <summary>
    ///     Gets the original decoded string text keyed by runtime string id.
    /// </summary>
    internal IReadOnlyDictionary<StringId, string> OriginalStrings { get; }

    /// <summary>
    ///     Gets original source offsets by runtime string id.
    /// </summary>
    internal IReadOnlyDictionary<StringId, ushort> SourceOffsetsByStringId { get; }

    /// <summary>
    ///     Gets runtime string ids by original source offset.
    /// </summary>
    internal IReadOnlyDictionary<ushort, StringId> StringIdsBySourceOffset { get; }

    /// <summary>
    ///     Gets the boot-seeded data block bytes.
    /// </summary>
    internal ReadOnlySpan<byte> DataBlockSeed => _dataBlockSeed;

    /// <summary>
    ///     Gets the purpose-named car traffic jam cutscene frame tables.
    /// </summary>
    internal CarTrafficJamFrameTables CarTrafficJamFrameTables { get; }

    /// <summary>
    ///     Gets the interaction handler table entry count.
    /// </summary>
    internal int InteractionHandlerCount => _interactionHandlers.Length;

    /// <summary>
    ///     Reads one boot-projected interaction handler id.
    /// </summary>
    /// <param name="handlerWordIndex">Word index into the projected interaction handler table.</param>
    internal InteractionHandlerId ReadInteractionHandlerId(int handlerWordIndex)
    {
        return _interactionHandlers[handlerWordIndex];
    }

    /// <summary>
    ///     Copies the boot-projected selection animation frame script.
    /// </summary>
    internal byte[] CopySelectionAnimationFrameScript()
    {
        return [.. _selectionAnimationFrameScript];
    }

    /// <summary>
    ///     Reads one boot-projected backdrop base-width entry.
    /// </summary>
    /// <param name="descriptorEntryIndex">Backdrop descriptor entry index.</param>
    internal ushort ReadBackdropBaseWidth(ushort descriptorEntryIndex)
    {
        return _backdropBaseWidths[descriptorEntryIndex];
    }

    /// <summary>
    ///     Reads one boot-projected backdrop descriptor-selection entry.
    /// </summary>
    /// <param name="selectionTableIndex">Backdrop selection table index.</param>
    internal ushort ReadBackdropDescriptorSelection(ushort selectionTableIndex)
    {
        return _backdropDescriptorSelections[selectionTableIndex];
    }

    /// <summary>
    ///     Reads one boot-projected backdrop source-column entry.
    /// </summary>
    /// <param name="descriptorEntryIndex">Backdrop descriptor entry index.</param>
    internal ushort ReadBackdropSourceColumn(ushort descriptorEntryIndex)
    {
        return _backdropSourceColumns[descriptorEntryIndex];
    }

    /// <summary>
    ///     Reads one boot-projected backdrop source-row entry.
    /// </summary>
    /// <param name="descriptorEntryIndex">Backdrop descriptor entry index.</param>
    internal ushort ReadBackdropSourceRow(ushort descriptorEntryIndex)
    {
        return _backdropSourceRows[descriptorEntryIndex];
    }

    /// <summary>
    ///     Gets the boot-projected RLE word stream for a source offset.
    /// </summary>
    /// <param name="sourceOffset">Original source offset of the RLE stream.</param>
    internal ReadOnlySpan<byte> GetWordRunStream(ushort sourceOffset)
    {
        return _wordRunStreamsBySourceOffset.TryGetValue(sourceOffset, out var stream)
            ? stream
            : throw new InvalidOperationException($"RLE word stream 0x{sourceOffset:X4} is not in boot data.");
    }
}

/// <summary>
///     Immutable frame tables used by the traffic-jam cutscene.
/// </summary>
internal sealed class CarTrafficJamFrameTables
{
    /// <summary>
    ///     Boot-projected copy of original <c>word_1BDAC</c> at 0x1BDAC.
    /// </summary>
    [GlobalSymbol("word_1BDAC", 0x1BDAC, GlobalFlags.TableOwner)]
    private readonly ushort[] _heights;

    /// <summary>
    ///     Boot-projected copy of original <c>word_1BD94</c> at 0x1BD94.
    /// </summary>
    [GlobalSymbol("word_1BD94", 0x1BD94, GlobalFlags.TableOwner)]
    private readonly ushort[] _sourceColumns;

    /// <summary>
    ///     Boot-projected copy of original <c>word_1BD9C</c> at 0x1BD9C.
    /// </summary>
    [GlobalSymbol("word_1BD9C", 0x1BD9C, GlobalFlags.TableOwner)]
    private readonly ushort[] _sourceRows;

    /// <summary>
    ///     Boot-projected copy of original <c>word_1BDA4</c> at 0x1BDA4.
    /// </summary>
    [GlobalSymbol("word_1BDA4", 0x1BDA4, GlobalFlags.TableOwner)]
    private readonly ushort[] _widths;

    /// <summary>
    ///     Creates boot-projected frame tables for the traffic-jam cutscene.
    /// </summary>
    /// <param name="sourceColumns">Source columns for each cutscene frame.</param>
    /// <param name="sourceRows">Source rows for each cutscene frame.</param>
    /// <param name="widths">Widths for each cutscene frame.</param>
    /// <param name="heights">Heights for each cutscene frame.</param>
    internal CarTrafficJamFrameTables(ushort[] sourceColumns, ushort[] sourceRows, ushort[] widths, ushort[] heights)
    {
        _sourceColumns = [.. sourceColumns];
        _sourceRows = [.. sourceRows];
        _widths = [.. widths];
        _heights = [.. heights];
    }

    /// <summary>
    ///     Copies the frame tables into the supplied cutscene arrays.
    /// </summary>
    /// <param name="sourceColumns">Destination source-column table.</param>
    /// <param name="sourceRows">Destination source-row table.</param>
    /// <param name="widths">Destination width table.</param>
    /// <param name="heights">Destination height table.</param>
    internal void CopyTo(ushort[] sourceColumns, ushort[] sourceRows, ushort[] widths, ushort[] heights)
    {
        _sourceColumns.CopyTo(sourceColumns, 0);
        _sourceRows.CopyTo(sourceRows, 0);
        _widths.CopyTo(widths, 0);
        _heights.CopyTo(heights, 0);
    }
}
