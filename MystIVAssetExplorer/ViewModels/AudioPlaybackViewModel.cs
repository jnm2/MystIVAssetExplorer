using Avalonia.Threading;
using MystIVAssetExplorer.Formats;
using NAudio.Vorbis;
using NAudio.Wave;
using ReactiveUI;
using System;
using System.IO;
using System.Reactive;

namespace MystIVAssetExplorer.ViewModels;

public sealed class AudioPlaybackViewModel : ViewModelBase, IDisposable
{
    private MemoryStream? currentDataStream;
    private LoopableStream? currentWaveStream;
    private WasapiOut? audioOutput;
    private readonly DispatcherTimer timer;

    public string? AudioName { get; private set => this.RaiseAndSetIfChanged(ref field, value); }

    public bool IsPlaying
    {
        get;
        set
        {
            if (value)
            {
                audioOutput?.PlaybackStopped += AudioOutput_PlaybackStopped;
                audioOutput?.Play();
            }
            else
            {
                audioOutput?.PlaybackStopped -= AudioOutput_PlaybackStopped;
                audioOutput?.Pause();
            }

            this.RaiseAndSetIfChanged(ref field, value);
            timer.IsEnabled = value;
        }
    }

    public bool IsLooping
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            currentWaveStream?.EnableLooping = value;
        }
    } = true;

    public ReactiveCommand<Unit, Unit> StopCommand { get; }

    public AudioPlaybackViewModel()
    {
        timer = new(TimeSpan.FromSeconds(0.01), DispatcherPriority.Normal, (_, _) => RaiseCurrentTimeChanged());

        StopCommand = ReactiveCommand.Create(() =>
        {
            if (audioOutput is null) return;

            audioOutput.PlaybackStopped -= AudioOutput_PlaybackStopped;
            audioOutput.Stop();
            currentWaveStream!.Seek(0, SeekOrigin.Begin);
            IsPlaying = false;
            RaiseCurrentTimeChanged();
        });
    }

    public void SwitchAudioFile(string name, MemoryStream audioData, SoundStreamFormat format)
    {
        IsPlaying = false;
        audioOutput?.Dispose();
        currentWaveStream?.Dispose();
        currentDataStream?.Dispose();

        currentDataStream = audioData;
        currentWaveStream = new LoopableStream(format switch
        {
            SoundStreamFormat.PCM => new WaveFileReader(currentDataStream),
            SoundStreamFormat.OggVorbis => new VorbisWaveReader(currentDataStream),
        }) { EnableLooping = IsLooping };

        TotalTime = currentWaveStream.TotalTime;

        audioOutput = new WasapiOut();
        audioOutput.Init(currentWaveStream);
        IsPlaying = true;

        AudioName = name;
    }

    private bool isRaisingTotalOrCurrentTimeChanged;
    public TimeSpan CurrentTime
    {
        get => currentWaveStream?.CurrentTime ?? default;
        set
        {
            if (isRaisingTotalOrCurrentTimeChanged) return;
            currentWaveStream?.CurrentTime = value;
        }
    }

    private void RaiseCurrentTimeChanged()
    {
        isRaisingTotalOrCurrentTimeChanged = true;
        try
        {
            this.RaisePropertyChanged(nameof(CurrentTime));
        }
        finally
        {
            isRaisingTotalOrCurrentTimeChanged = false;
        }
    }

    public TimeSpan TotalTime
    {
        get;
        private set
        {
            isRaisingTotalOrCurrentTimeChanged = true;
            try
            {
                this.RaiseAndSetIfChanged(ref field, value);
            }
            finally
            {
                isRaisingTotalOrCurrentTimeChanged = false;
            }
        }
    }

    private void AudioOutput_PlaybackStopped(object? sender, StoppedEventArgs e)
    {
        currentWaveStream!.Seek(0, SeekOrigin.Begin);
        RaiseCurrentTimeChanged();
        IsPlaying = false;
    }

    public void Dispose()
    {
        IsPlaying = false;
        audioOutput?.Dispose();
        currentWaveStream?.Dispose();
        currentDataStream?.Dispose();
    }
}
