using Game.Catalogs;

namespace Game.Runtime.Execution.Interaction;

internal sealed class InteractionSelectionResolver(Erbe runtime, PromptController promptController)
{
    private const ushort GlobalControlFirstRegionIndex = 0x001E;
    private const ushort GlobalControlRegionCount = 0x0017;
    private const ushort FallbackTextSelectionThreshold = 0x0011;

    internal int FindGlobalControlSelection()
    {
        return runtime.InteractiveRegions.FindFirstSelectablePanelRegionAtPointer(GlobalControlFirstRegionIndex,
            GlobalControlRegionCount);
    }

    internal ushort ResolveConfirmationSelectionLocalIndex(InteractionSession session)
    {
        var currentStateLocalSelection =
            FindCurrentStateLocalSelection(runtime.State.RawDataBlock.Control.ProgramStateId);
        if (session.ConfirmationPassCount == 0 &&
            currentStateLocalSelection == session.PrimarySelectedInteractionLocalIndex)
        {
            return 0x000E;
        }

        if (session.ConfirmationPassCount != 0 &&
            currentStateLocalSelection == session.SecondarySelectedInteractionLocalIndex)
        {
            return 0x000E;
        }

        var selectedGlobalControlIndex = FindGlobalControlSelection();
        return selectedGlobalControlIndex >= 0 ? (ushort)selectedGlobalControlIndex : ushort.MaxValue;
    }

    internal bool TryResolveCurrentStateSelection(ushort stateId, out InteractionSelectionResolution resolution)
    {
        var stateDescriptor = runtime.Scenes.ReadStateDescriptor(stateId);
        var localSelectionIndex = runtime.InteractiveRegions.FindFirstSelectablePanelRegionAtPointer(
            stateDescriptor.SceneEntryStartIndex,
            stateDescriptor.SceneEntryCount);
        if (localSelectionIndex < 0)
        {
            resolution = default;
            return false;
        }

        var selectionLocalIndex = (ushort)localSelectionIndex;
        var selectedInteractionIndex = (ushort)(stateDescriptor.SceneEntryStartIndex + selectionLocalIndex);
        var descriptorRef = InteractionDescriptorRef.SceneEntry(selectedInteractionIndex);
        resolution = new InteractionSelectionResolution(selectionLocalIndex,
            selectedInteractionIndex,
            descriptorRef,
            runtime.Interactions.ReadInteractiveSelectionDescriptor(descriptorRef));
        return true;
    }

    internal bool TryResolveCurrentStateOrTransitionSelection(ushort stateId,
        out InteractionSelectionResolution resolution,
        out bool usedFallback)
    {
        if (TryResolveCurrentStateSelection(stateId, out resolution))
        {
            usedFallback = false;
            return true;
        }

        var selectedGlobalControlIndex = FindGlobalControlSelection();
        usedFallback = TryResolveTransitionTextFallbackSelection(selectedGlobalControlIndex, out resolution);
        return usedFallback;
    }

    private bool TryResolveTransitionTextFallbackSelection(int selectedGlobalControlIndex,
        out InteractionSelectionResolution resolution)
    {
        if (selectedGlobalControlIndex < FallbackTextSelectionThreshold)
        {
            resolution = default;
            return false;
        }

        short currentTextEntryIndex = runtime.State.RawDataBlock.Control.TransitionTextStartIndex;
        for (var remainingSelectionSteps = selectedGlobalControlIndex - FallbackTextSelectionThreshold;
             remainingSelectionSteps > 0;
             remainingSelectionSteps--)
        {
            currentTextEntryIndex = promptController.FindNextEnabledTextAnimationIndex(currentTextEntryIndex);
        }

        if (currentTextEntryIndex < 0)
        {
            resolution = default;
            return false;
        }

        var selectionLocalIndex = PromptController.ResolveFallbackSelectionLocalIndex(currentTextEntryIndex);
        var selectedInteractionIndex = PromptController.ResolveFallbackSelectedInteractionIndex(currentTextEntryIndex);
        var descriptorRef = InteractionDescriptorRef.TransitionSelection(checked((ushort)currentTextEntryIndex));
        resolution = new InteractionSelectionResolution(selectionLocalIndex,
            selectedInteractionIndex,
            descriptorRef,
            runtime.Interactions.ReadInteractiveSelectionDescriptor(descriptorRef));
        return true;
    }

    internal static InteractionButton? TryResolveButton(int selectedGlobalControlIndex)
    {
        return selectedGlobalControlIndex is >= (int)InteractionButton.Inspect and <= (int)InteractionButton.NextText
            ? (InteractionButton)selectedGlobalControlIndex
            : null;
    }

    internal static ushort ToMask(InteractionButton button)
    {
        return button <= InteractionButton.Empty ? (ushort)(1 << (int)button) : (ushort)0;
    }

    private int FindCurrentStateLocalSelection(ushort stateId)
    {
        var descriptor = runtime.Scenes.ReadStateDescriptor(stateId);
        return runtime.InteractiveRegions.FindFirstSelectablePanelRegionAtPointer(descriptor.SceneEntryStartIndex,
            descriptor.SceneEntryCount);
    }
}
