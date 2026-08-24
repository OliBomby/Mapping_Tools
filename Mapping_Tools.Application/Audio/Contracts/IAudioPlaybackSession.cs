using Mapping_Tools.Application.Audio.Models;

namespace Mapping_Tools.Application.Audio.Contracts;

/// <summary>Owns one active playback device and its generated stream.</summary>
public interface IAudioPlaybackSession : IAsyncDisposable
{
    /// <summary>Gets the current playback state.</summary>
    AudioPlaybackState State { get; }

    /// <summary>Gets the best-effort current position.</summary>
    TimeSpan Position { get; }

    /// <summary>Completes when playback stops or fails.</summary>
    Task Completion { get; }

    /// <summary>Pauses output while retaining the current session.</summary>
    void Pause();

    /// <summary>Resumes output after a pause.</summary>
    void Resume();

    /// <summary>Stops output and releases device and stream resources.</summary>
    ValueTask StopAsync();
}

