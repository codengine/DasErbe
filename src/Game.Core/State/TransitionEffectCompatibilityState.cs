using Game.Shared.RE;
using Game.Text;

namespace Game.State;

/// <summary>
///     Session-owned compatibility state for the carried-over state-transition display effect slice
///     (<c>ResetTransitionEffect</c>/<c>AdvanceTransitionEffect</c>).
/// </summary>
internal sealed class TransitionEffectCompatibilityState
{
    private byte[]? _activeDynamicTokenText;
    private bool _hasGlyphColumnMaskIndex;
    private byte[]? _pendingDynamicTokenText;

    /// <summary>
    ///     Active transition text consumed by the animated top-band effect.
    /// </summary>
    /// <remarks>
    ///     Adapted from the original active token cursor at <c>dword_19292</c>. The managed state carries the
    ///     semantic text id plus <see cref="ActiveTokenTextIndex" /> instead of a text address cursor.
    /// </remarks>
    internal StringId ActiveTokenText = StringId.None;

    /// <summary>
    ///     Current byte index within <see cref="ActiveTokenText" />.
    /// </summary>
    internal ushort ActiveTokenTextIndex;

    /// <summary>
    ///     Current circular index into the transition-effect column buffer.
    /// </summary>
    [GlobalSymbol("word_1929A", 0x1929A)] internal ushort ColumnBufferIndex;

    /// <summary>
    ///     Enables the animated transition-text effect.
    /// </summary>
    [GlobalSymbol("byte_168F3", 0x168F3)] internal byte EnabledFlag;

    /// <summary>
    ///     Current transition-stream byte normalized to <c>token - 0x20</c> when the byte is non-zero.
    /// </summary>
    [GlobalSymbol("byte_19291", 0x19291)] internal byte GlyphIndex;

    /// <summary>
    ///     Pending transition text promoted after the active text stream drains.
    /// </summary>
    /// <remarks>
    ///     Adapted from the original pending token cursor at <c>word_19296:word_19298</c>. The managed state carries
    ///     the pending semantic text id rather than preserving offset/segment words.
    /// </remarks>
    internal StringId PendingTokenText = StringId.None;

    /// <summary>
    ///     Remaining glyph columns for the active transition-effect character.
    /// </summary>
    [GlobalSymbol("byte_19290", 0x19290)] internal byte RemainingGlyphColumns;

    /// <summary>
    ///     Current index into the published FONT_VGA glyph-column mask table.
    /// </summary>
    /// <remarks>
    ///     Adapted from the original glyph-column mask cursor at <c>dword_1D0E4</c>. The managed state carries a
    ///     table index because the FONT_VGA mask table is already published as a managed buffer.
    /// </remarks>
    private ushort _glyphColumnMaskIndex;

    /// <summary>
    ///     0x960-byte (300 * 8) circular 8-row column buffer cleared by <c>ResetTransitionEffect</c> and advanced by
    ///     <c>AdvanceTransitionEffect</c>.
    /// </summary>
    [GlobalSymbol("dseg:6008", 0x1C778, GlobalFlags.BufferOwner | GlobalFlags.CanonicalOwner)]
    internal byte[] ColumnBuffer { get; } = new byte[0x960];

    /// <summary>
    ///     Gets a value indicating whether an active transition-text stream is currently published.
    /// </summary>
    internal bool HasActiveTokenText => ActiveTokenText != StringId.None || _activeDynamicTokenText is not null;

    /// <summary>
    ///     Gets a value indicating whether the active transition-text stream is backed by dynamic bytes.
    /// </summary>
    internal bool HasActiveDynamicTokenText => _activeDynamicTokenText is not null;

    /// <summary>
    ///     Gets a value indicating whether the pending transition-text stream is backed by dynamic bytes.
    /// </summary>
    internal bool HasPendingDynamicTokenText => _pendingDynamicTokenText is not null;

