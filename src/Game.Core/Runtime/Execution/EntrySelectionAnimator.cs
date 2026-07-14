using Game.Display;
using Game.Input;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Execution;

internal sealed class EntrySelectionAnimator(Erbe runtime)
{
    private const ushort SelectionAnimationDestinationColumn = 0x0059;
    private const ushort SelectionAnimationDestinationRow = 0x0007;
    private const ushort SelectionAnimationWidthPixels = 0x002F;
    private const ushort SelectionAnimationHeightRows = 0x0032;
    private const int SelectionAnimationColumnsPerRow = 0x05;

    /// <summary>
    ///     Runs the selection animation to completion for one text id.
    /// </summary>
    /// <param name="stringId">Semantic reference for the text stream.</param>
    [FunctionSymbol("sub_14762", 0x14762)]
    internal void RunSelectionAnimation(StringId stringId)
    {
        var frameScript = runtime.Scenes.ReadSelectionAnimationFrameScript();
        ushort selectionAnimationFrameIndex = 0;

        // IDA 0x14772..0x1477F: queue the caller-supplied transition-text effect once before the animation loop starts.
        runtime.TransitionEffect.QueueStateTransitionEffectText(stringId);

        while (true)
        {
            var frameScriptByte = frameScript[selectionAnimationFrameIndex >> 3];
            if (frameScriptByte == 0xFF)
            {
                selectionAnimationFrameIndex = 0;
                frameScriptByte = frameScript[0];
            }

            // IDA 0x1478F..0x14820: resolve one animation tile, expose it, then either continue or stop on input or
            // once the transition effect disables itself.
            frameScriptByte--;
            var sourceColumn =
                (ushort)(frameScriptByte % SelectionAnimationColumnsPerRow * SelectionAnimationWidthPixels);
            var sourceRow = (ushort)(frameScriptByte / SelectionAnimationColumnsPerRow * SelectionAnimationHeightRows);
            var region = new DisplayCopyRegion(checked((short)SelectionAnimationWidthPixels),
                checked((short)SelectionAnimationHeightRows),
                checked((short)sourceRow),
                checked((short)sourceColumn),
                checked((short)SelectionAnimationDestinationRow),
                checked((short)SelectionAnimationDestinationColumn));
            runtime.DisplayCopy.CopyIndexedRegionToWorkBuffer(0, runtime.State.Program.ScratchBuffers.Scene, region);

            selectionAnimationFrameIndex++;
            runtime.TransitionEffect.AdvanceTransitionEffect();
            runtime.PointerOverlay.AdvancePointerOverlayFrame();

            if (runtime.InputAdapter.PollInputEvent() != RuntimeInputEvent.None ||
                runtime.State.TransitionEffect.EnabledFlag == 0)
            {
                return;
            }
        }
    }
}
