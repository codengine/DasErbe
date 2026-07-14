using System.Collections.Frozen;
using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Rooms.Basement;

/// <summary>
///     Owns basement interaction handlers and their local blocking helpers.
/// </summary>
/// <param name="runtime">Runtime owner that provides shared prompt state and session data.</param>
internal sealed class BasementRoom(Erbe runtime)
{
    private static readonly FrozenDictionary<InteractionHandlerId, StringId> SimpleTextHandlers =
        new Dictionary<InteractionHandlerId, StringId>
        {
            { InteractionHandlerId.Basement_InspectElectricHeater, StringId.Basement_ElectricHeaterInspection },
            { InteractionHandlerId.Basement_UseFireExtinguisher, StringId.Basement_FireExtinguisherUse },
            { InteractionHandlerId.Basement_InspectOutlet, StringId.Basement_OutletInspection }
        }.ToFrozenDictionary();

    private readonly BasementElectricHeaterHandler _electricHeaterHandler = new(runtime);

    /// <summary>
    ///     Dispatches one basement-owned handler directly to completion.
    /// </summary>
    /// <param name="handlerId">Resolved handler identifier from the interaction descriptor.</param>
    internal bool TryDispatchInteraction(InteractionHandlerId handlerId)
    {
        if (SimpleTextHandlers.TryGetValue(handlerId, out var stringId))
        {
            runtime.PromptController.RunTextAnimation(stringId);
            return true;
        }

        switch (handlerId)
        {
            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.Basement_UseElectricHeaterOnOutlet:
                _electricHeaterHandler.RunUseOutletOnElectricHeater();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.Basement_InspectFireExtinguisher:
                DispatchInspectFireExtinguisher();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.Basement_InspectHeatingSystem:
                DispatchInspectHeatingSystem();
                return true;

            default:
                return false;
        }
    }

    [FunctionSymbol("sub_12BA4", 0x12BA4)]
    private void DispatchInspectFireExtinguisher()
    {
        // IDA 0x12BA7..0x12BB8: choose between the primary and alternate fire-extinguisher inspection lines from the
        // reviewed heating-basement fire-extinguisher state word before entering the shared timed transition-text
        // seam.
        var fireExtinguisherState =
            runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BasementFireExtinguisherRecord];
        var stringId = fireExtinguisherState.State == StateId.Broken
            ? StringId.Basement_FireExtinguisherInspection
            : StringId.Basement_FireExtinguisherAlternateInspection;

        // IDA 0x12BB8..0x12BBF: forward the selected prompt text through the shared timed transition-text seam
        // sub_11A71.
        runtime.PromptController.RunTextAnimation(stringId);
    }

    [FunctionSymbol("sub_12BC0", 0x12BC0)]
    private void DispatchInspectHeatingSystem()
    {
        // IDA 0x12BC3..0x12BD4: choose between the primary and alternate heating-system inspection lines from the
        // reviewed heating-basement heating-system state word before entering the shared timed transition-text seam.
        var heatingSystemState =
            runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BasementHeatingSystemRecord];
        var stringId = heatingSystemState.State == StateId.Broken
            ? StringId.Basement_HeatingSystemInspection
            : StringId.Basement_HeatingSystemAlternateInspection;

        // IDA 0x12BD4..0x12BDB: forward the selected prompt text through the shared timed transition-text seam
        // sub_11A71.
        runtime.PromptController.RunTextAnimation(stringId);
    }
}
