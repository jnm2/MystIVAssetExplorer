using MystIVAssetExplorer.Formats;
using NAudio.Vorbis;
using NAudio.Wave;
using System;
using System.IO;

namespace MystIVAssetExplorer.Services;

public sealed class AudioService
{
    private MemoryStream? currentDataStream;
    private WaveStream? currentWaveStream;
    private WasapiOut? audioOutput;

    public event EventHandler? PlaybackStopped;

    public void SetAudioFile(MemoryStream stream, SoundStreamFormat format)
    {
        audioOutput?.PlaybackStopped -= AudioOutput_PlaybackStopped;
        audioOutput?.Dispose();
        currentWaveStream?.Dispose();
        currentDataStream?.Dispose();

        currentDataStream = stream;
        currentWaveStream = format switch
        {
            SoundStreamFormat.PCM => new WaveFileReader(stream),
            SoundStreamFormat.OggVorbis => new VorbisWaveReader(stream),
        };

        audioOutput = new WasapiOut();
        audioOutput.Init(currentWaveStream);
        audioOutput.PlaybackStopped += AudioOutput_PlaybackStopped;
    }

    public void Play() => audioOutput?.Play();

    public void Pause() => audioOutput?.Pause();

    private void AudioOutput_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        PlaybackStopped?.Invoke(this, EventArgs.Empty);
    }
}
