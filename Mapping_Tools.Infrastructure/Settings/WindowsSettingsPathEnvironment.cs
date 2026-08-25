using System.Runtime.Versioning;
using Mapping_Tools.Application.Settings.Contracts;
using Microsoft.Win32;

namespace Mapping_Tools.Infrastructure.Settings;

/// <summary>
///     Reads Windows registry and filesystem state needed to reproduce the legacy
///     osu! path-discovery behavior.
/// </summary>
public sealed class WindowsSettingsPathEnvironment : ISettingsPathEnvironment
{
    /// <summary>
    ///     <inheritdoc />
    public string UserName => Environment.UserName;

    /// <summary>
    ///     <inheritdoc />
    ///     <remarks>
    ///         Searches both 64-bit and WOW6432 uninstall registry locations by display name.
    ///         Non-Windows platforms return <see langword="null" />.
    ///     </remarks>
    public string? FindOsuInstallation()
    {
        if (!OperatingSystem.IsWindows()) return null;

        return FindByDisplayName(
                   Registry.LocalMachine.OpenSubKey(
                       @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall"),
                   "osu!")
               ?? FindByDisplayName(
                   Registry.LocalMachine.OpenSubKey(
                       @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"),
                   "osu!");
    }

    /// <summary>
    ///     <inheritdoc />
    public string GetBeatmapDirectory(string configPath)
    {
        try
        {
            foreach (string line in File.ReadLines(configPath))
            {
                string[] parts = line.Split('=', 2);
                if (parts.Length == 2
                    && parts[0].Trim().Equals(
                        "BeatmapDirectory",
                        StringComparison.Ordinal))
                    return parts[1].Trim();
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return "Songs";
    }

    /// <summary>
    ///     <inheritdoc />
    public void EnsureDirectoryExists(string path)
    {
        Directory.CreateDirectory(path);
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
}
