using System.Buffers.Binary;
using Game.Shared.RE;

namespace Game.DataBlock.Control;

/// <summary>
///     Raw control/footer section from the data block.
/// </summary>
internal sealed class ControlSectionData
{
    /// <summary>
    ///     Section start offset within the data block.
    /// </summary>
    private const int Offset = 0x17EC;

    /// <summary>
    ///     Total section length in bytes.
    /// </summary>
    internal const int Length = 0x26;

    /// <summary>
    ///     Current program-state id at data-block word <c>+0x17EC</c>, consumed by the outer dispatch loop.
    /// </summary>
    [GlobalSymbol("word_187F8", 0x187F8)]
    internal ushort ProgramStateId { get; set; }

    /// <summary>
    ///     Running environmental-error count at data-block word <c>+0x17EE</c>, consumed by queued warning flows,
    ///     the save-screen mask, and the interaction return path.
    /// </summary>
    [GlobalSymbol("word_187FA", 0x187FA)]
    internal ushort ErrorCount { get; set; }

    /// <summary>
    ///     Shared story-progress tracker backed by data-block word <c>+0x17F0</c>.
    /// </summary>
    [GlobalSymbol("word_187FC", 0x187FC)]
    internal StoryProgressTracker StoryProgress { get; } = new();

    /// <summary>
    ///     Shared Lolita-heart progress tracker backed by data-block word <c>+0x17F2</c>.
    /// </summary>
    [GlobalSymbol("word_187FE", 0x187FE)]
    internal LolitaProgressTracker LolitaProgress { get; } = new();

    /// <summary>
    ///     Backdrop destination column at data-block word <c>+0x17F4</c>, consumed by <c>RenderBackdrop</c>.
    /// </summary>
    [GlobalSymbol("word_18800", 0x18800)]
    internal short BackdropColumn { get; set; }

    /// <summary>
    ///     Signed row threshold at data-block word <c>+0x17F6</c>, used by <c>RenderCurrentScene</c> to decide when
    ///     the backdrop seam must run.
    /// </summary>
    [GlobalSymbol("word_18802", 0x18802)]
    internal short BackdropThresholdRow { get; set; }

    /// <summary>
    ///     Backdrop selection index at data-block word <c>+0x17F8</c>, consumed by <c>RenderBackdrop</c>.
    /// </summary>
    [GlobalSymbol("word_18804", 0x18804)]
    internal short BackdropSelectionIndex { get; set; }

    /// <summary>
    ///     Backdrop selection row at data-block word <c>+0x17FA</c>, consumed by <c>RenderBackdrop</c>.
    /// </summary>
    [GlobalSymbol("word_18806", 0x18806)]
    internal short BackdropSelectionRow { get; set; }

    /// <summary>
    ///     Backdrop-enable byte at data-block byte <c>+0x17FC</c>, latched by <c>prepareSceneTransition</c> and
    ///     consulted by later program-scene handlers.
    /// </summary>
    [GlobalSymbol("byte_18808", 0x18808)]
    internal byte BackdropEnabledFlag { get; set; }

    /// <summary>
    ///     Alternate-hero-portrait flag at data-block byte <c>+0x17FD</c>, gating whether bootstrap and later
    ///     handlers decode <c>HELD.LBM</c> or <c>HELD_S.LBM</c> into the backdrop source surface.
    /// </summary>
    [GlobalSymbol("byte_18809", 0x18809)]
    internal bool UseAlternateHeroPortrait { get; set; }

    /// <summary>
    ///     State-advance request flag at data-block byte <c>+0x17FE</c>, consumed by the main interaction loop in
    ///     <c>RunInteractiveLoop</c>.
    /// </summary>
    [GlobalSymbol("byte_1880A", 0x1880A)]
    internal byte AdvanceRequestedFlag { get; set; }

    /// <summary>
    ///     Current starting index into the program-state transition text-entry table at data-block byte <c>+0x17FF</c>,
    ///     consumed by <c>renderTransitionTextPanel</c>.
    /// </summary>
    [GlobalSymbol("byte_1880B", 0x1880B)]
    internal byte TransitionTextStartIndex { get; set; }

