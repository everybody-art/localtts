using System.IO;
using NAudio.Wave;

namespace LocalTTS.Services;

public class AudioPlayerService
{
    private WaveOutEvent? _waveOut;
    private WaveStream? _waveStream;

    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

    public double CurrentPositionSeconds
    {
        get
        {
            if (_waveOut == null || _waveStream == null) return 0;
            // Use output device position (bytes actually played) for accuracy
            var bytesPlayed = _waveOut.GetPosition();
            var bytesPerSecond = _waveStream.WaveFormat.AverageBytesPerSecond;
            return (double)bytesPlayed / bytesPerSecond;
        }
    }

    public event Action? PlaybackStopped;

    public void Play(byte[] audioData)
    {
        Stop();

        var ms = new MemoryStream(audioData);
        _waveStream = new Mp3FileReader(ms);
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_waveStream);
        _waveOut.PlaybackStopped += OnPlaybackStopped;
        _waveOut.Play();
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        PlaybackStopped?.Invoke();
        Cleanup();
    }

    public void Stop()
    {
        if (_waveOut != null)
        {
            _waveOut.Stop();
            Cleanup();
        }
    }

    private void Cleanup()
    {
        if (_waveOut != null)
        {
            _waveOut.PlaybackStopped -= OnPlaybackStopped;
            _waveOut.Dispose();
            _waveOut = null;
        }
        _waveStream?.Dispose();
        _waveStream = null;
    }
}
