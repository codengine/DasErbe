using Game.Catalogs;
using Game.Display;
using Game.Runtime.ProgramScene;
using Game.Shared.RE;
using Game.State;

namespace Game.Runtime;

/// <summary>
///     Session-owned program-scene helpers for carried-over current-state scene composition symbols.
/// </summary>
/// <param name="runtime">Owning runtime state.</param>
internal sealed class ProgramSceneController(Erbe runtime)
{
    private const short BackdropThresholdUpperBoundExclusive = 0x0080;
    private const short BackdropBaseHeightRows = 0x0042;
    private const short BackdropScreenBottomRow = 0x0080;
    private const int BackdropFootprintLeftColumnSpan = 0x14;
    private const int BackdropFootprintRightColumnSpanExclusive = 0x13;
    private const int BackdropSelectionFrameCount = 0x18;
    private readonly ProgramSceneBackdropAnimationRunner _backdropAnimationRunner = new(runtime);

    /// <summary>
    ///     Rebuilds the current scene composition into the work buffer.
    /// </summary>
    [FunctionSymbol("sub_1445B", 0x1445B)]
    internal void RenderCurrentScene()
    {
        var backdrop = runtime.State.RawDataBlock.Control;
        var sceneDescriptor = runtime.Scenes.ReadStateDescriptor(runtime.State.RawDataBlock.Control.ProgramStateId);
        var backdropEmitted = false;

        // IDA 0x14461..0x14485: resolve the current state descriptor and restore the retained 320x128 snapshot region
        // before rebuilding the current state's foreground scene composition.
        runtime.DisplayCopy.CopySnapshotRegionToWorkBuffer(0, 0, RuntimeState.FrameWidth, RuntimeState.StageHeight);

        // IDA 0x14488..0x1454D: iterate the current state's scene-entry range, emit the shared backdrop seam once before
        // the first region whose bottom row crosses the current backdrop threshold, gate each region against the
        // persistent-data state mask, then forward the copied six-word blit descriptor into the shared clipped
        // transparent blit seam via sub_10637.
        for (ushort sceneIndex = 0; sceneIndex < sceneDescriptor.SceneEntryCount; sceneIndex++)
        {
            var sceneEntry = runtime.Scenes.ReadProgramSceneEntry(
                checked((ushort)(sceneDescriptor.SceneEntryStartIndex + sceneIndex)));

            if (!backdropEmitted && sceneEntry.DestinationRow + sceneEntry.HeightRows > backdrop.BackdropThresholdRow)
            {
                // IDA 0x144CD..0x144DC: emit the shared backdrop helper via sub_1438A once before the first scene
                // region that crosses the current backdrop threshold row.
                RenderBackdrop();
                backdropEmitted = true;
            }

            // IDA 0x144DF..0x144ED: skip scene entries whose carried-over selection state marks them empty or taken
            // before consulting the persistent-data state gate. sub_14C83 mutates that +0x02 descriptor word through
            // the managed selection-state override map, so scene rendering must observe the same runtime-owned state as
            // hit-testing.
            if (sceneEntry.StateMask is StateId.Default or StateId.Disabled)
            {
                continue;
            }

            var statemask = runtime.State.RawDataBlock.SelectionTable[sceneEntry.EntryIndex].State;
            if (!ShouldBlitSceneEntry(statemask, sceneEntry.RequiredMask))
            {
                continue;
            }

            var blitRegion = new DisplayCopyRegion(sceneEntry.WidthPixels,
                sceneEntry.HeightRows,
                sceneEntry.SourceRow,
                sceneEntry.SourceColumn,
                sceneEntry.DestinationRow,
                sceneEntry.DestinationColumn);

            // IDA 0x144FE..0x14538: forward the six-word scene blit descriptor into the shared clipped transparent
            // copy seam via sub_10637.
            runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0,
                runtime.State.Program.ScratchBuffers.Scene,
                blitRegion);
        }

