using Game.Catalogs;
using Game.DataBlock.Scene;
using Game.Display;
using Game.Runtime.Rooms.House;
using Game.Shared.Diagnostics;
using Game.Shared.RE;
using Game.State;
using Game.Text;

namespace Game.Runtime.Execution;

internal sealed class EntrySequence(
    Erbe runtime,
    PromptController promptController,
    EntrySelectionAnimator entrySelectionAnimator)
{
    /// <summary>
    ///     Prepares the current scene transition and its optional timed prompt effect.
    /// </summary>
    /// <param name="currentState">State id whose descriptor and transition assets should be prepared.</param>
    [FunctionSymbol("sub_1455D", 0x1455D)]
    internal void PrepareSceneTransition(ushort currentState)
    {
        var descriptor = runtime.Scenes.ReadStateDescriptor(currentState);
        var currentStateLabel = runtime.Scenes.FormatStateIdForDiagnostics(currentState, descriptor);
        if (currentState == SceneCatalog.House)
        {
            HouseFriendlyManVisibilityState.Synchronize(runtime);
        }

        // IDA 0x14563..0x14622: publish the current state and scene hint, then reset and advance the transition band.
        runtime.State.RawDataBlock.Control.ProgramStateId = currentState;
        GameLog.Write(LoggingChannel.Runtime, $"sub_1455D prepare currentState={currentStateLabel}");

        GameLog.Debug(LoggingChannel.Runtime,
            $"sub_1455D descriptor state={currentStateLabel} sceneStart=0x{descriptor.SceneEntryStartIndex:X4} sceneCount=0x{descriptor.SceneEntryCount:X4} backdropEnabled=0x{descriptor.BackdropEnabledFlag:X2} visibleRowsBase=0x{descriptor.VisibleRowsBase:X2}");
        if (descriptor.RleWordStreamOffset != 0)
        {
            runtime.Scenes.DecodeWordRunsIntoTable(descriptor.RleWordStreamOffset,
                runtime.State.Program.BackdropMinimumThresholdRowTable);
        }

        runtime.State.RawDataBlock.Control.VisibleRowsBase = descriptor.VisibleRowsBase;
        runtime.TransitionEffect.ResetTransitionEffect();
        runtime.TransitionEffect.AdvanceTransitionEffect();

        runtime.State.Presentation.Display.CurrentDrawColorIndex = 0x00;
        runtime.DisplayPrimitives.FillRectangleWithCurrentColor(0x0000, 0x0000, RuntimeState.FrameWidth, 0x007F);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        runtime.DisplayPrimitives.FillRectangleWithCurrentColor(0x0000, 0x0000, RuntimeState.FrameWidth, 0x007F);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        // IDA 0x14625..0x146BA: refresh the retained per-state LBM pair into the scene-source surface and the shared
        // full-screen source surface.
        if (descriptor.FirstAssetId != AssetId.None)
        {
            var firstSceneBytes = runtime.ContentFileLoader.LoadOrThrow(descriptor.FirstAssetId);
            runtime.LbmDecoder.DecodeIntoBuffer(firstSceneBytes,
                runtime.State.Program.ScratchBuffers.Scene,
                DisplayCompatibilityState.StrideBytes);
        }

        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        if (descriptor.SecondAssetId != AssetId.None)
        {
            runtime.FullScreenSourceSurface.Reload(descriptor.SecondAssetId);
        }

        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        // IDA 0x146BD..0x1472E: copy the prepared frame into the work buffer, snapshot it, render the current scene and
        // prompt panel, upload the palette, then redraw the scene and panel once more.
        var region = new DisplayCopyRegion(RuntimeState.FrameWidth, RuntimeState.StageHeight, 0, 0, 0, 0);

        runtime.State.RawDataBlock.Control.BackdropEnabledFlag = descriptor.BackdropEnabledFlag;

        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0, runtime.FullScreenSourceSurface.Buffer, region);
        runtime.DisplayCopy.CopyWorkBufferToSnapshotBuffer();

        runtime.ProgramScene.RenderCurrentScene();
        promptController.RenderTextPanelAnimation();

        runtime.Palette.UploadFullPalette(runtime.State.Presentation.Display.PaletteDacTable);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0, runtime.FullScreenSourceSurface.Buffer, region);
        runtime.ProgramScene.RenderCurrentScene();
        promptController.RenderTextPanelAnimation();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();

        // IDA 0x14731..0x1475E: route the optional descriptor text to either the immediate transition band queue or
        // the timed-text helper.
        if (descriptor.OptionalTransitionText == StringId.None)
        {
            return;
        }

        if (currentState == 1)
        {
            runtime.TransitionEffect.QueueStateTransitionEffectText(descriptor.OptionalTransitionText);
            return;
        }

        promptController.RunTextAnimation(descriptor.OptionalTransitionText);
    }

    /// <summary>
    ///     Runs the first fixed selection animation branch and returns its next state.
    /// </summary>
    /// <returns>The next program-state id published by the original branch.</returns>
    [FunctionSymbol("sub_14827", 0x14827)]
    internal ushort RunInitialSelectionState()
    {
        entrySelectionAnimator.RunSelectionAnimation(StringId.Shared_InitialSelectionAnimation);
        return 0x0003;
    }

    /// <summary>
    ///     Runs the follow-up selection branch and its conditional timed text.
    /// </summary>
    /// <returns>The next program-state id published by the original branch.</returns>
    [FunctionSymbol("sub_14839", 0x14839)]
    internal ushort RunFollowupSelectionState()
    {
        entrySelectionAnimator.RunSelectionAnimation(StringId.Shared_FollowupSelectionAnimation);
        if (runtime.State.RawDataBlock.Control.LolitaProgress.HasCompletedLolitaHeartPrerequisites())
        {
            return 0x0006;
        }

        promptController.RunTextAnimation(StringId.Shared_FollowupTimedTransition);
        return 0x0011;
    }
}
