using Game.Text;

namespace Game.Runtime.Overlays.Inventory;

/// <summary>
///     Owns the blocking inventory whisk-and-bowl success helper.
/// </summary>
/// <param name="runtime">Runtime owner that provides prompt state and persistent selection tables.</param>
internal sealed class WhiskWithBowlSuccessHandler(Erbe runtime)
{
    private const ushort CreamAndBowlPreparationTextEntryIndex = 0x0004;
    private const byte CreamAndBowlPreparedState = 0x02;
    private const byte CreamAndBowlConsumedState = 0x03;

    /// <summary>
    ///     Runs the whisk-and-bowl success helper to completion.
    /// </summary>
    internal void RunTransitionTextEffect()
    {
        var entryStates = runtime.State.RawDataBlock.Control.TransitionTextEntryStates;

        // IDA 0x13C22..0x13C27: only the staged cream-and-bowl state value 0x02 enters the whisk success path; all
        // other states return immediately without side effects.
        if (entryStates[CreamAndBowlPreparationTextEntryIndex] != CreamAndBowlPreparedState)
        {
            return;
        }

        // IDA 0x13C29..0x13C2E: promote the cream-and-bowl staged state to 0x03 and republish entry 0x0004 as the
        // current start index before the success prompt runs.
        entryStates[CreamAndBowlPreparationTextEntryIndex] = CreamAndBowlConsumedState;
        runtime.State.RawDataBlock.Control.TransitionTextStartIndex = (byte)CreamAndBowlPreparationTextEntryIndex;

        // IDA 0x13C33..0x13C3C: block through the fixed "Erste Sahne!" line via the shared timed transition-text
        // seam sub_11A71.
        runtime.PromptController.RunTextAnimation(StringId.Inventory_WhiskWithBowlSuccess);

        // IDA 0x13C3D..0x13C3D: material side effect: publish the whipped-cream milestone in the shared
        // Lolita-heart progress tracker only after the success prompt returns.
        runtime.State.RawDataBlock.Control.LolitaProgress.MarkWhippedCreamPrepared();
    }
}
