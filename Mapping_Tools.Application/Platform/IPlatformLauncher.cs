namespace Mapping_Tools.ApplicationServices.Platform;

public interface IPlatformLauncher
{
    Task<bool> OpenUriAsync(Uri uri, CancellationToken cancellationToken = default);

    Task<bool> OpenFileAsync(string path, CancellationToken cancellationToken = default);

    Task<bool> OpenFolderAsync(string path, CancellationToken cancellationToken = default);
}
