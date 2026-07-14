using System.Collections.Frozen;
using Game.Catalogs;
using Game.DataBlock.Control;
using Game.DataBlock.Interaction;
using Game.DataBlock.Selection;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Rooms.House;

internal sealed class HouseRoom(Erbe runtime)
{
    private const int CarKeyCollectedStateIndex = 0x0F;
    private const ushort HouseInspectionBaseVariantIndex = 0x0006;
    private const ushort HouseInspectionChangedVariantIndex = 0x0007;
    private const ushort HouseInspectionResolvedVariantIndex = 0x0008;
    private const ushort BicyclePumpTakeTextEntryIndex = 0x000B;

    private static readonly FrozenDictionary<InteractionHandlerId, StringId> SimpleTextHandlers =
        new Dictionary<InteractionHandlerId, StringId>
        {
            { InteractionHandlerId.House_OpenGarage, StringId.House_GarageOpenFailure },
            { InteractionHandlerId.House_InspectBicyclePump, StringId.House_BicyclePumpDescription }
        }.ToFrozenDictionary();

    private readonly HouseBicycleHandler _bicycleHandler = new(runtime);
    private readonly HouseBusRideHandler _busRideHandler = new(runtime);
    private readonly HouseFriendlyManHandler _friendlyManHandler = new(runtime);

    // InteractionDescriptorRecord[0x3E] stores the noun shown by the house garage inspection hotspot: "Garage" while
    // the garage is closed, then "Auto" after the living-room button opens it.
    [GlobalSymbol("garageInspectionTargetTextOffset", 0x17A3A)]
    private StringId GarageInspectionText
    {
        set =>
            runtime.Interactions.SetInteractiveSelectionText(
                InteractionDescriptorRef.InteractiveRegion(InteractionDescriptorCatalog.GarageInspectionTargetRecord),
                value);
    }

    internal bool TryDispatchInteraction(InteractionHandlerId handlerId)
    {
        if (SimpleTextHandlers.TryGetValue(handlerId, out var stringId))
        {
            runtime.PromptController.RunTextAnimation(stringId);
            return true;
        }

        switch (handlerId)
        {
            // handlerByteOffset +0x0C / controlMask 0x0008
            // linearAddress: 0x11BAA
            case InteractionHandlerId.House_BusStopNoOp:
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.House_InspectHouse:
                DispatchInspectHouse();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.House_InspectGarage:
                DispatchInspectGarage();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.House_InspectBusStop:
                DispatchInspectBusStop();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.House_UseBusStop:
                _busRideHandler.RunBusRide();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.House_UseGarage:
                DispatchUseGarage();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.House_InspectBicycle:
                DispatchInspectBicycle();
                return true;

            // handlerByteOffset +0x06 / controlMask 0x0004
            case InteractionHandlerId.House_OpenBicycleLock:
                _bicycleHandler.RunUnlockWithKeypad();
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.House_UseBicycle:
                DispatchUseBicycle();
                return true;

            // handlerByteOffset +0x02 / controlMask 0x0020
            case InteractionHandlerId.House_TakeBicyclePump:
                runtime.PromptController.SelectTransitionTextEntry(BicyclePumpTakeTextEntryIndex);
                return true;

            // handlerByteOffset +0x04 / controlMask 0x0002
            case InteractionHandlerId.House_UseBicyclePumpOnBicycle:
                DispatchUseBicyclePumpOnBicycle();
                return true;

            // handlerByteOffset +0x00 / controlMask 0x0001
            case InteractionHandlerId.House_InspectFriendlyMan:
                _friendlyManHandler.RunInspectFriendlyMan();
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    ///     Toggles the exterior garage-vehicle state bundle published by the interior house switch.
    /// </summary>
    internal void ToggleGarageVehicleStateFromInteriorSwitch()
    {
        var selectionTable = runtime.State.RawDataBlock.SelectionTable;
        var carState = selectionTable[SelectionEntryCatalog.Garage];

        switch (carState.State)
        {
            case StateId.House.CarReady:
                carState.State = StateId.Default;
                selectionTable[SelectionEntryCatalog.HouseSwitch].State = StateId.Default;
                selectionTable[SelectionEntryCatalog.CarKeyItem].StateId = StateId.Default;
                selectionTable[SelectionEntryCatalog.Garage].StateId = StateId.Default;
                GarageInspectionText = StringId.Panel_LabelGarage;
                return;
            case StateId.Default:
                carState.State = StateId.House.CarReady;
                selectionTable[SelectionEntryCatalog.HouseSwitch].State = StateId.House.CarReady;
                selectionTable[SelectionEntryCatalog.CarKeyItem].StateId = StateId.House.GarageVehicleVariantCar;
                selectionTable[SelectionEntryCatalog.Garage].StateId = StateId.House.GarageVehicleVariantCar;
                GarageInspectionText = StringId.Panel_LabelCar;
                break;
        }
    }

    /// <summary>
    ///     Runs the house-side inspection text chosen from the story-progress subset that tracks house condition.
    /// </summary>
    [FunctionSymbol("sub_11BCD", 0x11BCD)]
    private void DispatchInspectHouse()
    {
        var houseInspectionVariant =
            runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.HouseConditionRecord];

        var inspectionProgress = runtime.State.RawDataBlock.Control.StoryProgress.GetHouseConditionInspectionProgress();
        houseInspectionVariant.State = inspectionProgress switch
        {
            HouseConditionInspectionProgress.Complete => HouseInspectionResolvedVariantIndex,
            HouseConditionInspectionProgress.Partial => HouseInspectionChangedVariantIndex,
            _ => houseInspectionVariant.State
        };

        switch (houseInspectionVariant.State)
        {
            case HouseInspectionBaseVariantIndex:
                runtime.PromptController.RunTextAnimation(StringId.House_VillaConditionBase);
                return;
            case HouseInspectionChangedVariantIndex:
                runtime.PromptController.RunTextAnimation(StringId.House_VillaConditionChanged);
                return;
            case HouseInspectionResolvedVariantIndex:
                runtime.PromptController.RunTextAnimation(StringId.House_VillaConditionResolved);
                return;
            default:
                return;
        }
    }

    /// <summary>
    ///     Runs the garage-side text animation chosen from the recovered garage-vehicle state.
    /// </summary>
    [FunctionSymbol("sub_11C26", 0x11C26)]
    private void DispatchInspectGarage()
    {
        var garageVehicleState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.Garage];
        var garagePrompt = garageVehicleState.State == StateId.House.CarReady
            ? StringId.House_GarageReadyVehicle
            : StringId.House_GarageDescription;
        runtime.PromptController.RunTextAnimation(garagePrompt);
    }

