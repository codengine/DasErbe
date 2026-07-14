using Game.Catalogs;
using Game.Runtime.Bootstrapping;
using Game.Shared.Diagnostics;
using Game.Shared.RE;
using Game.Shared.Rendering;

namespace Game.Runtime;

/// <summary>
///     Owns the runtime-thread bootstrap loop from startup title through interactive state dispatch.
/// </summary>
/// <param name="runtime">Runtime owner that provides bootstrap subsystems, state, and shared seams.</param>
internal sealed class Bootstrap(Erbe runtime)
{
    private const ushort InitialProgramState = 0x0001;
    private readonly BootstrapEnvironmentInitializer _environmentInitializer = new(runtime);
    private readonly StartGameRunner _startGameRunner = new(runtime);
    private readonly StartupTitleSequenceRunner _startupTitleSequenceRunner = new(runtime);
    private bool _bootInitialized;

    /// <summary>
    ///     Allocates session scratch buffers, installs startup policies, and seeds the initial program state.
    /// </summary>
    [FunctionSymbol("sub_154BF", 0x154BF)]
    private void Boot()
    {
        if (_bootInitialized)
        {
            return;
        }

        _bootInitialized = true;

        // IDA 0x154BF..0x155A8: enter the CRT prefix, allocate the runtime-owned scratch buffers, install startup
        // environment policy, seed the runtime-owned data block from boot data, and publish FONT_VGA before the
        // runtime-thread bootstrap loop starts.
        BootDataBlockSeeder.SeedDataBlock(runtime);
        runtime.State.RawDataBlock.Control.ProgramStateId = InitialProgramState;
        _environmentInitializer.InitializeBootEnvironment();
    }

    /// <summary>
    ///     Runs the blocking runtime-thread bootstrap loop to terminal teardown.
    /// </summary>
    internal void Run()
    {
        Boot();
        _startupTitleSequenceRunner.Run();

        // IDA 0x155AD..0x155CF: return from the startup-title sequence, clear both active display buffers via the
        // now-inlined sub_109CC/sub_1097E clear-buffer seam, enable the pointer-overlay bookkeeping, initialize the
        // main display panel, expose the first prepared panel frame, then enter the saved-data selection screen.
        var display = runtime.State.Presentation.Display;
        Blitter.Clear(display.GetWorkBuffer(), 0);
        runtime.DisplayBufferPublisher.PublishPreparedBuffer();
        Blitter.Clear(display.GetWorkBuffer(), 0);
        runtime.PointerOverlay.EnablePointerOverlay();
        _environmentInitializer.InitializeDisplayPanel();
        // runtime.HostPacing.WaitFrame();

        var persistentDataSnapshot = runtime.State.GetSnapshot();
        runtime.ContentFiles.WriteBufferToContentFile(AssetId.DefaultDataImage,
            persistentDataSnapshot,
            (uint)persistentDataSnapshot.Length);
        _startGameRunner.Run();

        while (true)
        {
            var currentProgramState = runtime.State.RawDataBlock.Control.ProgramStateId;
            var currentStateLabel = runtime.Scenes.FormatStateIdForDiagnostics(currentProgramState);
            GameLog.Debug(LoggingChannel.Runtime, $"sub_154BF prepare state={currentStateLabel}");

            var nextProgramState = PrepareAndRunProgramState(currentProgramState);
            PublishProgramState(nextProgramState);

            if (nextProgramState == 0x0012)
            {
                break;
            }

            if (nextProgramState == 0x0011)
            {
                // IDA 0x155F5..0x155FA: state 0x11 re-enters sub_12383 before the next outer-loop iteration prepares
                // and dispatches the current program state again.
                GameLog.Write(LoggingChannel.Runtime,
                    $"sub_154BF replaying saved-data selection for state {runtime.Scenes.FormatStateIdForDiagnostics(0x0011)}.");
                _startGameRunner.Run();
            }
        }

        // IDA 0x155FC: enter the bootstrap-shutdown pointer-overlay teardown seam via sub_11A50.
        runtime.PointerOverlay.DisablePointerOverlay();

        // IDA 0x155FF..0x15615 remains downstream of sub_11A50 in this turn: restore the saved critical-error
        // policy, restore the captured display mode, then release the original scratch buffers before returning to
        // start.
    }

    private ushort PrepareAndRunProgramState(ushort currentProgramState)
    {
        runtime.EntrySequence.PrepareSceneTransition(currentProgramState);

        return currentProgramState switch
        {
            0x0001 => RunInitialSelectionState(),
            0x0002 => RunFollowupSelectionState(),
            0x0006 => RunLolitaHeartOutroState(),
            0x0010 => RunCarTrafficJamCutsceneState(),
            0x0003 or 0x0004 or 0x0005 or >= 0x0007 and <= 0x000F => RunInteractiveProgramState(),
            _ => DispatchProgramStateFrontier(currentProgramState)
        };
    }

    private ushort RunInitialSelectionState()
    {
        return runtime.EntrySequence.RunInitialSelectionState();
    }

    private ushort RunFollowupSelectionState()
    {
        return runtime.EntrySequence.RunFollowupSelectionState();
    }

    private ushort RunLolitaHeartOutroState()
    {
        runtime.LolitaHeartOutro.RunLolitaHeartOutroState();
        return 0x0011;
    }

    private ushort RunCarTrafficJamCutsceneState()
    {
        runtime.CarTrafficJamCutscene.RunCarTrafficJamCutscene();
        return 0x0011;
    }

    private ushort RunInteractiveProgramState()
    {
        return runtime.InteractionLoop.RunInteractiveLoop();
    }

    private void PublishProgramState(ushort nextProgramState)
    {
        // IDA 0x155E4..0x155F3: publish the returned next-state id before the 0x11 / 0x12 branch logic runs.
        runtime.State.RawDataBlock.Control.ProgramStateId = nextProgramState;
        GameLog.Write(LoggingChannel.Runtime,
            $"sub_154BF nextState={runtime.Scenes.FormatStateIdForDiagnostics(nextProgramState)}");

        if (nextProgramState == 0x0012)
        {
            GameLog.Write(LoggingChannel.Runtime,
                $"sub_154BF terminal branch: state {runtime.Scenes.FormatStateIdForDiagnostics(0x0012)}.");
        }
    }

    private static ushort DispatchProgramStateFrontier(ushort currentProgramState)
    {
        // IDA 0x155DA..0x155E0: dispatch through funcs_155E0[currentProgramState] and preserve the original callee
        // boundary for every evidence-backed table slot.
        return currentProgramState switch
        {
            0x0000 or 0x0011 or 0x0012 or 0x0013 => throw new NotImplementedException(
                $"sub_154BF (0x154BF): funcs_155E0[state 0x{currentProgramState:X4}] targets raw start (0x10000); this loop-specific slot remains unresolved at the dispatch seam."),
            _ => throw new NotImplementedException(
                $"sub_154BF (0x154BF): funcs_155E0 has no represented target for state 0x{currentProgramState:X4}.")
        };
    }
}
