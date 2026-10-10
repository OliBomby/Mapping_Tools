using System.ComponentModel;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Infrastructure.Editor.Memory;

namespace Mapping_Tools.Infrastructure.Editor;

/// <summary>Reads unsaved osu!stable editor state on Windows and under Wine on Linux.</summary>
public sealed class StableMemoryLiveBeatmapReader : ILiveBeatmapReader, IDisposable
{
    private readonly ApplicationSettings settings;
    private readonly IApplicationDirectories directories;
    private readonly Func<bool> isSupported;
    private readonly Lock lifecycleGate = new();
    private readonly SemaphoreSlim readerLock = new(1, 1);
    private EditorProcessMemory? processMemory;
    private StableEditorMemoryReader? reader;
    private int activeReads;
    private bool disposed;

    /// <summary>Initializes a reader using the native Songs path and diagnostic directory.</summary>
    /// <param name="settings">Settings containing the osu! Songs directory.</param>
    /// <param name="directories">Application-owned locations for validation logs.</param>
    public StableMemoryLiveBeatmapReader(ApplicationSettings settings, IApplicationDirectories directories)
        : this(settings, directories, () => OperatingSystem.IsWindows() || OperatingSystem.IsLinux())
    {
    }

    internal StableMemoryLiveBeatmapReader(ApplicationSettings settings, IApplicationDirectories directories,
        Func<bool> isSupported)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.directories = directories ?? throw new ArgumentNullException(nameof(directories));
        this.isSupported = isSupported ?? throw new ArgumentNullException(nameof(isSupported));
    }

    /// <inheritdoc />
    public async Task<LiveBeatmapSnapshot?> ReadAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!isSupported()) return null;

        EnterRead();
        bool lockTaken = false;
        try
        {
            await readerLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockTaken = true;
            var snapshot = await Task.Run(() => ReadSnapshot(cancellationToken), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            return snapshot;
        }
        finally
        {
            if (lockTaken) readerLock.Release();
            ExitRead();
        }
    }

    private LiveBeatmapSnapshot? ReadSnapshot(CancellationToken cancellationToken)
    {
        try
        {
            if (processMemory is null || processMemory.HasExited)
            {
                processMemory?.Dispose();
                processMemory = EditorProcessMemory.Open();
                reader = processMemory is null ? null : new StableEditorMemoryReader(processMemory);
            }

            return reader?.ReadSnapshot(settings.SongsPath, cancellationToken);
        }
        catch (InvalidDataException exception)
        {
            directories.EnsureCreated();
            File.WriteAllText(Path.Combine(directories.ApplicationData, "editor_reader_error.txt"), exception.ToString());
            throw;
        }
        catch (InvalidOperationException)
        {
            ResetProcess();
            return null;
        }
        catch (Win32Exception)
        {
            ResetProcess();
            return null;
        }
        catch (IOException) when (processMemory?.HasExited == true)
        {
            ResetProcess();
            return null;
        }
    }

    private void ResetProcess()
    {
        processMemory?.Dispose();
        processMemory = null;
        reader = null;
    }

    private void EnterRead()
    {
        lock (lifecycleGate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            activeReads++;
        }
    }

    private void ExitRead()
    {
        bool releaseResources;
        lock (lifecycleGate)
        {
            activeReads--;
            releaseResources = disposed && activeReads == 0;
        }

        if (releaseResources) ReleaseResources();
    }

    /// <summary>Releases the process and synchronization resources after active reads finish.</summary>
    public void Dispose()
    {
        bool releaseResources;
        lock (lifecycleGate)
        {
            if (disposed) return;
            disposed = true;
            releaseResources = activeReads == 0;
        }

        if (releaseResources) ReleaseResources();
    }

    private void ReleaseResources()
    {
        ResetProcess();
        readerLock.Dispose();
    }
}
