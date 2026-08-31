using System.ComponentModel;
using System.Diagnostics;
using Mapping_Tools.Application.Platform;

namespace Mapping_Tools.Infrastructure.Platform;

/// <summary>
///     Reveals a file or directory with the graphical file manager provided by
///     Windows, macOS, or Linux.
/// </summary>
public sealed class PortableFileRevealService : IFileRevealService
{
    private readonly Func<PortableFileRevealPlatform> getPlatform;
    private readonly Func<ProcessStartInfo, Process?> startProcess;

    /// <summary>
    ///     Creates a file-reveal service using the current operating system and
    ///     the platform process launcher.
    /// </summary>
    public PortableFileRevealService()
        : this(GetCurrentPlatform, Process.Start)
    {
    }

    internal PortableFileRevealService(
        Func<PortableFileRevealPlatform> getPlatform,
        Func<ProcessStartInfo, Process?> startProcess)
    {
        this.getPlatform = getPlatform ?? throw new ArgumentNullException(nameof(getPlatform));
        this.startProcess = startProcess ?? throw new ArgumentNullException(nameof(startProcess));
    }

    /// <inheritdoc />
    /// <remarks>
    ///     Linux file managers cannot generally select an individual file, so a
    ///     file request opens its containing directory. A missing file-manager
    ///     executable is reported as an unsuccessful request instead of escaping
    ///     as a process-start exception.
    /// </remarks>
    public Task<bool> RevealAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        string fullPath = Path.GetFullPath(path);
        bool isDirectory = Directory.Exists(fullPath);
        if (!isDirectory && !File.Exists(fullPath))
            throw new FileNotFoundException("The path to reveal does not exist.", fullPath);

        ProcessStartInfo? startInfo = CreateStartInfo(
            getPlatform(),
            fullPath,
            isDirectory);
        if (startInfo is null) return Task.FromResult(false);

        try
        {
            return Task.FromResult(startProcess(startInfo) is not null);
        }
        catch (Win32Exception)
        {
            return Task.FromResult(false);
        }
        catch (InvalidOperationException)
        {
            return Task.FromResult(false);
        }
        catch (FileNotFoundException)
        {
            return Task.FromResult(false);
        }
    }

    internal static ProcessStartInfo? CreateStartInfo(
        PortableFileRevealPlatform platform,
        string fullPath,
        bool isDirectory)
    {
        return platform switch
        {
            PortableFileRevealPlatform.Windows => CreateWindowsStartInfo(fullPath, isDirectory),
            PortableFileRevealPlatform.MacOs => CreateMacOsStartInfo(fullPath),
            PortableFileRevealPlatform.Linux => CreateLinuxStartInfo(fullPath, isDirectory),
            _ => null,
        };
    }

    private static ProcessStartInfo CreateWindowsStartInfo(string fullPath, bool isDirectory)
    {
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

        return startInfo;
    }

    private static ProcessStartInfo CreateMacOsStartInfo(string fullPath)
    {
        ProcessStartInfo startInfo = new("open");
        startInfo.ArgumentList.Add("-R");
        startInfo.ArgumentList.Add(fullPath);
        return startInfo;
    }

    private static ProcessStartInfo CreateLinuxStartInfo(string fullPath, bool isDirectory)
    {
        ProcessStartInfo startInfo = new("xdg-open");
        startInfo.ArgumentList.Add(isDirectory
            ? fullPath
            : Directory.GetParent(fullPath)?.FullName ?? fullPath);
        return startInfo;
    }

    private static PortableFileRevealPlatform GetCurrentPlatform()
    {
        if (OperatingSystem.IsWindows()) return PortableFileRevealPlatform.Windows;

        if (OperatingSystem.IsMacOS()) return PortableFileRevealPlatform.MacOs;

        if (OperatingSystem.IsLinux()) return PortableFileRevealPlatform.Linux;

        return PortableFileRevealPlatform.Unsupported;
    }
}

internal enum PortableFileRevealPlatform
{
    Unsupported,
    Windows,
    MacOs,
    Linux,
}
