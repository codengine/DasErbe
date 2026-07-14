using System.Text;
using Game.Shared.Diagnostics;
using Game.Shared.RE;
using Game.Shared.Rendering;
using Game.State;
using Game.Text;

namespace Game.Display;

/// <summary>
///     Session-owned transition-effect helpers for carried-over state-transition display symbols.
/// </summary>
/// <param name="state">Live runtime state.</param>
/// <param name="strings">String catalog used for queued text resolution and diagnostics.</param>
internal sealed class TransitionEffectController(RuntimeState state, GameStringCatalog strings)
{
    private const int TransitionColumnCount = 0x12C;
    private const int TransitionColumnHeight = 0x08;
    private const ushort TransitionBandOffsetBytes = 0x28F2;
    private const int ModeXStrideBytes = 80;
    private const int TransitionBandTopRow = TransitionBandOffsetBytes / ModeXStrideBytes;
    private const int TransitionBandLeftColumn = TransitionBandOffsetBytes % ModeXStrideBytes * 4 + 3;

    /// <summary>
    ///     Queues the next transition-text stream for the animated top-band effect.
    /// </summary>
    /// <param name="stringId">Semantic reference for the next transition-text stream.</param>
    [FunctionSymbol("sub_10FD5", 0x10FD5)]
    internal void QueueStateTransitionEffectText(StringId stringId)
    {
        var transition = state.TransitionEffect;

        // IDA 0x10FD8..0x10FE4: enable the transition effect and check whether an active token cursor is already live.
        transition.EnabledFlag = 1;
        var hasActiveTokenText = transition.HasActiveTokenText;
        LogTransitionEffectCursorState("sub_10FD5 queue", transition, stringId);

        if (!hasActiveTokenText)
        {
            // IDA 0x10FE6..0x10FF3: when no active stream is running, publish the incoming text stream as the active
            // token cursor immediately.
            transition.SetActiveTokenText(stringId);
            transition.ActiveTokenTextIndex = 0;
            LogTransitionEffectCursorState("sub_10FD5 activate", transition, stringId);
            return;
        }

        // IDA 0x10FF5..0x11002: otherwise publish the incoming stream as the pending token cursor.
        transition.SetPendingTokenText(stringId);
        LogTransitionEffectCursorState("sub_10FD5 pend", transition, stringId);
    }

    /// <summary>
    ///     Queues one dynamic transition-text stream for the animated top-band effect.
    /// </summary>
    /// <param name="textBytes">Caller-owned null-terminated CP437 text bytes.</param>
    internal void QueueStateTransitionEffectText(ReadOnlySpan<byte> textBytes)
    {
        var transition = state.TransitionEffect;
        var copiedBytes = textBytes.ToArray();

        transition.EnabledFlag = 1;
        if (!transition.HasActiveTokenText)
        {
            transition.SetActiveDynamicTokenText(copiedBytes);
            transition.ActiveTokenTextIndex = 0;
            return;
        }

        transition.SetPendingDynamicTokenText(copiedBytes);
    }

    /// <summary>
    ///     Resets the animated transition-text effect state and primes its first redraw.
    /// </summary>
    [FunctionSymbol("sub_11004", 0x11004, FunctionFlags.AdaptedForMonoGame)]
    internal void ResetTransitionEffect()
    {
        var transition = state.TransitionEffect;

        // IDA 0x1100E..0x1101F: reset the pending and active token-stream state consumed by sub_1104A.
        transition.ClearPendingTokenText();
        transition.ClearActiveTokenText();
        transition.ActiveTokenTextIndex = 0;
        transition.ClearGlyphColumnMaskIndex();

        // IDA 0x11020..0x11032: clear the 0x960-byte transition-pattern table at dseg:6008.
        Array.Clear(transition.ColumnBuffer);

        // IDA 0x11034..0x11049: reset the countdown byte and call sub_1104A once while byte_168F3 is set.
        transition.RemainingGlyphColumns = 0;
        transition.EnabledFlag = 1;

        // IDA 0x1103E: advance/redraw the state-transition effect once while byte_168F3 is set.
        AdvanceTransitionEffect();

        // IDA 0x11041..0x11049: restore byte_168F3 after the effect apply returns.
        transition.EnabledFlag = 0;
    }

