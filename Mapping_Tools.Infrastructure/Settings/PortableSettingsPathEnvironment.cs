using System.Runtime.Versioning;
using System.Security;
using Mapping_Tools.Application.Settings.Contracts;
using Microsoft.Win32;

namespace Mapping_Tools.Infrastructure.Settings;

/// <summary>
///     Resolves osu! paths using the legacy Windows registry when available and
///     conservative filesystem probes on other desktop platforms.
/// </summary>
public sealed class PortableSettingsPathEnvironment : ISettingsPathEnvironment
{
    private readonly Func<bool> isWindows;
    private readonly Func<string, string?> getEnvironmentVariable;
    private readonly Func<string, bool> directoryExists;

    /// <summary>
    ///     Creates an environment using the current operating system and process
    ///     environment.
    /// </summary>
    public PortableSettingsPathEnvironment()
        : this(
            OperatingSystem.IsWindows,
            Environment.GetEnvironmentVariable,
            Directory.Exists)
    {
    }

    internal PortableSettingsPathEnvironment(
        Func<bool> isWindows,
        Func<string, string?> getEnvironmentVariable,
        Func<string, bool> directoryExists)
    {
        this.isWindows = isWindows ?? throw new ArgumentNullException(nameof(isWindows));
        this.getEnvironmentVariable = getEnvironmentVariable
                                      ?? throw new ArgumentNullException(nameof(getEnvironmentVariable));
        this.directoryExists = directoryExists ?? throw new ArgumentNullException(nameof(directoryExists));
    }

    /// <inheritdoc />
    public string UserName => Environment.UserName;

    /// <inheritdoc />
    /// <remarks>
    ///     Windows keeps the two legacy uninstall registry locations. Other
    ///     platforms are probed only at conventional user-owned paths; an
    ///     inaccessible or absent candidate is ignored.
    /// </remarks>
    public string? FindOsuInstallation()
    {
        if (OperatingSystem.IsWindows() && isWindows()) return FindWindowsInstallation();

        foreach (string candidate in GetNonWindowsCandidates())
        {
            try
            {
                if (directoryExists(candidate)) return candidate;
            }
            catch (ArgumentException)
            {
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return null;
    }

    /// <inheritdoc />
    public string GetBeatmapDirectory(string configPath)
    {
        try
        {
            foreach (string line in File.ReadLines(configPath))
            {
                string[] parts = line.Split('=', 2);
                if (parts.Length != 2
                    || !parts[0].Trim().Equals(
                        "BeatmapDirectory",
                        StringComparison.Ordinal))
                    continue;

                string value = parts[1].Trim();
                return string.IsNullOrWhiteSpace(value) ? "Songs" : value;
            }
        }
        catch (ArgumentException)
        {
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (NotSupportedException)
        {
        }
        catch (SecurityException)
        {
        }

        return "Songs";
    }

    /// <inheritdoc />
    public void EnsureDirectoryExists(string path)
    {
        Directory.CreateDirectory(path);
    }

    [SupportedOSPlatform("windows")]
    private static string? FindWindowsInstallation()
    {
        try
        {
            return FindByDisplayName(
                       Registry.LocalMachine.OpenSubKey(
                           @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
                       "osu!")
                   ?? FindByDisplayName(
                       Registry.LocalMachine.OpenSubKey(
                           @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
                       "osu!");
        }
        catch (IOException)
        {
            return null;
        }
        catch (SecurityException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    [SupportedOSPlatform("windows")]
    private static string? FindByDisplayName(RegistryKey? parentKey, string name)
    {
        using (parentKey)
        {
            if (parentKey is null) return null;

            foreach (string subKeyName in parentKey.GetSubKeyNames())
            {
                using var subKey = parentKey.OpenSubKey(subKeyName);
                if (subKey?.GetValue("DisplayName")?.ToString() != name) continue;

                string? uninstallCommand = subKey.GetValue("UninstallString")?.ToString();
                return string.IsNullOrWhiteSpace(uninstallCommand)
                    ? null
                    : Path.GetDirectoryName(uninstallCommand.Trim('"'));
            }

            return null;
        }
    }

    private IEnumerable<string> GetNonWindowsCandidates()
    {
        string? home = getEnvironmentVariable("HOME");
        if (string.IsNullOrWhiteSpace(home))
            home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (string.IsNullOrWhiteSpace(home)) yield break;

        yield return Path.Combine(home, "osu!");
        yield return Path.Combine(home, ".local", "share", "osu!");
        yield return Path.Combine(home, ".config", "osu!");
        yield return Path.Combine(home, ".wine", "drive_c", "osu!");
        yield return Path.Combine(home, ".wine", "drive_c", "Program Files", "osu!");
        yield return Path.Combine(home, ".wine", "drive_c", "Program Files (x86)", "osu!");
        yield return Path.Combine(home, "Library", "Application Support", "osu!");
    }
}