    /// <summary>
    ///     Publishes one catalog-backed active transition-text stream.
    /// </summary>
    /// <param name="stringId">Semantic reference for the active transition-text stream.</param>
    internal void SetActiveTokenText(StringId stringId)
    {
        ActiveTokenText = stringId;
        _activeDynamicTokenText = null;
    }

    /// <summary>
    ///     Publishes one dynamic active transition-text stream.
    /// </summary>
    /// <param name="textBytes">Caller-owned null-terminated CP437 text bytes.</param>
    internal void SetActiveDynamicTokenText(byte[] textBytes)
    {
        ActiveTokenText = StringId.None;
        _activeDynamicTokenText = textBytes;
    }

    /// <summary>
    ///     Clears the active transition-text stream.
    /// </summary>
    internal void ClearActiveTokenText()
    {
        ActiveTokenText = StringId.None;
        _activeDynamicTokenText = null;
    }

    /// <summary>
    ///     Publishes one catalog-backed pending transition-text stream.
    /// </summary>
    /// <param name="stringId">Semantic reference for the pending transition-text stream.</param>
    internal void SetPendingTokenText(StringId stringId)
    {
        PendingTokenText = stringId;
        _pendingDynamicTokenText = null;
    }

    /// <summary>
    ///     Publishes one dynamic pending transition-text stream.
    /// </summary>
    /// <param name="textBytes">Caller-owned null-terminated CP437 text bytes.</param>
    internal void SetPendingDynamicTokenText(byte[] textBytes)
    {
        PendingTokenText = StringId.None;
        _pendingDynamicTokenText = textBytes;
    }

    /// <summary>
    ///     Clears the pending transition-text stream.
    /// </summary>
    internal void ClearPendingTokenText()
    {
        PendingTokenText = StringId.None;
        _pendingDynamicTokenText = null;
    }

    /// <summary>
    ///     Gets the active dynamic transition-text bytes or throws when the active stream is catalog-backed.
    /// </summary>
    internal ReadOnlySpan<byte> GetActiveDynamicTokenTextOrThrow()
    {
        return _activeDynamicTokenText ??
               throw new InvalidOperationException("The active transition-text stream is not backed by dynamic bytes.");
    }

    /// <summary>
    ///     Gets the pending dynamic transition-text bytes or throws when the pending stream is catalog-backed.
    /// </summary>
    internal ReadOnlySpan<byte> GetPendingDynamicTokenTextOrThrow()
    {
        return _pendingDynamicTokenText ??
               throw new InvalidOperationException(
                   "The pending transition-text stream is not backed by dynamic bytes.");
    }

    /// <summary>
    ///     Publishes the current FONT_VGA glyph-column mask index.
    /// </summary>
    /// <param name="glyphColumnMaskIndex">Index into the glyph-column mask table.</param>
    internal void SetGlyphColumnMaskIndex(ushort glyphColumnMaskIndex)
    {
        _glyphColumnMaskIndex = glyphColumnMaskIndex;
        _hasGlyphColumnMaskIndex = true;
    }

    /// <summary>
    ///     Clears the current FONT_VGA glyph-column mask index.
    /// </summary>
    internal void ClearGlyphColumnMaskIndex()
    {
        _glyphColumnMaskIndex = 0;
        _hasGlyphColumnMaskIndex = false;
    }

    /// <summary>
    ///     Gets the current glyph-column mask index or throws when no glyph is active.
    /// </summary>
    internal ushort GetGlyphColumnMaskIndexOrThrow()
    {
        if (!_hasGlyphColumnMaskIndex)
        {
            throw new InvalidOperationException(
                "sub_1104A expects an active glyph-column-mask index in the published FONT_VGA mask table.");
        }

        return _glyphColumnMaskIndex;
    }

    /// <summary>
    ///     Advances the current glyph-column mask index by one column.
    /// </summary>
    internal void AdvanceGlyphColumnMaskIndex()
    {
        if (!_hasGlyphColumnMaskIndex)
        {
            throw new InvalidOperationException("sub_1104A cannot advance an unset glyph-column-mask index.");
        }

        _glyphColumnMaskIndex++;
    }
}
