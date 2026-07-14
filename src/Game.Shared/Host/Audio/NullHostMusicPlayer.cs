namespace Game.Shared.Host.Audio;

/// <summary>
///     No-op music player used when the host has nothing to play through.
/// </summary>
public sealed class NullHostMusicPlayer : IHostMusicPlayer
{
    /// <inheritdoc />
    public void Dispose()
    {
    }

    /// <inheritdoc />
    public void Play(string assetName, bool repeat, float volume)
    {
        _ = assetName;
        _ = repeat;
        _ = volume;
    }

    /// <inheritdoc />
    public void SetPaused(bool paused)
    {
        _ = paused;
    }

    /// <inheritdoc />
    public void SetVolume(float volume)
    {
        _ = volume;
    }

    /// <inheritdoc />
    public void Stop()
    {
    }
}
