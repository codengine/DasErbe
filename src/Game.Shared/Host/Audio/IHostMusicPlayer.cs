namespace Game.Shared.Host.Audio;

/// <summary>
///     Plays the streamed music assets the game asks for.
/// </summary>
public interface IHostMusicPlayer : IDisposable
{
    /// <summary>
    ///     Starts one music asset.
    /// </summary>
    /// <param name="assetName">Host content asset name without an extension.</param>
    /// <param name="repeat"><see langword="true" /> to loop when playback reaches the end.</param>
    /// <param name="volume">Initial volume in the range [0, 1].</param>
    void Play(string assetName, bool repeat, float volume);

    /// <summary>
    ///     Updates the active music volume.
    /// </summary>
    /// <param name="volume">Volume in the range [0, 1].</param>
    void SetVolume(float volume);

    /// <summary>
    ///     Pauses or resumes playback.
    /// </summary>
    /// <param name="paused"><see langword="true" /> to pause playback.</param>
    void SetPaused(bool paused);

    /// <summary>
    ///     Stops playback.
    /// </summary>
    void Stop();
}
