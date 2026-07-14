using Game.Catalogs;
using Game.DataBlock;
using Game.Display;
using Game.Shared.RE;

namespace Game.Runtime.Execution;

internal sealed class SaveScreenController(Erbe runtime)
{
    private const ushort SaveScreenRegionSourceColumn = 0x00D8;
    private const ushort SaveScreenRegionSourceRow = 0x004C;
    private const ushort SaveScreenRegionWidthPixels = 0x0058;
    private const ushort SaveScreenRegionHeightRows = 0x0036;
    private const ushort SaveScreenRegionDestinationColumn = 0x00D9;
    private const ushort SaveScreenRegionDestinationRow = 0x008F;
    private const byte SaveScreenDrawColorIndex = 0x21;
    private const ushort SaveScreenMaskLeftColumn = 0x00E0;
    private const ushort SaveScreenMaskTopRow = 0x00B7;
    private const ushort SaveScreenMaskBottomRow = 0x00C1;
    private const ushort SaveScreenMaskRightColumnBase = 0x00DF;
    private const ushort SaveScreenMaskColumnsPerSelection = 0x000F;
    private const ushort SaveScreenInteractiveRegionCount = 0x0002;

    /// <summary>
    ///     Runs the modal save screen to completion.
    /// </summary>
    [FunctionSymbol("sub_124C9", 0x124C9)]
    internal void StepSaveScreen()
    {
        runtime.FullScreenSourceSurface.Reload(AssetId.DisplayBackdrop);
        runtime.State.Presentation.Display.CurrentDrawColorIndex = SaveScreenDrawColorIndex;
        RenderSaveScreenFrameBody();

        RenderSaveScreenFrameBody();

        while (true)
        {
            var selectedRegionIndex = -1;
            var inputEvent = runtime.InputAdapter.PollInputEvent();
            if (inputEvent.IsPrimaryConfirmAction)
            {
                selectedRegionIndex = runtime.InteractiveRegions.FindFirstSelectablePanelRegionAtPointer(0,
                    SaveScreenInteractiveRegionCount);
                if (selectedRegionIndex == 1)
                {
                    var dataSnapshot = runtime.State.GetSnapshot();
                    _ = runtime.ContentFiles.WriteBufferToContentFile(AssetId.SaveGame,
                        dataSnapshot,
                        DataBlockModel.BlockLength);
                }
            }

            runtime.PointerOverlay.AdvancePointerOverlayFrame();
            if (selectedRegionIndex != -1)
            {
                return;
            }
        }
    }

    /// <summary>
    ///     Toggles the persistent-data state mask for one selected record.
    /// </summary>
    /// <param name="selectedInteractionIndex">Selected interaction index that identifies the persistent-data entry.</param>
    [FunctionSymbol("sub_11B3F", 0x11B3F)]
    internal void DispatchToggleSelectionDataState(ushort selectedInteractionIndex)
    {
        var entryIndex = runtime.Interactions
            .ReadInteractiveSelectionDescriptor(InteractionDescriptorRef.InteractiveRegion(selectedInteractionIndex))
            .SelectionEntryIndex;
        var selectionEntry = runtime.State.RawDataBlock.SelectionTable[entryIndex];
        selectionEntry.State = selectionEntry.State switch
        {
            0x0080 => 0x0000,
            0x0000 => 0x0080,
            _ => selectionEntry.State
        };
    }

    private void RenderSaveScreenFrameBody()
    {
        var selectionPanelRegion = new DisplayCopyRegion(checked((short)SaveScreenRegionWidthPixels),
            checked((short)SaveScreenRegionHeightRows),
            checked((short)SaveScreenRegionSourceRow),
            checked((short)SaveScreenRegionSourceColumn),
            checked((short)SaveScreenRegionDestinationRow),
            checked((short)SaveScreenRegionDestinationColumn));

        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0,
            runtime.FullScreenSourceSurface.Buffer,
            selectionPanelRegion);
        FillSaveSelectionMask();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }

    private void FillSaveSelectionMask()
    {
        var errorCount = runtime.State.RawDataBlock.Control.ErrorCount;
        if (errorCount == 0)
        {
            return;
        }

        runtime.DisplayPrimitives.FillRectangleWithCurrentColor(SaveScreenMaskLeftColumn,
            SaveScreenMaskTopRow,
            checked((ushort)(SaveScreenMaskRightColumnBase + errorCount * SaveScreenMaskColumnsPerSelection)),
            SaveScreenMaskBottomRow);
    }
}