        // IDA 0x14550..0x14556: if no scene entry crossed the backdrop threshold earlier, emit the shared backdrop seam
        // once after the loop.
        if (!backdropEmitted)
        {
            RenderBackdrop();
        }
    }

    private static bool ShouldBlitSceneEntry(ushort currentMask, ushort requiredMask)
    {
        return currentMask != StateId.Disabled && requiredMask != StateId.Disabled && requiredMask != 0 &&
               (currentMask & requiredMask) != 0;
    }

    /// <summary>
    ///     Runs the backdrop-selection animation to completion.
    /// </summary>
    /// <param name="descriptorRef">Semantic reference for the animated interaction descriptor.</param>
    [FunctionSymbol("sub_14B73", 0x14B73)]
    internal void AnimateBackdropSelection(InteractionDescriptorRef descriptorRef)
    {
        _backdropAnimationRunner.RunBackdropSelection(descriptorRef,
            CanPlaceBackdropAt,
            RenderCurrentScene,
            AdvanceProgramSceneBackdropSelectionIndex);
    }

    /// <summary>
    ///     Checks whether the backdrop can occupy one candidate column and threshold row.
    /// </summary>
    /// <param name="candidateColumn">Candidate backdrop destination column.</param>
    /// <param name="candidateThresholdRow">Candidate backdrop threshold row.</param>
    [FunctionSymbol("sub_14B2E", 0x14B2E)]
    private bool CanPlaceBackdropAt(short candidateColumn, short candidateThresholdRow)
    {
        var program = runtime.State.Program;

        // IDA 0x14B34..0x14B6F: reject any candidate threshold row at or below the screen bottom, then ensure the
        // full 39-column backdrop footprint clears the current state's decoded minimum-threshold table.
        if (candidateThresholdRow >= BackdropThresholdUpperBoundExclusive)
        {
            return false;
        }

        for (var footprintColumn = candidateColumn - BackdropFootprintLeftColumnSpan;
             footprintColumn < candidateColumn + BackdropFootprintRightColumnSpanExclusive;
             footprintColumn++)
        {
            if (candidateThresholdRow < program.BackdropMinimumThresholdRowTable[footprintColumn])
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Advances the backdrop selection-frame index modulo the 24-frame cycle.
    /// </summary>
    [FunctionSymbol("sub_14374", 0x14374)]
    private void AdvanceProgramSceneBackdropSelectionIndex()
    {
        var backdrop = runtime.State.RawDataBlock.Control;

        // IDA 0x14377..0x14388: increment the backdrop selection index and wrap it modulo the 24-frame backdrop
        // animation cycle.
        var nextSelectionIndex = backdrop.BackdropSelectionIndex + 1;
        backdrop.BackdropSelectionIndex = (short)(nextSelectionIndex % BackdropSelectionFrameCount);
    }

    /// <summary>
    ///     Draws the current program-scene backdrop when the state requests it.
    /// </summary>
    [FunctionSymbol("sub_1438A", 0x1438A)]
    internal void RenderBackdrop()
    {
        var backdrop = runtime.State.RawDataBlock.Control;

        // IDA 0x14390..0x143A9: derive the visible backdrop span from VisibleRowsBase and the current
        // backdrop threshold, then return immediately unless the current state requests backdrop rendering via
        // byte_18808 == 1.
        var visibleRowsBelowThreshold =
            backdrop.VisibleRowsBase - (BackdropScreenBottomRow - backdrop.BackdropThresholdRow);
        if (backdrop.BackdropEnabledFlag != 1)
        {
            return;
        }

        // IDA 0x143AC..0x14403: resolve the current backdrop descriptor entry id from the selection table and extract
        // the fixed source column, source row, base width, and optional horizontal mirror flag from the executable
        // lookup tables.
        var backdropSelectionTableIndex = (backdrop.BackdropSelectionIndex >> 1) + backdrop.BackdropSelectionRow * 0x0C;
        var backdropDescriptorEntryWithFlags =
            runtime.Scenes.ReadBackdropDescriptorSelection(checked((ushort)backdropSelectionTableIndex));
        var horizontalStep = (short)((backdropDescriptorEntryWithFlags & 0x0080) != 0 ? -1 : 1);
        var backdropDescriptorEntryIndex = backdropDescriptorEntryWithFlags & 0x007F;
        var sourceColumn = runtime.Scenes.ReadBackdropSourceColumn(checked((ushort)backdropDescriptorEntryIndex));
        var sourceRow = runtime.Scenes.ReadBackdropSourceRow(checked((ushort)backdropDescriptorEntryIndex));
        var baseWidthPixels = runtime.Scenes.ReadBackdropBaseWidth(checked((ushort)backdropDescriptorEntryIndex));

        // IDA 0x14406..0x1444C: scale the source backdrop rectangle against the current visible span and build the
        // resolved backdrop draw descriptor for sub_10776.
        var destinationWidthPixels = (short)((baseWidthPixels * visibleRowsBelowThreshold) >> 7);
        var destinationHeightRows = (short)((BackdropBaseHeightRows * visibleRowsBelowThreshold) >> 7);
        var destinationColumn = (short)(backdrop.BackdropColumn - ((baseWidthPixels * visibleRowsBelowThreshold) >> 8));
        var destinationRow = (short)(backdrop.BackdropThresholdRow - destinationHeightRows);
        var backdropRegion = new ProgramSceneBackdropCopyRegion(unchecked((short)sourceColumn),
            unchecked((short)sourceRow),
            unchecked((short)baseWidthPixels),
            BackdropBaseHeightRows,
            destinationColumn,
            destinationRow,
            destinationWidthPixels,
            destinationHeightRows,
            horizontalStep);

        // IDA 0x1444D..0x14452: forward the resolved backdrop descriptor into the shared scaled backdrop draw seam via
        // sub_10776.
        runtime.DisplayCopy.DrawScaledBackdrop(backdropRegion);
    }
}