    /// <summary>
    ///     16-entry transition-text state table at data-block bytes <c>+0x1800..+0x180F</c>, walked while the
    ///     transition-text panel advances through executable-backed lines.
    /// </summary>
    [GlobalSymbol("", 0x1880C, GlobalFlags.BufferView | GlobalFlags.AliasView | GlobalFlags.PersistedState)]
    internal byte[] TransitionTextEntryStates { get; } = new byte[0x10];

    /// <summary>
    ///     Descriptor-backed backdrop-span base byte at data-block byte <c>+0x1810</c>, latched by
    ///     <c>prepareSceneTransition</c> and consumed by later program-scene handlers.
    /// </summary>
    [GlobalSymbol("byte_1881C", 0x1881C)]
    internal byte VisibleRowsBase { get; set; }

    private byte _trailingPaddingByte;

    /// <summary>
    ///     Reads the control section from raw data-block bytes.
    /// </summary>
    /// <param name="block">Complete or sufficiently large data block.</param>
    internal void ReadFromBlock(ReadOnlySpan<byte> block)
    {
        if (block.Length < Offset + Length)
        {
            throw new InvalidOperationException(
                $"Control section block must contain at least 0x{Offset + Length:X} bytes.");
        }

        var source = block.Slice(Offset, Length);

        ProgramStateId = BinaryPrimitives.ReadUInt16LittleEndian(source[..sizeof(ushort)]);
        ErrorCount = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x02, sizeof(ushort)));
        StoryProgress.ReadFromRawValue(BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x04, sizeof(ushort))));
        LolitaProgress.ReadFromRawValue(BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(0x06, sizeof(ushort))));
        BackdropColumn = BinaryPrimitives.ReadInt16LittleEndian(source.Slice(0x08, sizeof(short)));
        BackdropThresholdRow = BinaryPrimitives.ReadInt16LittleEndian(source.Slice(0x0A, sizeof(short)));
        BackdropSelectionIndex = BinaryPrimitives.ReadInt16LittleEndian(source.Slice(0x0C, sizeof(short)));
        BackdropSelectionRow = BinaryPrimitives.ReadInt16LittleEndian(source.Slice(0x0E, sizeof(short)));
        BackdropEnabledFlag = source[0x10];
        UseAlternateHeroPortrait = source[0x11] != 0;
        AdvanceRequestedFlag = source[0x12];
        TransitionTextStartIndex = source[0x13];
        source.Slice(0x14, 0x10).CopyTo(TransitionTextEntryStates);
        VisibleRowsBase = source[0x24];
        _trailingPaddingByte = source[0x25];
    }

    /// <summary>
    ///     Writes the control section to raw data-block bytes.
    /// </summary>
    /// <param name="block">Complete or sufficiently large data block.</param>
    internal void WriteToBlock(Span<byte> block)
    {
        if (block.Length < Offset + Length)
        {
            throw new InvalidOperationException(
                $"Control section block must provide at least 0x{Offset + Length:X} bytes.");
        }

        var destination = block.Slice(Offset, Length);

        BinaryPrimitives.WriteUInt16LittleEndian(destination[..sizeof(ushort)], ProgramStateId);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x02, sizeof(ushort)), ErrorCount);
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x04, sizeof(ushort)), StoryProgress.ToRawValue());
        BinaryPrimitives.WriteUInt16LittleEndian(destination.Slice(0x06, sizeof(ushort)), LolitaProgress.ToRawValue());
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(0x08, sizeof(short)), BackdropColumn);
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(0x0A, sizeof(short)), BackdropThresholdRow);
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(0x0C, sizeof(short)), BackdropSelectionIndex);
        BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(0x0E, sizeof(short)), BackdropSelectionRow);
        destination[0x10] = BackdropEnabledFlag;
        destination[0x11] = UseAlternateHeroPortrait ? (byte)1 : (byte)0;
        destination[0x12] = AdvanceRequestedFlag;
        destination[0x13] = TransitionTextStartIndex;
        TransitionTextEntryStates.AsSpan().CopyTo(destination.Slice(0x14, 0x10));
        destination[0x24] = VisibleRowsBase;
        destination[0x25] = _trailingPaddingByte;
    }
}
