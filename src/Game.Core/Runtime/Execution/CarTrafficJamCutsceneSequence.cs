using Game.Display;
using Game.Shared.RE;
using Game.Text;

namespace Game.Runtime.Execution;

internal sealed class CarTrafficJamCutsceneSequence(Erbe runtime)
{
    private const ushort InitialDelayRetraceCount = 0x0046;
    private const ushort PerIterationDelayRetraceCount = 0x0004;
    private const ushort IterationCount = 0x005A;
    private const ushort DestinationColumnModulo = 0x0160;
    private const ushort DestinationRowModulo = 0x0060;
    private const short DestinationColumnBias = 0x0020;
    private const ushort DestinationSeedMultiplier = 0x0005;
    private const ushort InitialDestinationSeed = 0x00AD;
    private readonly ushort[] _frameHeightTableCopy = new ushort[4];
    private readonly ushort[] _frameSourceColumnTableCopy = new ushort[4];
    private readonly ushort[] _frameSourceRowTableCopy = new ushort[4];
    private readonly ushort[] _frameWidthTableCopy = new ushort[4];

    /// <summary>
    ///     Runs the car traffic-jam cutscene to completion.
    /// </summary>
    [FunctionSymbol("sub_1490C", 0x1490C)]
    internal void RunCarTrafficJamCutscene()
    {
        runtime.BootData.CarTrafficJamFrameTables.CopyTo(_frameSourceColumnTableCopy,
            _frameSourceRowTableCopy,
            _frameWidthTableCopy,
            _frameHeightTableCopy);

        ushort frameCycleIndex = 0;
        var destinationSeed = InitialDestinationSeed;

        runtime.HostPacing.WaitFrames(InitialDelayRetraceCount - 1);
        for (ushort iterationIndex = 0; iterationIndex < IterationCount; iterationIndex++)
        {
            runtime.HostPacing.WaitFrames(PerIterationDelayRetraceCount - 1);

            frameCycleIndex = (ushort)((frameCycleIndex + 1) % 4);
            var currentRegion = new DisplayCopyRegion(unchecked((short)_frameWidthTableCopy[frameCycleIndex]),
                unchecked((short)_frameHeightTableCopy[frameCycleIndex]),
                unchecked((short)_frameSourceRowTableCopy[frameCycleIndex]),
                unchecked((short)_frameSourceColumnTableCopy[frameCycleIndex]),
                unchecked((short)(destinationSeed % DestinationRowModulo)),
                unchecked((short)(destinationSeed % DestinationColumnModulo - DestinationColumnBias)));
            destinationSeed = unchecked((ushort)(destinationSeed * DestinationSeedMultiplier + 1));

            var sceneSourceSurface = runtime.State.Program.ScratchBuffers.Scene;
            ReplayCurrentFrame(sceneSourceSurface, currentRegion);
            ReplayCurrentFrame(sceneSourceSurface, currentRegion);
        }

        runtime.PromptController.QueueOzoneAlternateSceneTransition(StringId.Shared_CarTrafficJamWarning, 5);
        runtime.PromptController.ShowOzoneAlternateScene();
    }

    private void ReplayCurrentFrame(byte[] sceneSourceSurface, DisplayCopyRegion currentRegion)
    {
        runtime.DisplayCopy.CopyClippedTransparentIndexedRegionToWorkBuffer(0, sceneSourceSurface, currentRegion);
        runtime.PointerOverlay.AdvancePointerOverlayFrame();
    }
}
