using System.Diagnostics;
using Mapping_Tools.ApplicationServices.Platform;

namespace Mapping_Tools.Infrastructure.Platform;

public sealed class WindowsFileRevealService : IFileRevealService
{
    public Task<bool> RevealAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        if (!OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException(
                "Selecting a file in its file manager is currently supported only on Windows.");
        }

        string fullPath = Path.GetFullPath(path);
        bool isDirectory = Directory.Exists(fullPath);
        if (!isDirectory && !File.Exists(fullPath))
        {
            throw new FileNotFoundException("The path to reveal does not exist.", fullPath);
        }

        ProcessStartInfo startInfo = new("explorer.exe");
        if (isDirectory)
        {
            startInfo.ArgumentList.Add(fullPath);
        }
        else
        {
            startInfo.ArgumentList.Add("/select,");
            startInfo.ArgumentList.Add(fullPath);
        }

        Process? process = Process.Start(startInfo);
        return Task.FromResult(process is not null);
    }
}
