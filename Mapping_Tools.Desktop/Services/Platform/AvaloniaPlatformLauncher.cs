using Avalonia.Platform.Storage;
using Mapping_Tools.Application.Platform;

namespace Mapping_Tools.Desktop.Services.Platform;

/// <summary>
///     Adapts the launcher owned by an initialized Avalonia top-level window to
///     the application's URI, file, and directory launch contract.
/// </summary>
public sealed class AvaloniaPlatformLauncher : IPlatformLauncher
{
    private readonly Func<ILauncher?> launcherAccessor;

    /// <summary>
    ///     Creates an adapter that resolves the launcher lazily, after the window exists.
    /// </summary>
    /// <param name="launcherAccessor">Returns the current top-level launcher, if initialized.</param>
    public AvaloniaPlatformLauncher(Func<ILauncher?> launcherAccessor)
    {
        this.launcherAccessor = launcherAccessor
                                ?? throw new ArgumentNullException(nameof(launcherAccessor));
    }

    /// <inheritdoc />
    public async Task<bool> OpenUriAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri) throw new ArgumentException("The URI must be absolute.", nameof(uri));

        cancellationToken.ThrowIfCancellationRequested();
        bool launched = await GetLauncher().LaunchUriAsync(uri);
        cancellationToken.ThrowIfCancellationRequested();
        return launched;
    }

    /// <inheritdoc />
    public async Task<bool> OpenFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath)) throw new FileNotFoundException("The file to open does not exist.", fullPath);

        cancellationToken.ThrowIfCancellationRequested();
        bool launched = await GetLauncher().LaunchFileInfoAsync(new FileInfo(fullPath));
        cancellationToken.ThrowIfCancellationRequested();
        return launched;
    }

    /// <inheritdoc />
    public async Task<bool> OpenFolderAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath)) throw new DirectoryNotFoundException($"The folder to open does not exist: '{fullPath}'.");

        cancellationToken.ThrowIfCancellationRequested();
        bool launched = await GetLauncher().LaunchDirectoryInfoAsync(new DirectoryInfo(fullPath));
        cancellationToken.ThrowIfCancellationRequested();
        return launched;
    }

    private ILauncher GetLauncher()
    {
        return launcherAccessor()
               ?? throw new InvalidOperationException(
                   "Launcher access requires an initialized Avalonia top-level window.");
    }
}
