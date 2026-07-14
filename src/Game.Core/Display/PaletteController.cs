using Game.Shared.RE;
using Game.Shared.Rendering;
using Game.State;

namespace Game.Display;

/// <summary>
///     Session-owned palette upload and fade helpers for carried-over display startup symbols.
/// </summary>
/// <param name="state">Live runtime state.</param>
internal sealed class PaletteController(RuntimeState state)
{
    private const ushort PaletteDacTableOffset = 0x5D03;
    private const int PaletteEntryCount = 0x100;
    private const int PaletteByteCount = PaletteEntryCount * 3;

    /// <summary>
    ///     Uploads the full runtime DAC table into the presentation palette.
    /// </summary>
    /// <param name="paletteDacTable">0x300-byte DAC table encoded as 6-bit RGB triplets.</param>
    [FunctionSymbol("sub_102D7", 0x102D7, FunctionFlags.AdaptedForMonoGame)]
    internal void UploadFullPalette(ReadOnlySpan<byte> paletteDacTable)
    {
        // IDA 0x102DA: sub_1022F polls VGA retrace; obsolete in the host/frame-clock driven port.
        // IDA 0x102DD..0x102ED: publish the full 0..0xFF palette range through the now-inlined sub_102AC.
        if (paletteDacTable.Length < PaletteByteCount)
        {
            throw new ArgumentException("Palette DAC table does not contain the full upload range.",
                nameof(paletteDacTable));
        }

        Span<Rgba32> colors = stackalloc Rgba32[PaletteEntryCount];

        // IDA 0x102B1..0x102D6: write the full inclusive VGA DAC range. The managed runtime preserves the 6-bit DAC
        // values while adapting the hardware port IO to the runtime-owned RGBA palette.
        for (var paletteIndex = 0; paletteIndex < PaletteEntryCount; paletteIndex++)
        {
            var sourceOffset = paletteIndex * 3;
            colors[paletteIndex] = new Rgba32(ExpandDacChannel(paletteDacTable[sourceOffset]),
                ExpandDacChannel(paletteDacTable[sourceOffset + 1]),
                ExpandDacChannel(paletteDacTable[sourceOffset + 2]));
        }

        state.Presentation.Palette.SetAll(colors);
    }

    private ReadOnlyMemory<byte> ReadPaletteDacTable(ushort paletteTableOffset)
    {
        if (paletteTableOffset != PaletteDacTableOffset)
        {
            throw new InvalidOperationException(
                $"Palette fade helpers currently expect the carried-over paletteDacTable at DGROUP offset 0x{PaletteDacTableOffset:X4}, not 0x{paletteTableOffset:X4}.");
        }

        // The startup-title palette callers pass DS:5D03, which is the runtime DAC backing table populated from LBM
        // CMAP chunks rather than immutable executable image bytes.
        return state.Presentation.Display.PaletteDacTable;
    }

    /// <summary>
    ///     Applies one startup fade step from the runtime DAC table into the presentation palette.
    /// </summary>
    /// <param name="paletteTableOffset">Expected DGROUP offset of the runtime title DAC table.</param>
    /// <param name="fadeStep">Fade multiplier used by the carried-over palette fade loops.</param>
    internal void ApplyPaletteFadeStepFromDacTable(ushort paletteTableOffset, ushort fadeStep)
    {
        var sourcePaletteDacTable = ReadPaletteDacTable(paletteTableOffset);

        // IDA 0x102F2..0x10340 and 0x10341..0x1038E: both title fade loops scale the runtime DAC table for
        // one fade step and publish the full 0..0xFF palette range. The original retrace polling through sub_1022F is
        // obsolete in the host/frame-clock driven port.
        UploadScaledFullPalette(sourcePaletteDacTable.Span, fadeStep);
    }

    private void UploadScaledFullPalette(ReadOnlySpan<byte> sourcePaletteDacTable, ushort fadeStep)
    {
        if (sourcePaletteDacTable.Length < PaletteByteCount)
        {
            throw new ArgumentException("Palette fade source table does not contain the full upload range.",
                nameof(sourcePaletteDacTable));
        }

        Span<Rgba32> colors = stackalloc Rgba32[PaletteEntryCount];

        // IDA 0x10306..0x10328 and 0x10355..0x10377: preserve the original (dacByte * fadeStep) >> 7 byte math.
        // The managed runtime owns the RGBA palette directly, so it skips the temporary scaled DAC table and writes the
        // final color span in one pass before the sub_102D7-equivalent full-range publication side effect.
        for (var paletteIndex = 0; paletteIndex < PaletteEntryCount; paletteIndex++)
        {
            var sourceOffset = paletteIndex * 3;
            colors[paletteIndex] = new Rgba32(ScaleAndExpandDacChannel(sourcePaletteDacTable[sourceOffset], fadeStep),
                ScaleAndExpandDacChannel(sourcePaletteDacTable[sourceOffset + 1], fadeStep),
                ScaleAndExpandDacChannel(sourcePaletteDacTable[sourceOffset + 2], fadeStep));
        }

        state.Presentation.Palette.SetAll(colors);
    }

    private static byte ExpandDacChannel(byte channel)
    {
        return (byte)(channel << 2);
    }

    private static byte ScaleAndExpandDacChannel(byte channel, ushort fadeStep)
    {
        var scaledChannel = (byte)((channel * fadeStep) >> 7);
        return ExpandDacChannel(scaledChannel);
    }
}
