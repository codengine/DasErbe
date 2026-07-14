using Game.Shared.RE;
using Game.Shared.Rendering;
using Game.State;

namespace Game.Text;

/// <summary>
///     Session-owned FONT_VGA text rendering and measurement helpers for carried-over text symbols.
/// </summary>
/// <param name="state">Live runtime state.</param>
/// <param name="strings">String catalog used for catalog-backed rendering.</param>
/// <param name="textCursor">Text cursor controller used for CR/LF row advancement.</param>
internal sealed class TextRendererController(RuntimeState state,
    GameStringCatalog strings,
    TextCursorController textCursor)
{
    private const int GlyphHeight = 8;
    private const byte FirstRenderableCharacter = 0x20;

    /// <summary>
    ///     Measures the rendered width of a null-terminated text byte stream.
    /// </summary>
    /// <param name="textBytes">Null-terminated text byte stream.</param>
    [FunctionSymbol("sub_1161D", 0x1161D)]
    internal ushort MeasureTextWidthPixels(ReadOnlySpan<byte> textBytes)
    {
        ushort widthPixels = 0;

        // IDA 0x11623..0x11650: walk the caller-owned null-terminated text byte stream and accumulate
        // glyphAdvanceColumns + 1 for every renderable byte at or above 0x20.
        foreach (var textByte in textBytes)
        {
            switch (textByte)
            {
                case 0:
                    return widthPixels;
                case < FirstRenderableCharacter:
                    continue;
                default:
                    widthPixels += (ushort)(GetGlyphAdvanceColumns(textByte) + 1);
                    break;
            }
        }

        throw new InvalidOperationException("sub_1161D requires a null-terminated text byte stream.");
    }

    /// <summary>
    ///     Measures the rendered width of a catalog-backed string.
    /// </summary>
    /// <param name="textId">String id to measure.</param>
    internal ushort MeasureStringWidthPixels(StringId textId)
    {
        return MeasureTextWidthPixels(strings.GetCp437String(textId));
    }

    /// <summary>
    ///     Renders a null-terminated text byte stream with the published FONT_VGA state.
    /// </summary>
    /// <param name="textBytes">Null-terminated text byte stream.</param>
    [FunctionSymbol("sub_11657", 0x11657, FunctionFlags.AdaptedForMonoGame)]
    internal void RenderTextBlock(ReadOnlySpan<byte> textBytes)
    {
        // IDA 0x1165C..0x11662: empty text returns immediately. The managed runtime treats an empty byte stream or a
        // leading terminator as the no-text sentinel for this caller-owned text slice.
        if (textBytes.IsEmpty || textBytes[0] == 0)
        {
            return;
        }

        // IDA 0x11664..0x11697: walk the caller-owned null-terminated text byte stream. LF and CR both advance to the
        // next text row, while any other byte enters the shared glyph-rendering seam via sub_10C3C.
        foreach (var textByte in textBytes)
        {
            if (textByte == 0)
            {
                return;
            }

            switch (textByte)
            {
                case 0x0A:
                case 0x0D:
                    textCursor.AdvanceTextBlockCursorToNextRow();
                    break;

                default:
                    DrawTextGlyphAtCursor(textByte);
                    break;
            }
        }

        throw new InvalidOperationException("sub_11657 requires a null-terminated text byte stream.");
    }

    /// <summary>
    ///     Renders a catalog-backed string with the published FONT_VGA state.
    /// </summary>
    /// <param name="textId">String id to render.</param>
    internal void RenderStringBlock(StringId textId)
    {
        RenderTextBlock(strings.GetCp437String(textId));
    }

    /// <summary>
    ///     Draws one renderable FONT_VGA glyph at the current text cursor.
    /// </summary>
    /// <param name="character">Character code to render via the published FONT_VGA glyph tables.</param>
    [FunctionSymbol("sub_10C3C", 0x10C3C, FunctionFlags.AdaptedForMonoGame)]
    private void DrawTextGlyphAtCursor(byte character)
    {
        // IDA 0x10C44..0x10C4A: control bytes below 0x20 are ignored here; CR/LF are handled by the caller.
        if (character < 0x20)
        {
            return;
        }

        var glyphIndex = unchecked((byte)(character + 0xE0));
        if (glyphIndex >= FontCompatibilityState.GlyphCount)
        {
            throw new InvalidOperationException(
                $"Character byte 0x{character:X2} falls outside the published FONT_VGA glyph table contract.");
        }

        var presentation = state.Presentation;
        var text = presentation.Text;
        var font = presentation.Font;
        var glyphAdvanceColumns = font.GlyphAdvanceColumns[glyphIndex];

        // IDA 0x10C95..0x10C9E: zero-width glyphs are skipped without touching the cursor.
        if (glyphAdvanceColumns == 0)
        {
            return;
        }

        var destinationSurface = presentation.Display.GetWorkBuffer();
        var destinationColumn = text.TextCursorColumn;
        var destinationRow = text.TextCursorRow;
        EnsureGlyphFits(destinationSurface, destinationColumn, destinationRow, glyphAdvanceColumns);

        // IDA 0x10CA0..0x10CEC: draw the packed FONT_VGA column masks into the current work buffer using the
        // current text ink color, then advance the cursor by glyph width + 1. The managed runtime adapts the original
        // planar map-mask writes into direct indexed-pixel writes on the retained work buffer.
        var glyphMaskOffset = glyphIndex * FontCompatibilityState.GlyphColumnCount;
        var inkColor = presentation.Display.TextInkColorIndex;
        for (var glyphColumnIndex = 0; glyphColumnIndex < glyphAdvanceColumns; glyphColumnIndex++)
        {
            var columnMask = font.GlyphColumnMasks[glyphMaskOffset + glyphColumnIndex];
            if (columnMask == 0)
            {
                continue;
            }

            var rowSpanColumn = destinationColumn + glyphColumnIndex;
            for (var rowIndex = 0; rowIndex < GlyphHeight; rowIndex++)
            {
                if ((columnMask & (0x80 >> rowIndex)) == 0)
                {
                    continue;
                }

                destinationSurface.GetRowSpan(destinationRow + rowIndex)[rowSpanColumn] = inkColor;
            }
        }

        text.TextCursorColumn += (ushort)(glyphAdvanceColumns + 1);
        state.Presentation.Screen.Invalidate(new IntRect(destinationColumn,
            destinationRow,
            glyphAdvanceColumns,
            GlyphHeight));
    }

    private byte GetGlyphAdvanceColumns(byte character)
    {
        if (character < FirstRenderableCharacter)
        {
            return 0;
        }

        var glyphIndex = character - FirstRenderableCharacter;
        var glyphAdvanceColumns = state.Presentation.Font.GlyphAdvanceColumns;
        if (glyphIndex >= glyphAdvanceColumns.Length)
        {
            throw new InvalidOperationException(
                $"Character byte 0x{character:X2} falls outside the published FONT_VGA glyph table contract.");
        }

        return glyphAdvanceColumns[glyphIndex];
    }

    private static void EnsureGlyphFits(Surface destinationSurface,
        ushort destinationColumn,
        ushort destinationRow,
        byte glyphAdvanceColumns)
    {
        if (destinationColumn + glyphAdvanceColumns > destinationSurface.Width)
        {
            throw new InvalidOperationException(
                $"Text glyph columns [{destinationColumn}, {destinationColumn + glyphAdvanceColumns}) exceed the work buffer width.");
        }

        if (destinationRow + GlyphHeight > destinationSurface.Height)
        {
            throw new InvalidOperationException(
                $"Text glyph rows [{destinationRow}, {destinationRow + GlyphHeight}) exceed the work buffer height.");
        }
    }
}