    /// <summary>
    ///     Advances the animated transition-text effect by one column update.
    /// </summary>
    [FunctionSymbol("sub_1104A", 0x1104A, FunctionFlags.AdaptedForMonoGame)]
    internal void AdvanceTransitionEffect()
    {
        var transition = state.TransitionEffect;
        var currentColumnBufferIndex = transition.ColumnBufferIndex;
        if (currentColumnBufferIndex >= TransitionColumnCount)
        {
            throw new InvalidOperationException(
                "sub_1104A expects the transition column-buffer index to stay within 0..299.");
        }

        var currentColumnBufferOffset = checked(currentColumnBufferIndex * TransitionColumnHeight);

        // IDA 0x11052..0x110CF: when the current glyph-column countdown reaches zero, acquire the next active
        // transition-text stream, normalize the next byte into a FONT_VGA glyph index, and clear the current 8-row
        // column slot in the circular buffer.
        if (transition.RemainingGlyphColumns == 0)
        {
            if (!transition.HasActiveTokenText)
            {
                PromotePendingTransitionTokenCursor(transition);
            }
            else
            {
                ReadNextTransitionGlyph(transition);
            }

            Array.Clear(transition.ColumnBuffer, currentColumnBufferOffset, TransitionColumnHeight);
        }
        else
        {
            // IDA 0x11134..0x1122D: consume one published FONT_VGA glyph-column mask byte and expand its bits into the
            // current 8-row transition column buffer entry.
            transition.RemainingGlyphColumns--;
            WriteGlyphColumnIntoTransitionBuffer(transition, currentColumnBufferOffset);
        }

        // IDA 0x1122D..0x12FD: advance the 300-column circular buffer index, then redraw the 300x8 state-transition
        // strip into the current work buffer.
        transition.ColumnBufferIndex = (ushort)((transition.ColumnBufferIndex + 1) % TransitionColumnCount);
        RedrawTransitionBand(transition);
    }

    private void PromotePendingTransitionTokenCursor(TransitionEffectCompatibilityState transition)
    {
        if (transition.HasActiveTokenText)
        {
            return;
        }

        if (transition.HasPendingDynamicTokenText)
        {
            transition.SetActiveDynamicTokenText(transition.GetPendingDynamicTokenTextOrThrow().ToArray());
        }
        else
        {
            transition.SetActiveTokenText(transition.PendingTokenText);
        }

        transition.ActiveTokenTextIndex = 0;
        if (!transition.HasActiveTokenText)
        {
            transition.EnabledFlag = 0;
            return;
        }

        transition.ClearPendingTokenText();
        LogTransitionEffectCursorState("sub_1104A promote-pending", transition, StringId.None);
    }

    private void ReadNextTransitionGlyph(TransitionEffectCompatibilityState transition)
    {
        if (!transition.HasActiveTokenText)
        {
            return;
        }

        var streamByte = ReadTransitionTextByte(transition, transition.ActiveTokenTextIndex);
        transition.ActiveTokenTextIndex++;
        transition.GlyphIndex = streamByte;
        if (streamByte == 0)
        {
            transition.ClearActiveTokenText();
            transition.ActiveTokenTextIndex = 0;
            transition.GlyphIndex = 0;
            transition.ClearGlyphColumnMaskIndex();
            LogTransitionEffectCursorState("sub_1104A stream-end", transition, StringId.None);
            return;
        }

        var glyphIndex = unchecked((byte)(streamByte - 0x20));
        var glyphAdvanceColumns = state.Presentation.Font.GlyphAdvanceColumns;
        if (glyphIndex >= glyphAdvanceColumns.Length)
        {
            throw new InvalidOperationException(
                $"sub_1104A read transition glyph byte 0x{streamByte:X2}, which does not map to a published FONT_VGA glyph.");
        }

        transition.GlyphIndex = glyphIndex;
        transition.RemainingGlyphColumns = glyphAdvanceColumns[glyphIndex];
        transition.SetGlyphColumnMaskIndex((ushort)(glyphIndex * TransitionColumnHeight));
    }

    private void WriteGlyphColumnIntoTransitionBuffer(TransitionEffectCompatibilityState transition,
        int currentColumnBufferOffset)
    {
        var font = state.Presentation.Font;
        var glyphColumnMaskIndex = transition.GetGlyphColumnMaskIndexOrThrow();
        if ((uint)glyphColumnMaskIndex >= font.GlyphColumnMasks.Length)
        {
            throw new InvalidOperationException(
                "sub_1104A expects the glyph-column-mask cursor to stay within the published FONT_VGA mask table.");
        }

        var glyphColumnMask = font.GlyphColumnMasks[glyphColumnMaskIndex];
        transition.AdvanceGlyphColumnMaskIndex();

        var columnBuffer = transition.ColumnBuffer;
        for (var rowIndex = 0; rowIndex < TransitionColumnHeight; rowIndex++)
        {
            var rowMask = (byte)(0x80 >> rowIndex);
            columnBuffer[currentColumnBufferOffset + rowIndex] =
                (glyphColumnMask & rowMask) != 0 ? byte.MaxValue : (byte)0;
        }
    }

