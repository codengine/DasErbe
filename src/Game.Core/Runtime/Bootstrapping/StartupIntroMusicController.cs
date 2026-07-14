namespace Game.Runtime.Bootstrapping;

/// <summary>
///     Coordinates the host-added startup intro music with the recovered startup-title sequence.
/// </summary>
/// <param name="runtime">The runtime-owned runtime whose host music seam plays the intro asset.</param>
internal sealed class StartupIntroMusicController(Erbe runtime)
{
    private const string IntroMusicAssetName = "intro";
    private const ushort FullFadeStep = 0x0080;
    private bool _started;
    private bool _stopped;

    /// <summary>
    ///     Starts the intro music once for the current startup-title sequence.
    /// </summary>
    internal void Start()
    {
        if (_started)
        {
            return;
        }

        // Host-specific addition: the original startup-title symbol has no recovered music contract, so this uses the
        // engine-hosted content music seam while keeping the visual IDA 0x152DD orchestration unchanged.
        runtime.Music.Play(IntroMusicAssetName, true, 1f);
        _started = true;
        _stopped = false;
    }

    /// <summary>
    ///     Applies the current startup palette fade-out step as a proportional music volume.
    /// </summary>
    /// <param name="fadeStep">The current startup fade step, where 0x80 is full volume and 0 is silent.</param>
    internal void ApplyFadeVolume(ushort fadeStep)
    {
        if (!_started || _stopped)
        {
            return;
        }

        runtime.Music.SetVolume(Math.Clamp(fadeStep / (float)FullFadeStep, 0f, 1f));
    }

    /// <summary>
    ///     Stops the intro music once the startup fade-out reaches black.
    /// </summary>
    internal void Stop()
    {
        if (!_started || _stopped)
        {
            return;
        }

        runtime.Music.Stop();
        _stopped = true;
    }
}