    /// <summary>
    ///     Runs the bus-stop inspection text chosen from the current program state.
    /// </summary>
    [FunctionSymbol("sub_11E3D", 0x11E3D)]
    private void DispatchInspectBusStop()
    {
        var busStopPrompt = runtime.State.RawDataBlock.Control.ProgramStateId == 0x0005
            ? StringId.House_BusStopTooFar
            : StringId.House_BusStopRouteDescription;
        runtime.PromptController.RunTextAnimation(busStopPrompt);
    }

    /// <summary>
    ///     Runs the garage-use text animation when the repaired car and collected key are both present.
    /// </summary>
    [FunctionSymbol("sub_11C43", 0x11C43)]
    private void DispatchUseGarage()
    {
        var garageVehicleState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.Garage];
        if (garageVehicleState.State != StateId.House.CarReady ||
            runtime.State.RawDataBlock.Control.TransitionTextEntryStates[CarKeyCollectedStateIndex] == 0)
        {
            return;
        }

        runtime.PromptController.RunTextAnimation(StringId.House_GarageUseReadyVehicle);
    }

    /// <summary>
    ///     Uses the bicycle pump on the bicycle and updates the recovered bicycle-condition state.
    /// </summary>
    [FunctionSymbol("sub_12312", 0x12312)]
    private void DispatchUseBicyclePumpOnBicycle()
    {
        var bicycleState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BicycleRecord];
        switch (bicycleState.State)
        {
            case StateId.House.Bicycle.Flat:
                bicycleState.State = StateId.House.Bicycle.Ready;
                break;

            case StateId.House.Bicycle.Flat | StateId.Open:
                bicycleState.State = StateId.House.Bicycle.Ready | StateId.Open;
                bicycleState.StateId = StateId.House.Bicycle.Ready;
                break;
        }

        runtime.PromptController.RunTextAnimation(StringId.House_BicyclePumpUseOnBicycleSuccess);
    }

    /// <summary>
    ///     Runs the bicycle inspection text animation chosen from the recovered bicycle-condition state.
    /// </summary>
    [FunctionSymbol("sub_11C61", 0x11C61)]
    private void DispatchInspectBicycle()
    {
        var bicycleState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BicycleRecord];
        switch (bicycleState.State)
        {
            case StateId.House.Bicycle.Flat:
                runtime.PromptController.RunTextAnimation(StringId.House_BicycleLockedAndFlat);
                return;
            case StateId.House.Bicycle.Ready:
                runtime.PromptController.RunTextAnimation(StringId.House_BicycleLocked);
                return;
            case StateId.House.Bicycle.Flat | StateId.Open:
                runtime.PromptController.RunTextAnimation(StringId.House_BicycleFlat);
                return;
            case StateId.House.Bicycle.Ready | StateId.Open:
                runtime.PromptController.RunTextAnimation(StringId.House_BicycleReady);
                return;
            default:
                return;
        }
    }

    [FunctionSymbol("sub_11CB4", 0x11CB4)]
    private void DispatchUseBicycle()
    {
        // IDA 0x11CBA..0x11CC2 and 0x11E2D..0x11E33: only the reviewed ready-bike state 0x0082 enters the animation
        // branch. Every other state falls back to the shared bicycle inspection prompt seam at sub_11C61.
        var bicycleState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BicycleRecord];
        if (bicycleState.State != (StateId.House.Bicycle.Ready | StateId.Open))
        {
            DispatchInspectBicycle();
            return;
        }

        _bicycleHandler.RunRide();
    }
}
