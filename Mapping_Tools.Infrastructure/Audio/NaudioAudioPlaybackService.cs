using Mapping_Tools.Application.Audio;
using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Core.Audio;
using NAudio.Wave;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>Uses the host's WASAPI output while keeping the player lifetime behind an Application session.</summary>
public sealed class NaudioAudioPlaybackService : IAudioPlaybackService
{
    /// <inheritdoc />
    public Task<IAudioPlaybackSession> PlayAsync(
        AudioClip clip,
        AudioPlaybackOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clip);
        cancellationToken.ThrowIfCancellationRequested();
        if (clip.IsEmpty) throw new ArgumentException("An empty clip cannot be played.", nameof(clip));

        var session = new NaudioPlaybackSession(clip, options?.Loop == true);
        try
        {
            session.Start();
        }
        catch
        {
            session.DisposeAsync().AsTask().GetAwaiter().GetResult();
            throw;
        }

        return Task.FromResult<IAudioPlaybackSession>(session);
    }

    private sealed class NaudioPlaybackSession : IAudioPlaybackSession
    {
        private readonly TaskCompletionSource<object?> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object gate = new();
        private readonly bool loop;
        private readonly IWavePlayer player;
        private readonly RawSourceWaveStream stream;
        private bool disposed;
        private TimeSpan lastPosition;
        private AudioPlaybackState state = AudioPlaybackState.Stopped;
        private bool stopping;

        public NaudioPlaybackSession(AudioClip clip, bool loop)
        {
            this.loop = loop;
            byte[] bytes = new byte[clip.Samples.Length * sizeof(float)];
            Buffer.BlockCopy(clip.CopySamples(), 0, bytes, 0, bytes.Length);
            var format = WaveFormat.CreateIeeeFloatWaveFormat(clip.Format.SampleRate, clip.Format.Channels);
            stream = new RawSourceWaveStream(bytes, 0, bytes.Length, format);
            player = new WasapiOut();
            try
            {
                player.PlaybackStopped += OnPlaybackStopped;
                player.Init(stream);
            }
            catch
            {
                player.PlaybackStopped -= OnPlaybackStopped;
                player.Dispose();
                stream.Dispose();
                throw;
            }
        }

        public AudioPlaybackState State
        {
            get
            {
                lock (gate)
                {
                    return state;
                }
            }
        }

        public TimeSpan Position
        {
            get
            {
                lock (gate)
                {
                    return disposed ? lastPosition : stream.CurrentTime;
                }
            }
        }

        public Task Completion => completion.Task;

        public void Pause()
        {
            lock (gate)
            {
                if (disposed || state != AudioPlaybackState.Playing) return;

                player.Pause();
                state = AudioPlaybackState.Paused;
            }
        }

        public void Resume()
        {
            lock (gate)
            {
                if (disposed || state != AudioPlaybackState.Paused) return;

                player.Play();
                state = AudioPlaybackState.Playing;
            }
        }

        public ValueTask StopAsync()
        {
            Stop();
            return ValueTask.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            Stop();
            return ValueTask.CompletedTask;
        }

        public void Start()
        {
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                try
                {
                    player.Play();
                    state = AudioPlaybackState.Playing;
                }
                catch (Exception exception)
                {
                    state = AudioPlaybackState.Failed;
                    completion.TrySetException(exception);
                    throw;
                }
            }
        }

        private void Stop()
        {
            lock (gate)
            {
                if (disposed) return;

                stopping = true;
                state = AudioPlaybackState.Stopped;
                lastPosition = stream.CurrentTime;
                disposed = true;
                player.PlaybackStopped -= OnPlaybackStopped;
            }

            // WasapiOut.Stop can synchronously wait for PlaybackStopped. Do not hold
            // gate here because the callback also takes that lock.
            try
            {
                player.Stop();
            }
            finally
            {
                player.Dispose();
                stream.Dispose();
                completion.TrySetResult(null);
            }
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs eventArgs)
        {
            lock (gate)
            {
                if (disposed || stopping) return;

                if (eventArgs.Exception is not null)
                {
                    state = AudioPlaybackState.Failed;
                    completion.TrySetException(eventArgs.Exception);
                    return;
                }

                if (loop)
                {
                    try
                    {
                        stream.Position = 0;
                        player.Play();
                    }
                    catch (Exception exception)
                    {
                        state = AudioPlaybackState.Failed;
                        completion.TrySetException(exception);
                    }

                    return;
                }

                state = AudioPlaybackState.Stopped;
                completion.TrySetResult(null);
            }
        }
    }
}
