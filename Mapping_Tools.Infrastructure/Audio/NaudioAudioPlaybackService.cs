using Mapping_Tools.Application.Audio;
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
        private readonly TaskCompletionSource<object?> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly object _gate = new();
        private readonly bool _loop;
        private readonly IWavePlayer _player;
        private readonly RawSourceWaveStream _stream;
        private bool _disposed;
        private TimeSpan _lastPosition;
        private AudioPlaybackState _state = AudioPlaybackState.Stopped;
        private bool _stopping;

        public NaudioPlaybackSession(AudioClip clip, bool loop)
        {
            _loop = loop;
            byte[] bytes = new byte[clip.Samples.Length * sizeof(float)];
            Buffer.BlockCopy(clip.CopySamples(), 0, bytes, 0, bytes.Length);
            var format = WaveFormat.CreateIeeeFloatWaveFormat(clip.Format.SampleRate, clip.Format.Channels);
            _stream = new RawSourceWaveStream(bytes, 0, bytes.Length, format);
            _player = new WasapiOut();
            try
            {
                _player.PlaybackStopped += OnPlaybackStopped;
                _player.Init(_stream);
            }
            catch
            {
                _player.PlaybackStopped -= OnPlaybackStopped;
                _player.Dispose();
                _stream.Dispose();
                throw;
            }
        }

        public AudioPlaybackState State
        {
            get
            {
                lock (_gate)
                {
                    return _state;
                }
            }
        }

        public TimeSpan Position
        {
            get
            {
                lock (_gate)
                {
                    return _disposed ? _lastPosition : _stream.CurrentTime;
                }
            }
        }

        public Task Completion => _completion.Task;

        public void Pause()
        {
            lock (_gate)
            {
                if (_disposed || _state != AudioPlaybackState.Playing) return;

                _player.Pause();
                _state = AudioPlaybackState.Paused;
            }
        }

        public void Resume()
        {
            lock (_gate)
            {
                if (_disposed || _state != AudioPlaybackState.Paused) return;

                _player.Play();
                _state = AudioPlaybackState.Playing;
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
            lock (_gate)
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                try
                {
                    _player.Play();
                    _state = AudioPlaybackState.Playing;
                }
                catch (Exception exception)
                {
                    _state = AudioPlaybackState.Failed;
                    _completion.TrySetException(exception);
                    throw;
                }
            }
        }

        private void Stop()
        {
            lock (_gate)
            {
                if (_disposed) return;

                _stopping = true;
                _state = AudioPlaybackState.Stopped;
                try
                {
                    _lastPosition = _stream.CurrentTime;
                    _player.Stop();
                }
                finally
                {
                    _player.PlaybackStopped -= OnPlaybackStopped;
                    _player.Dispose();
                    _stream.Dispose();
                    _disposed = true;
                    _completion.TrySetResult(null);
                }
            }
        }

        private void OnPlaybackStopped(object? sender, StoppedEventArgs eventArgs)
        {
            lock (_gate)
            {
                if (_disposed || _stopping) return;

                if (eventArgs.Exception is not null)
                {
                    _state = AudioPlaybackState.Failed;
                    _completion.TrySetException(eventArgs.Exception);
                    return;
                }

                if (_loop)
                {
                    try
                    {
                        _stream.Position = 0;
                        _player.Play();
                    }
                    catch (Exception exception)
                    {
                        _state = AudioPlaybackState.Failed;
                        _completion.TrySetException(exception);
                    }

                    return;
                }

                _state = AudioPlaybackState.Stopped;
                _completion.TrySetResult(null);
            }
        }
    }
}
