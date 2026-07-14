using System.Collections.Frozen;
using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Rooms.Bedroom;

/// <summary>
///     Owns bedroom interaction handlers and their local blocking helpers.
/// </summary>
/// <param name="runtime">Runtime owner that provides shared runtime seams and session state.</param>
internal sealed class BedroomRoom(Erbe runtime)
{
    private const ushort BoxTakeTextEntryIndex = 0x0009;

    private static readonly FrozenDictionary<InteractionHandlerId, StringId> SimpleTextHandlers =
        new Dictionary<InteractionHandlerId, StringId>
        {
            { InteractionHandlerId.Bedroom_InspectPoster, StringId.Bedroom_PosterInspection },
            { InteractionHandlerId.Bedroom_InspectPinboard, StringId.Bedroom_PinboardInspection },
            { InteractionHandlerId.Bedroom_ReadPinboard, StringId.Bedroom_PinboardRead },
            { InteractionHandlerId.Bedroom_InspectComputer, StringId.Bedroom_ComputerInspection }
        }.ToFrozenDictionary();

    private readonly BedroomBtxTerminalHandler _btxTerminalHandler = new(runtime);
    private readonly BedroomDoorHandler _doorHandler = new(runtime);
    private readonly BedroomHeaterHandler _heaterHandler = new(runtime);
    private readonly BedroomTelephoneHandler _telephoneHandler = new(runtime);
    private readonly BedroomWindowHandler _windowHandler = new(runtime);

    internal bool TryDispatchInteraction(InteractionHandlerId handlerId, ushort selectedInteractionIndex)
    {
        if (SimpleTextHandlers.TryGetValue(handlerId, out var stringId))
        {
            runtime.PromptController.RunTextAnimation(stringId);
            return true;
        }

        switch (handlerId)
        {
            case InteractionHandlerId.Bedroom_UseChair:
                return true;
            case InteractionHandlerId.Bedroom_InspectBed:
                DispatchInspectBed();
                return true;
            case InteractionHandlerId.Bedroom_InspectBird:
                DispatchInspectBird();
                return true;
            case InteractionHandlerId.Bedroom_InspectBox:
                DispatchInspectBox();
                return true;
            case InteractionHandlerId.Bedroom_TakeBoxContents:
                DispatchTakeBoxContents();
                return true;
            case InteractionHandlerId.Bedroom_OpenCloseBox:
                DispatchOpenCloseBox();
                return true;
            case InteractionHandlerId.Bedroom_InspectKeyboard:
                DispatchInspectKeyboard();
                return true;
            case InteractionHandlerId.Bedroom_UseBtxTerminal:
                runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomKeyboardRecord].State =
                    StateId.Bedroom.BtxTerminalReady;
                return true;
            case InteractionHandlerId.Bedroom_InspectHeater:
                _heaterHandler.RunInspectHeater();
                return true;
            case InteractionHandlerId.Bedroom_UseHeater:
                _heaterHandler.RunUseHeater();
                return true;
            case InteractionHandlerId.Bedroom_OpenWindow:
                _windowHandler.RunWindowOpen(selectedInteractionIndex);
                return true;
            case InteractionHandlerId.Bedroom_UseTelephone:
                _telephoneHandler.RunUseTelephone();
                return true;
            case InteractionHandlerId.Bedroom_UseComputer:
                _btxTerminalHandler.RunBtxTerminalMenu();
                return true;
            case InteractionHandlerId.Bedroom_ExitDoor:
                _doorHandler.RunDoorExit();
                return true;
            default:
                return false;
        }
    }

    [FunctionSymbol("sub_12DA7", 0x12DA7)]
    private void DispatchInspectBed()
    {
        var isBedBroken = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomBedRecord].State ==
                          StateId.Broken;
        var stringId = isBedBroken
            ? StringId.Bedroom_BedDestroyedInspection
            : StringId.Bedroom_BedComfortableInspection;
        runtime.PromptController.RunTextAnimation(stringId);
    }

    [FunctionSymbol("sub_12DC3", 0x12DC3)]
    private void DispatchInspectBird()
    {
        var isBirdAlive = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomBirdRecord].State ==
                          StateId.Bedroom.BirdAlive;
        var stringId = isBirdAlive ? StringId.Bedroom_BirdAliveInspection : StringId.Bedroom_BirdDeadInspection;
        runtime.PromptController.RunTextAnimation(stringId);
    }

    [FunctionSymbol("sub_12DDF", 0x12DDF)]
    private void DispatchInspectBox()
    {
        var boxState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomBoxRecord];
        var stringId = boxState.State == (StateId.Container.Filled | StateId.Open)
            ? StringId.Bedroom_BoxOpenFilledInspection
            : StringId.Bedroom_BoxClosedEmptyInspection;
        runtime.PromptController.RunTextAnimation(stringId);
    }

    [FunctionSymbol("sub_12E0B", 0x12E0B)]
    private void DispatchTakeBoxContents()
    {
        var boxState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomBoxRecord];
        if (boxState.State != (StateId.Container.Filled | StateId.Open))
        {
            return;
        }

        boxState.State = StateId.Container.Empty | StateId.Open;
        runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BusStopRecord].StateId =
            StateId.Bedroom.BoxContentsTaken;
        runtime.PromptController.SelectTransitionTextEntry(BoxTakeTextEntryIndex);
    }

    [FunctionSymbol("sub_12E2C", 0x12E2C)]
    private void DispatchOpenCloseBox()
    {
        var boxState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomBoxRecord];
        boxState.State = boxState.State switch
        {
            StateId.Container.Empty => StateId.Container.Empty | StateId.Open,
            StateId.Container.Filled => StateId.Container.Filled | StateId.Open,
            StateId.Container.Empty | StateId.Open => StateId.Container.Empty,
            StateId.Container.Filled | StateId.Open => StateId.Container.Filled,
            _ => boxState.State
        };
    }

    [FunctionSymbol("sub_12E82", 0x12E82)]
    private void DispatchInspectKeyboard()
    {
        var isKeyboardBroken =
            runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomKeyboardRecord].State ==
            StateId.Broken;
        var stringId = isKeyboardBroken
            ? StringId.Bedroom_BrokenKeyboardInspection
            : StringId.Shared_DefaultInteractionText;
        runtime.PromptController.RunTextAnimation(stringId);
    }
}
