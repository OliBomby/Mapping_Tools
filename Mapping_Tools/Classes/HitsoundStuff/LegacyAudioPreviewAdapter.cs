#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Infrastructure.Audio;

namespace Mapping_Tools.Classes.HitsoundStuff;

/// <summary>
/// Provides a WPF-side compatibility adapter for the new audio preview boundary.
/// The legacy Hitsound Studio editor may keep its existing orchestration until step 42,
/// while preview callers can opt into deterministic Application-owned playback sessions.
/// </summary>
public sealed class LegacyAudioPreviewAdapter : IAsyncDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly AudioPreviewService _preview;
    private IAudioPlaybackSession? _session;
    private bool _disposed;

    /// <summary>Creates the compatibility adapter with the concrete legacy audio stack.</summary>
    public LegacyAudioPreviewAdapter()
    {
        NaudioAudioDecoder decoder = new();
        NaudioSoundFontRenderer soundFontRenderer = new();
        NaudioAudioEffectService effects = new();
        NaudioAudioGenerator generator = new(decoder, soundFontRenderer, effects);
        _preview = new AudioPreviewService(
            decoder,
            generator,
            new NaudioAudioPlaybackService(),
            new FastFourierSpectrumCalculator());
    }

    /// <summary>
    /// Stops any previous preview and starts the requested legacy sample.
    /// </summary>
    /// <param name="sample">The existing mutable sample-generation arguments.</param>
    /// <param name="cancellationToken">Token shared by generation and playback startup.</param>
    /// <returns>The active Application playback session.</returns>
    public async Task<IAudioPlaybackSession> PreviewAsync(
        SampleGeneratingArgs sample,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(sample);
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            await StopCoreAsync().ConfigureAwait(false);
            IAudioPlaybackSession session = await _preview.PreviewGeneratedAsync(
                new AudioGenerationRequest(sample),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            _session = session;
            return session;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Stops the current preview, if one exists.</summary>
    public async ValueTask StopAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await StopCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async ValueTask StopCoreAsync()
    {
        if (_session is null)
        {
            return;
        }

        try
        {
            await _session.DisposeAsync().ConfigureAwait(false);
        }
        finally
        {
            _session = null;
        }
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            await StopCoreAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }
}
