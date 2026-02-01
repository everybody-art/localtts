using System.IO;
using NAudio.Wave;

namespace LocalTTS.Services;

public class AudioPlayerService
{
    private WaveOutEvent? _waveOut;
    private WaveStream? _waveStream;

    public bool IsPlaying => _waveOut?.PlaybackState == PlaybackState.Playing;

    public void Play(byte[] audioData)
    {
        Stop();

        var ms = new MemoryStream(audioData);
        _waveStream = new Mp3FileReader(ms);
        _waveOut = new WaveOutEvent();
        _waveOut.Init(_waveStream);
        _waveOut.PlaybackStopped += (_, _) => Cleanup();
        _waveOut.Play();
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
        _waveOut?.Dispose();
        _waveStream?.Dispose();
        _waveOut = null;
        _waveStream = null;
    }
}