    private void RedrawTransitionBand(TransitionEffectCompatibilityState transition)
    {
        var workBuffer = state.Presentation.Display.GetWorkBuffer();
        EnsureTransitionBandFits(workBuffer);

        var columnBuffer = transition.ColumnBuffer;
        var leadingColumnIndex = transition.ColumnBufferIndex;
        for (var rowIndex = 0; rowIndex < TransitionColumnHeight; rowIndex++)
        {
            var destinationRow = workBuffer.GetRowSpan(TransitionBandTopRow + rowIndex)
                .Slice(TransitionBandLeftColumn, TransitionColumnCount);

            for (var columnIndex = 0; columnIndex < TransitionColumnCount; columnIndex++)
            {
                var sourceColumnIndex = (leadingColumnIndex + columnIndex) % TransitionColumnCount;
                destinationRow[columnIndex] = columnBuffer[sourceColumnIndex * TransitionColumnHeight + rowIndex];
            }
        }

        state.Presentation.Screen.Invalidate(new IntRect(TransitionBandLeftColumn,
            TransitionBandTopRow,
            TransitionColumnCount,
            TransitionColumnHeight));
    }

    private byte ReadTransitionTextByte(TransitionEffectCompatibilityState transition, int byteIndex)
    {
        var bytes = transition.HasActiveDynamicTokenText
            ? transition.GetActiveDynamicTokenTextOrThrow()
            : strings.GetCp437String(transition.ActiveTokenText);
        return (uint)byteIndex >= bytes.Length
            ? throw new InvalidOperationException("sub_1104A transition-text cursor exceeded the text buffer length.")
            : bytes[byteIndex];
    }

    private static void EnsureTransitionBandFits(Surface workBuffer)
    {
        if (TransitionBandTopRow < 0 || TransitionBandLeftColumn < 0 ||
            TransitionBandTopRow + TransitionColumnHeight > workBuffer.Height ||
            TransitionBandLeftColumn + TransitionColumnCount > workBuffer.Width)
        {
            throw new InvalidOperationException(
                "sub_1104A transition band does not fit within the retained work buffer.");
        }
    }

    private void LogTransitionEffectCursorState(string site,
        TransitionEffectCompatibilityState transition,
        StringId incomingText)
    {
        var programStateId = state.RawDataBlock.Control.ProgramStateId;
        if (programStateId is not 0x0001 and not 0x0003)
        {
            return;
        }

        GameLog.Debug(LoggingChannel.Runtime,
            $"{site} state=0x{programStateId:X4} incoming={FormatStringId(incomingText)} active={FormatStringId(transition.ActiveTokenText)} pending={FormatStringId(transition.PendingTokenText)} enabled=0x{transition.EnabledFlag:X2} remainingCols=0x{transition.RemainingGlyphColumns:X2} preview={ReadTransitionCursorPreview(transition.ActiveTokenText, transition.ActiveTokenTextIndex)}");
    }

    private string ReadTransitionCursorPreview(StringId stringId, int textIndex)
    {
        if (state.TransitionEffect.HasActiveDynamicTokenText)
        {
            var dynamicBytes = state.TransitionEffect.GetActiveDynamicTokenTextOrThrow();
            if ((uint)textIndex >= dynamicBytes.Length)
            {
                return "<out-of-range>";
            }

            var dynamicEndOffset = textIndex;
            while (dynamicEndOffset < dynamicBytes.Length && dynamicBytes[dynamicEndOffset] != 0 &&
                   dynamicEndOffset - textIndex < 48)
            {
                dynamicEndOffset++;
            }

            return dynamicEndOffset == textIndex
                ? "\"\""
                : FormatTextPreview(dynamicBytes.Slice(textIndex, dynamicEndOffset - textIndex));
        }

        if (stringId == StringId.None)
        {
            return "\"\"";
        }

        var bytes = strings.GetCp437String(stringId);
        if ((uint)textIndex >= bytes.Length)
        {
            return "<out-of-range>";
        }

        var endOffset = textIndex;
        while (endOffset < bytes.Length && bytes[endOffset] != 0 && endOffset - textIndex < 48)
        {
            endOffset++;
        }

        return endOffset == textIndex ? "\"\"" : FormatTextPreview(bytes.Slice(textIndex, endOffset - textIndex));
    }

    private string FormatStringId(StringId stringId)
    {
        return stringId == StringId.None ? "null" : strings.FormatStringId(stringId);
    }

    private static string FormatTextPreview(ReadOnlySpan<byte> textBytes)
    {
        const int maxPreviewLength = 48;

        var previewBuilder = new StringBuilder();
        previewBuilder.Append('"');
        var previewLength = Math.Min(textBytes.Length, maxPreviewLength);
        for (var index = 0; index < previewLength; index++)
        {
            var textByte = textBytes[index];
            previewBuilder.Append(textByte is >= 0x20 and <= 0x7E ? (char)textByte : '.');
        }

        if (textBytes.Length > maxPreviewLength)
        {
            previewBuilder.Append("...");
        }

        previewBuilder.Append('"');
        previewBuilder.Append(" hex=");

        var hexLength = Math.Min(textBytes.Length, 16);
        for (var index = 0; index < hexLength; index++)
        {
            if (index > 0)
            {
                previewBuilder.Append(' ');
            }

            previewBuilder.Append(textBytes[index].ToString("X2"));
        }

        return previewBuilder.ToString();
    }
}
