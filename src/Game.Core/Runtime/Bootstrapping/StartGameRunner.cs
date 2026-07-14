using Game.Catalogs;
using Game.DataBlock.Selection;
using Game.Display;
using Game.Runtime.Rooms.Bedroom;
using Game.Runtime.Rooms.House;
using Game.Shared.Diagnostics;
using Game.Shared.RE;
using Game.State;

namespace Game.Runtime.Bootstrapping;

internal sealed class StartGameRunner(Erbe runtime)
{
    private const ushort QuickLaundromatOutsideProgramState = 0x000C;
    private const ushort QuickLaundromatProgramState = 0x000F;
    private const ushort QuickLaundromatOutsideBrochureSceneEntry = 0x0084;
    private const ushort QuickLaundromatInsideBrochureSceneEntry = 0x00A2;

    /// <summary>
    ///     Gets the currently selected persistent-data region index, or <c>-1</c> while awaiting a choice.
    /// </summary>
    private int _selectedRegionIndex = -1;

    /// <summary>
    ///     Runs the persistent-data selection screen and preloads the selected session data.
    /// </summary>
    [FunctionSymbol("sub_12383", 0x12383)]
    internal void Run()
    {
        var persistentDataLoadedForCurrentSelection = false;
        _selectedRegionIndex = -1;

        runtime.FullScreenSourceSurface.Reload(AssetId.DisplayBackdrop);
        var selectionPanelRegion = new DisplayCopyRegion(88, 54, 76, 126, 143, 217);

        runtime.State.Presentation.Display.CurrentDrawColorIndex = 33;
        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            selectionPanelRegion);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            selectionPanelRegion);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        while (_selectedRegionIndex == -1)
        {
            var inputEvent = runtime.InputAdapter.PollInputEvent();
            if (inputEvent.IsPrimaryConfirmAction)
            {
                _selectedRegionIndex = runtime.InteractiveRegions.FindFirstSelectablePanelRegionAtPointer(0, 2);
                if (_selectedRegionIndex != -1)
                {
                    var persistentDataAsset = _selectedRegionIndex == 1
                        ? AssetId.SaveGame
                        : AssetId.DefaultDataImage;
                    var persistentDataPath = runtime.Assets.ResolveOrThrow(persistentDataAsset);
                    if (runtime.Resources.TryResolve(persistentDataPath, out var persistentDataEntry))
                    {
                        // IDA 0x123B4..0x123D5 adapted: preserve the original fixed-buffer persistent-data overlay
                        // behavior by copying the loaded bytes into a full snapshot-sized block and leaving the unread
                        // tail unchanged.
                        var persistentDataBytes = persistentDataEntry.ReadAll().ToArray();
                        var persistentDataSnapshot = runtime.State.GetSnapshot();
                        persistentDataBytes.AsSpan(
                                0,
                                Math.Min(persistentDataBytes.Length, persistentDataSnapshot.Length))
                            .CopyTo(persistentDataSnapshot);
                        runtime.State.Initialize(persistentDataSnapshot);
                        persistentDataLoadedForCurrentSelection = true;
                        PublishLoadedBedroomDoorExitTransientHandler();
                        ReconcileLoadedQuickLaundromatBrochureSceneEntries();
                        HouseFriendlyManVisibilityState.Synchronize(runtime);
                    }
                }
            }

            runtime.PointerOverlay.AdvancePointerOverlayFrame();
        }

        FinalizeSelection(persistentDataLoadedForCurrentSelection);
    }

    private void FinalizeSelection(bool persistentDataLoadedForCurrentSelection)
    {
        var heroPortraitAsset = runtime.State.RawDataBlock.Control.UseAlternateHeroPortrait
            ? AssetId.HeroPortraitAlternate
            : AssetId.HeroPortrait;
        var heroPortraitBytes = runtime.ContentFileLoader.LoadOrThrow(heroPortraitAsset);
        runtime.LbmDecoder.DecodeIntoBuffer(heroPortraitBytes,
            runtime.State.Program.ScratchBuffers.Backdrop,
            DisplayCompatibilityState.StrideBytes);

        if (persistentDataLoadedForCurrentSelection)
        {
            RestoreLoadedQuickLaundromatInheritedSceneSurfaces();
        }
    }

    private void PublishLoadedBedroomDoorExitTransientHandler()
    {
        var heaterState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomHeaterRecord].State;
        var birdState = runtime.State.RawDataBlock.SelectionTable[SelectionEntryCatalog.BedroomBirdRecord].State;
        var expectedHandlerId = heaterState == StateId.Bedroom.HeaterActive && birdState == StateId.Bedroom.BirdAlive
            ? InteractionHandlerId.Bedroom_ExitDoor
            : InteractionHandlerId.None;
        var currentHandlerId = runtime.Interactions.ReadHandlerId(BedroomDoorExitInteractionSlot.SelectionGlobalIndex,
            BedroomDoorExitInteractionSlot.CurrentSelectionHandlerByteOffset);

        if (currentHandlerId != expectedHandlerId)
        {
            GameLog.Warning(LoggingChannel.Runtime,
                $"Correcting bedroom door-exit transient handler misbehavior after persistent data load: selectionGlobal=0x{BedroomDoorExitInteractionSlot.SelectionGlobalIndex:X4} " +
                $"handlerByteOffset=+0x{BedroomDoorExitInteractionSlot.CurrentSelectionHandlerByteOffset:X2} old=0x{(ushort)currentHandlerId:X4} new=0x{(ushort)expectedHandlerId:X4}.");
        }

        runtime.Interactions.SetHandlerId(BedroomDoorExitInteractionSlot.SelectionGlobalIndex,
            BedroomDoorExitInteractionSlot.CurrentSelectionHandlerByteOffset,
            expectedHandlerId);
    }

    private void ReconcileLoadedQuickLaundromatBrochureSceneEntries()
    {
        var insideState = runtime.Interactions.ReadInteractiveSelectionDescriptor(
            InteractionDescriptorRef.SceneEntry(QuickLaundromatInsideBrochureSceneEntry)).SelectionStateId;
        var outsideState = runtime.Interactions.ReadInteractiveSelectionDescriptor(
            InteractionDescriptorRef.SceneEntry(QuickLaundromatOutsideBrochureSceneEntry)).SelectionStateId;
        if (insideState != StateId.Disabled && outsideState != StateId.Disabled)
        {
            return;
        }

        if (insideState == StateId.Disabled && outsideState == StateId.Disabled)
        {
            return;
        }

        GameLog.Warning(LoggingChannel.Runtime,
            "Correcting quick-laundromat brochure scene-entry misbehavior after persistent data load: " +
            $"insideEntry=0x{QuickLaundromatInsideBrochureSceneEntry:X4} insideState=0x{insideState:X4} " +
            $"outsideEntry=0x{QuickLaundromatOutsideBrochureSceneEntry:X4} outsideState=0x{outsideState:X4} " +
            $"newState=0x{StateId.Disabled:X4}.");
        runtime.Interactions.SetSelectionState(
            InteractionDescriptorRef.SceneEntry(QuickLaundromatInsideBrochureSceneEntry),
            StateId.Disabled);
        runtime.Interactions.SetSelectionState(
            InteractionDescriptorRef.SceneEntry(QuickLaundromatOutsideBrochureSceneEntry),
            StateId.Disabled);
    }

    private void RestoreLoadedQuickLaundromatInheritedSceneSurfaces()
    {
        if (runtime.State.RawDataBlock.Control.ProgramStateId != QuickLaundromatProgramState)
        {
            return;
        }

        GameLog.Warning(LoggingChannel.Runtime,
            "Correcting quick-laundromat retained-scene misbehavior after persistent data load: " +
            $"state=0x{QuickLaundromatProgramState:X4} " +
            $"inheritedState=0x{QuickLaundromatOutsideProgramState:X4} " +
            $"inheritedAssets=(0x{(ushort)AssetId.ShopScene2Overlay:X4},0x{(ushort)AssetId.ShopScene2:X4}).");

        var sceneBytes = runtime.ContentFileLoader.LoadOrThrow(AssetId.ShopScene2Overlay);
        runtime.LbmDecoder.DecodeIntoBuffer(sceneBytes,
            runtime.State.Program.ScratchBuffers.Scene,
            DisplayCompatibilityState.StrideBytes);

        runtime.FullScreenSourceSurface.Reload(AssetId.ShopScene2);
        ComposeLoadedQuickLaundromatOutsideSceneIntoInheritedSurface();
    }

    private void ComposeLoadedQuickLaundromatOutsideSceneIntoInheritedSurface()
    {
        var control = runtime.State.RawDataBlock.Control;
        var originalProgramState = control.ProgramStateId;
        var outsideSceneDescriptor = runtime.Scenes.ReadStateDescriptor(QuickLaundromatOutsideProgramState);

        try
        {
            control.ProgramStateId = QuickLaundromatOutsideProgramState;
            control.VisibleRowsBase = outsideSceneDescriptor.VisibleRowsBase;
            control.BackdropEnabledFlag = outsideSceneDescriptor.BackdropEnabledFlag;
            runtime.ProgramScene.RenderCurrentScene();
            runtime.DisplayCopy.CopyWorkBufferToSnapshotBuffer();
        }
        finally
        {
            control.ProgramStateId = originalProgramState;
            control.VisibleRowsBase = runtime.Scenes.ReadStateDescriptor(originalProgramState).VisibleRowsBase;
        }
    }
}
