using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Desktop.Models;

namespace Mapping_Tools.Desktop.Composition;

internal static class DesktopSettingsDefaults
{
    internal static DesktopApplicationSettings Create(bool isWindows, bool isLinux = false)
    {
        DesktopApplicationSettings settings = new();
        if (isWindows) return settings;

        if (!isLinux) settings.BeatmapLiveStateReading = BeatmapLiveStateReadingMode.Disabled;
        settings.EditorReload = EditorReloadMode.Disabled;
        return settings;
    }
}
