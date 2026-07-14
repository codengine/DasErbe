using Game.Shared.RE;
using Game.State;

namespace Game.Text;

/// <summary>
///     Publishes the decoded <c>FONT_VGA</c> atlas into the runtime-owned glyph tables consumed by later text rendering
///     routines.
/// </summary>
/// <param name="font">The font</param>
internal sealed class FontPublisher(FontCompatibilityState font)
{
    private const int AtlasStrideBytes = 320;
    private const int GlyphColumnsPerRow = 40;
    private const byte FontInkColor = 7;
    private const byte NormalizedInkColor = 1;
    private const byte SpaceGlyphAdvanceColumns = 7;

    private const int RequiredGlyphCellReadableBytes = FontCompatibilityState.GlyphColumnCount +
                                                       (FontCompatibilityState.GlyphColumnCount - 1) * AtlasStrideBytes;

    /// <summary>
    ///     Publishes the decoded <c>FONT_VGA</c> atlas into the runtime-owned glyph tables.
    /// </summary>
    /// <param name="decodedFontAtlas">Decoded <c>FONT_VGA</c> atlas bytes.</param>
    [FunctionSymbol("sub_10B5C", 0x10B5C)]
    internal void PublishDecodedFontAtlas(byte[] decodedFontAtlas)
    {
        var glyphAdvanceColumns = font.GlyphAdvanceColumns;
        var glyphColumnMasks = font.GlyphColumnMasks;

        // IDA 0x10B62..0x10C30: walk the 128 fixed glyph cells in the decoded FONT_VGA atlas,
        // convert each 8x8 cell into eight packed column masks, and publish the effective glyph width.
        for (var glyphIndex = 0; glyphIndex < FontCompatibilityState.GlyphCount; glyphIndex++)
        {
            var glyphColumnOffset = glyphIndex % GlyphColumnsPerRow * FontCompatibilityState.GlyphColumnCount;
            var glyphRowOffset = glyphIndex / GlyphColumnsPerRow * FontCompatibilityState.GlyphColumnCount *
                                 AtlasStrideBytes;
            var glyphSourceOffset = glyphColumnOffset + glyphRowOffset;
            var glyphMaskOffset = glyphIndex * FontCompatibilityState.GlyphColumnCount;

            EnsureReadable(decodedFontAtlas,
                glyphSourceOffset,
                RequiredGlyphCellReadableBytes,
                "decoded FONT_VGA glyph cell");

            glyphAdvanceColumns[glyphIndex] = 0;
            for (var glyphColumnIndex = 0;
                 glyphColumnIndex < FontCompatibilityState.GlyphColumnCount;
                 glyphColumnIndex++)
            {
                var sourceOffset = glyphSourceOffset + glyphColumnIndex;
                byte packedColumnMask = 0;

                for (var rowIndex = 0; rowIndex < FontCompatibilityState.GlyphColumnCount; rowIndex++)
                {
                    var atlasOffset = sourceOffset + rowIndex * AtlasStrideBytes;
                    if (decodedFontAtlas[atlasOffset] != FontInkColor)
                    {
                        continue;
                    }

                    decodedFontAtlas[atlasOffset] = NormalizedInkColor;
                    packedColumnMask |= (byte)(0x80 >> rowIndex);
                }

                glyphColumnMasks[glyphMaskOffset + glyphColumnIndex] = packedColumnMask;
                if (packedColumnMask != 0)
                {
                    glyphAdvanceColumns[glyphIndex] = (byte)(glyphColumnIndex + 1);
                }
            }
        }

        // IDA 0x10C33: force glyph 0 (space) to a 7-column advance even though its atlas cell is blank.
        glyphAdvanceColumns[0] = SpaceGlyphAdvanceColumns;
    }

    private static void EnsureReadable(byte[] buffer, int offset, int length, string description)
    {
        if (offset < 0 || length < 0 || offset > buffer.Length - length)
        {
            throw new InvalidDataException($"FONT_VGA atlas is truncated while reading {description}.");
        }
    }
}
