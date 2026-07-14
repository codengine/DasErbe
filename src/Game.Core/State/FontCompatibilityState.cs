using Game.Shared.RE;

namespace Game.State;

internal sealed class FontCompatibilityState
{
    internal const int GlyphCount = 0x80;
    internal const int GlyphColumnCount = 8;

    /// <summary>
    ///     Per-glyph advance column counts published from the decoded <c>FONT_VGA</c> atlas.
    /// </summary>
    [GlobalSymbol("byte_1D3F4", 0x1D3F4, GlobalFlags.BufferOwner | GlobalFlags.CanonicalOwner)]
    internal byte[] GlyphAdvanceColumns { get; } = new byte[GlyphCount];

    /// <summary>
    ///     Packed 8-byte column masks for each published glyph in the decoded <c>FONT_VGA</c> atlas.
    /// </summary>
    [GlobalSymbol("byte_1D474", 0x1D474, GlobalFlags.BufferOwner | GlobalFlags.CanonicalOwner)]
    internal byte[] GlyphColumnMasks { get; } = new byte[GlyphCount * GlyphColumnCount];
}
