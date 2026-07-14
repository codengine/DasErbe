using Game.Catalogs;
using Game.Input;
using Game.State;

namespace Game.Runtime.ProgramScene;

internal sealed class ProgramSceneBackdropAnimationRunner(Erbe runtime)
{
    private const int BackdropMoveLeftSelectionRow = 0;
    private const int BackdropMoveRightSelectionRow = 1;
    private const int BackdropMoveUpSelectionRow = 2;
    private const int BackdropMoveDownSelectionRow = 3;

    internal void RunBackdropSelection(InteractionDescriptorRef descriptorRef,
        Func<short, short, bool> canPlaceBackdropAt,
        Action renderCurrentScene,
        Action advanceBackdropSelectionIndex)
    {
        var activeDescriptor = runtime.Interactions.ReadInteractiveSelectionDescriptor(descriptorRef);
        var backdrop = runtime.State.RawDataBlock.Control;

        while (true)
        {
            AdvanceOneIteration(activeDescriptor,
                canPlaceBackdropAt,
                renderCurrentScene,
                advanceBackdropSelectionIndex);
            var shouldFinalize =
                (backdrop.BackdropColumn == activeDescriptor.AnimationTargetBackdropColumn &&
                 backdrop.BackdropThresholdRow == activeDescriptor.AnimationTargetBackdropThresholdRow) ||
                runtime.InputAdapter.PollInputEvent() != RuntimeInputEvent.None;

            renderCurrentScene();
            runtime.PointerOverlay.AdvancePointerOverlayFrame();

            if (shouldFinalize)
            {
                break;
            }
        }

        backdrop.BackdropColumn = activeDescriptor.AnimationTargetBackdropColumn;
        backdrop.BackdropThresholdRow = activeDescriptor.AnimationTargetBackdropThresholdRow;
        backdrop.BackdropSelectionRow = activeDescriptor.AnimationTargetBackdropSelectionRow;
        renderCurrentScene();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
        renderCurrentScene();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }

    private void AdvanceOneIteration(InteractiveSelectionDescriptor activeDescriptor,
        Func<short, short, bool> canPlaceBackdropAt,
        Action renderCurrentScene,
        Action advanceBackdropSelectionIndex)
    {
        var program = runtime.State.Program;
        var backdrop = runtime.State.RawDataBlock.Control;
        var targetBackdropColumn = activeDescriptor.AnimationTargetBackdropColumn;
        var targetBackdropThresholdRow = activeDescriptor.AnimationTargetBackdropThresholdRow;

        var horizontalSelectionRow = backdrop.BackdropColumn > targetBackdropColumn
            ? BackdropMoveLeftSelectionRow
            : BackdropMoveRightSelectionRow;
        var verticalSelectionRow = backdrop.BackdropThresholdRow > targetBackdropThresholdRow
            ? BackdropMoveUpSelectionRow
            : BackdropMoveDownSelectionRow;

        if (backdrop.BackdropColumn == targetBackdropColumn)
        {
            if (backdrop.BackdropThresholdRow != targetBackdropThresholdRow && canPlaceBackdropAt(
                    backdrop.BackdropColumn,
                    (short)(backdrop.BackdropThresholdRow + program.BackdropStepTable[verticalSelectionRow])))
            {
                backdrop.BackdropSelectionRow = (short)verticalSelectionRow;
                backdrop.BackdropThresholdRow =
                    (short)(backdrop.BackdropThresholdRow + program.BackdropStepTable[verticalSelectionRow]);
            }
        }
        else
        {
            var candidateBackdropColumn =
                (short)(backdrop.BackdropColumn + program.BackdropStepTable[horizontalSelectionRow]);
            if (canPlaceBackdropAt(candidateBackdropColumn, backdrop.BackdropThresholdRow))
            {
                backdrop.BackdropSelectionRow = (short)horizontalSelectionRow;
                backdrop.BackdropColumn = candidateBackdropColumn;
            }
            else if (canPlaceBackdropAt(backdrop.BackdropColumn,
                         (short)(backdrop.BackdropThresholdRow +
                                 program.BackdropStepTable[BackdropMoveDownSelectionRow])))
            {
                backdrop.BackdropSelectionRow = BackdropMoveDownSelectionRow;
                backdrop.BackdropThresholdRow = (short)(backdrop.BackdropThresholdRow +
                                                        program.BackdropStepTable[BackdropMoveDownSelectionRow]);
            }
        }

        renderCurrentScene();
        advanceBackdropSelectionIndex();
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }
}
