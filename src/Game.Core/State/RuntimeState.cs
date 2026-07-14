using Game.DataBlock;

namespace Game.State;

internal sealed class RuntimeState
{
    /// <summary>
    ///     Height in rows of the upper stage area where the game scene is drawn before the lower panel begins.
    /// </summary>
    internal const int StageHeight = 128;

    internal const int FrameHeight = 200;
    internal const int FrameWidth = 320;
    internal const int FrameByteCount = FrameWidth * FrameHeight;

    internal InputState Input { get; } = new();
    internal PointerOverlayCompatibilityState PointerOverlay { get; } = new();
    internal PresentationState Presentation { get; } = new();
    internal ProgramState Program { get; } = new();
    internal TransitionEffectCompatibilityState TransitionEffect { get; } = new();

    internal bool IsInitialized { get; private set; }
    internal DataBlockModel RawDataBlock { get; } = new();

    internal void Initialize(ReadOnlySpan<byte> source)
    {
        RawDataBlock.Initialize(source);
        IsInitialized = true;
    }

    internal byte[] GetSnapshot()
    {
        return DataBlockWriter.Write(RawDataBlock);
    }
}
