using System.ComponentModel;
using System.Diagnostics;
using Mapping_Tools.Application.Audio.Contracts;
using Mapping_Tools.Application.Audio.Models;
using Mapping_Tools.Core.Audio;
using NAudio.Wave;

namespace Mapping_Tools.Infrastructure.Audio;

/// <summary>
///     Plays generated previews through a native player available on the host
///     operating system.
/// </summary>
/// <remarks>
///     NAudio's WASAPI output is Windows-only. This adapter keeps the preview
///     feature available on macOS and Linux by handing a temporary PCM WAV file
///     to <c>afplay</c>, <c>paplay</c>, <c>aplay</c>, or <c>ffplay</c>.
/// </remarks>
public sealed class ProcessAudioPlaybackService : IAudioPlaybackService
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

        string path = Path.Combine(
            Path.GetTempPath(),
            $"mt_sample_preview_{Guid.NewGuid():N}.wav");
        try
        {
            var session = new ProcessAudioPlaybackSession(
                path,
                clip,
                clip.Duration,
                options?.Loop == true);
            session.Start();
            return Task.FromResult<IAudioPlaybackSession>(session);
        }
        catch
        {
            TryDelete(path);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed class ProcessAudioPlaybackSession : IAudioPlaybackSession
    {
        private readonly object gate = new();
        private readonly AudioClip clip;
        private readonly string path;
        private readonly TimeSpan duration;
        private readonly bool loop;
        private readonly TaskCompletionSource<object?> completion =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly CancellationTokenSource monitorCancellation = new();
        private readonly Stopwatch stopwatch = new();
        private Process? process;
        private AudioPlaybackState state = AudioPlaybackState.Stopped;
        private TimeSpan contentOffset;
        private bool disposed;

        public ProcessAudioPlaybackSession(
            string path,
            AudioClip clip,
            TimeSpan duration,
            bool loop)
        {
            this.path = path;
            this.clip = clip;
            this.duration = duration;
            this.loop = loop;
        }

        public AudioPlaybackState State
        {
            get
            {
                lock (gate) return state;
            }
        }

        public TimeSpan Position
        {
            get
            {
                lock (gate)
                {
                    TimeSpan position = contentOffset + stopwatch.Elapsed;
                    return loop ? position : Min(position, duration);
                }
            }
        }

        public Task Completion => completion.Task;

        public void Start()
        {
            WriteClip(path, clip, TimeSpan.Zero);
            Process started = StartPlayerProcess(path);
            lock (gate)
            {
                ObjectDisposedException.ThrowIf(disposed, this);
                process = started;
                state = AudioPlaybackState.Playing;
                stopwatch.Restart();
            }

            _ = MonitorAsync(started, monitorCancellation.Token);
        }

        public void Pause()
        {
            Process? processToStop;
            TimeSpan pausedPosition;
            lock (gate)
            {
                if (disposed || state != AudioPlaybackState.Playing) return;

                processToStop = process;
                pausedPosition = contentOffset + stopwatch.Elapsed;
                if (loop && duration > TimeSpan.Zero)
                    pausedPosition = TimeSpan.FromTicks(pausedPosition.Ticks % duration.Ticks);

                contentOffset = pausedPosition;
                stopwatch.Reset();
                process = null;
                state = AudioPlaybackState.Paused;
            }

            StopProcess(processToStop);
        }

        public void Resume()
        {
            TimeSpan resumePosition;
            lock (gate)
            {
                if (disposed || state != AudioPlaybackState.Paused) return;

                resumePosition = contentOffset;
                if (resumePosition >= duration)
                {
                    state = AudioPlaybackState.Stopped;
                    contentOffset = duration;
                    completion.TrySetResult(null);
                    return;
                }
            }

            Process started;
            try
            {
                WriteClip(path, clip, resumePosition);
                started = StartPlayerProcess(path);
            }
            catch (Exception exception)
            {
                lock (gate)
                {
                    state = AudioPlaybackState.Failed;
                    completion.TrySetException(exception);
                }

                return;
            }

            lock (gate)
            {
                if (disposed)
                {
                    StopProcess(started);
                    return;
                }

                process = started;
                state = AudioPlaybackState.Playing;
                contentOffset = resumePosition;
                stopwatch.Restart();
            }

            _ = MonitorAsync(started, monitorCancellation.Token);
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

        private async Task MonitorAsync(Process monitored, CancellationToken cancellationToken)
        {
            try
            {
                await monitored.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            bool restart;
            lock (gate)
            {
                if (disposed || !ReferenceEquals(process, monitored)) return;

                process = null;
                monitored.Dispose();
                restart = loop;
                if (restart)
                {
                    contentOffset = TimeSpan.Zero;
                    stopwatch.Restart();
                }
                else
                {
                    contentOffset = duration;
                    stopwatch.Stop();
                    state = AudioPlaybackState.Stopped;
                    completion.TrySetResult(null);
                }
            }

            if (restart)
            {
                try
                {
                    WriteClip(path, clip, TimeSpan.Zero);
                    Process next = StartPlayerProcess(path);
                    lock (gate)
                    {
                        if (disposed)
                        {
                            StopProcess(next);
                            return;
                        }

                        process = next;
                    }

                    _ = MonitorAsync(next, cancellationToken);
                }
                catch (Exception exception)
                {
                    lock (gate)
                    {
                        state = AudioPlaybackState.Failed;
                        completion.TrySetException(exception);
                    }
                }
            }
            else
            {
                TryDelete(path);
            }
        }

        private void Stop()
        {
            Process? processToStop;
            lock (gate)
            {
                if (disposed) return;

                disposed = true;
                state = AudioPlaybackState.Stopped;
                stopwatch.Stop();
                processToStop = process;
                process = null;
                monitorCancellation.Cancel();
                completion.TrySetResult(null);
            }

            StopProcess(processToStop);
            TryDelete(path);
        }

        private static void StopProcess(Process? process)
        {
            if (process is null) return;

            try
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }
            catch (Win32Exception)
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        private static Process StartPlayerProcess(string path)
        {
            foreach (var command in GetPlayerCommands(path))
            {
                try
                {
                    var info = new ProcessStartInfo(command.FileName)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    };
                    foreach (string argument in command.Arguments) info.ArgumentList.Add(argument);

                    return Process.Start(info)
                           ?? throw new InvalidOperationException(
                               $"The native audio player '{command.FileName}' did not start.");
                }
                catch (Win32Exception) when (!OperatingSystem.IsWindows())
                {
                }
            }

            throw new PlatformNotSupportedException(
                "No supported native audio player was found. Install afplay, paplay, aplay, or ffplay.");
        }

        private static void WriteClip(string path, AudioClip clip, TimeSpan offset)
        {
            long startFrame = (long)(offset.TotalSeconds * clip.Format.SampleRate);
            startFrame = Math.Clamp(startFrame, 0, clip.FrameCount);
            int startSample = checked((int)(startFrame * clip.Format.Channels));
            float[] samples = clip.Samples.Span[startSample..].ToArray();
            if (samples.Length == 0)
                throw new InvalidOperationException("The audio clip has no samples remaining to play.");

            var remaining = new AudioClip(clip.Format, samples);
            WaveFileWriter.CreateWaveFile(
                path,
                new AudioClipSampleProvider(remaining).ToWaveProvider16());
        }

        private static IEnumerable<PlayerCommand> GetPlayerCommands(string path)
        {
            if (OperatingSystem.IsWindows())
            {
                string escapedPath = path.Replace("'", "''", StringComparison.Ordinal);
                yield return new PlayerCommand(
                    "powershell.exe",
                    ["-NoProfile", "-NonInteractive", "-Command", $"(New-Object Media.SoundPlayer '{escapedPath}').PlaySync()"]);
                yield break;
            }

            if (OperatingSystem.IsMacOS())
            {
                yield return new PlayerCommand("afplay", [path]);
                yield return new PlayerCommand("ffplay", ["-nodisp", "-autoexit", "-loglevel", "quiet", path]);
                yield break;
            }

            yield return new PlayerCommand("paplay", [path]);
            yield return new PlayerCommand("aplay", ["-q", path]);
            yield return new PlayerCommand("ffplay", ["-nodisp", "-autoexit", "-loglevel", "quiet", path]);
        }

        private static TimeSpan Min(TimeSpan left, TimeSpan right)
        {
            return left < right ? left : right;
        }

        private sealed record PlayerCommand(string FileName, IReadOnlyList<string> Arguments);
    }
}
