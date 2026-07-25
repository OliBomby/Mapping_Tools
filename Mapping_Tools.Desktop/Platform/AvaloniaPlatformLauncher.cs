using Avalonia.Platform.Storage;
using Mapping_Tools.ApplicationServices.Platform;

namespace Mapping_Tools.Desktop.Platform;

public sealed class AvaloniaPlatformLauncher : IPlatformLauncher
{
    private readonly Func<ILauncher?> _launcherAccessor;

    public AvaloniaPlatformLauncher(Func<ILauncher?> launcherAccessor)
    {
        _launcherAccessor = launcherAccessor
            ?? throw new ArgumentNullException(nameof(launcherAccessor));
    }

    public async Task<bool> OpenUriAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri)
        {
            throw new ArgumentException("The URI must be absolute.", nameof(uri));
        }

        cancellationToken.ThrowIfCancellationRequested();
        bool launched = await GetLauncher().LaunchUriAsync(uri);
        cancellationToken.ThrowIfCancellationRequested();
        return launched;
    }

    public async Task<bool> OpenFileAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The file to open does not exist.", fullPath);
        }

        cancellationToken.ThrowIfCancellationRequested();
        bool launched = await GetLauncher().LaunchFileInfoAsync(new FileInfo(fullPath));
        cancellationToken.ThrowIfCancellationRequested();
        return launched;
    }

    public async Task<bool> OpenFolderAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string fullPath = Path.GetFullPath(path);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"The folder to open does not exist: '{fullPath}'.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        bool launched = await GetLauncher().LaunchDirectoryInfoAsync(new DirectoryInfo(fullPath));
        cancellationToken.ThrowIfCancellationRequested();
        return launched;
    }

    private ILauncher GetLauncher()
    {
        return _launcherAccessor()
            ?? throw new InvalidOperationException(
                "Launcher access requires an initialized Avalonia top-level window.");
    }
}
