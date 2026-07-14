using Game.Display;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Execution;

internal sealed class LolitaHeartOutroSequence(Erbe runtime)
{
    private const ushort EndingSceneSourceColumn = 0x00C2;
    private const ushort EndingSceneSourceRow = 0x0000;
    private const ushort EndingSceneWidthPixels = 0x004D;
    private const ushort EndingSceneHeightRows = 0x0043;
    private const short EndingSceneDestinationColumn = 0x006F;
    private const short EndingSceneDestinationRow = 0x0032;

    /// <summary>
    ///     Runs the fixed Lolita heart outro branch.
    /// </summary>
    [FunctionSymbol("sub_14861", 0x14861)]
    internal void RunLolitaHeartOutroState()
    {
        var sceneSourceSurface = runtime.State.Program.ScratchBuffers.Scene;

        ReplayOutroFrame(sceneSourceSurface);
        ReplayOutroFrame(sceneSourceSurface);

        runtime.PromptController.RunTextAnimation(StringId.Shared_LolitaHeartOutro);
    }

    private void ReplayOutroFrame(byte[] sceneSourceSurface)
    {
        var outroRegion = new DisplayCopyRegion(unchecked((short)EndingSceneWidthPixels),
            unchecked((short)EndingSceneHeightRows),
            unchecked((short)EndingSceneSourceRow),
            unchecked((short)EndingSceneSourceColumn),
            EndingSceneDestinationRow,
            EndingSceneDestinationColumn);

        runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0, sceneSourceSurface, outroRegion);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }
}
