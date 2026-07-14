using Game.Shared.Host.Audio;
using Game.Shared.Diagnostics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Media;

namespace Game.Desktop.MonoGame;

/// <summary>
///     Plays song assets through <see cref="MediaPlayer" />.
/// </summary>
/// <param name="content">Content manager used to load song assets.</param>
public sealed class MonoGameSongPlayer(ContentManager content) : IHostMusicPlayer
{
    private bool _isPaused;
    private bool _isPlaying;

    /// <inheritdoc />
    public void Dispose()
    {
        Stop();
    }

    /// <inheritdoc />
    public void Play(string assetName, bool repeat, float volume)
    {
        GameLog.Debug(LoggingChannel.Program, $"Starting song '{assetName}'.");
        Song song;
        try
        {
            song = content.Load<Song>(assetName);
        }
        catch (Exception ex)
        {
            GameLog.Error(LoggingChannel.Files, $"Failed to load song '{assetName}'.", ex);
            throw;
        }

        if (MediaPlayer.State != MediaState.Stopped)
        {
            MediaPlayer.Stop();
        }

        MediaPlayer.IsRepeating = repeat;
        MediaPlayer.Volume = ClampVolume(volume);
        MediaPlayer.Play(song);
        _isPlaying = true;
        if (_isPaused)
        {
            MediaPlayer.Pause();
        }
    }

    /// <inheritdoc />
    public void SetPaused(bool paused)
    {
        _isPaused = paused;
        if (!_isPlaying)
        {
            return;
        }

        if (paused)
        {
            if (MediaPlayer.State == MediaState.Playing)
            {
                MediaPlayer.Pause();
            }

            return;
        }

        if (MediaPlayer.State == MediaState.Paused)
        {
            MediaPlayer.Resume();
        }
    }

    /// <inheritdoc />
    public void SetVolume(float volume)
    {
        MediaPlayer.Volume = ClampVolume(volume);
    }

    /// <inheritdoc />
    public void Stop()
    {
        if (!_isPlaying && MediaPlayer.State == MediaState.Stopped)
        {
            return;
        }

        GameLog.Debug(LoggingChannel.Program, "Stopping song playback.");
        MediaPlayer.Stop();
        _isPlaying = false;
    }

    private static float ClampVolume(float volume)
    {
        return float.IsNaN(volume) ? 0f : Math.Clamp(volume, 0f, 1f);
    }
}
