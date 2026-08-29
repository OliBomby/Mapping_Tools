using System.Globalization;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Settings.Models;

namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard;

/// <summary>
///     Reads and caches the osu! display settings used by the Geometry Dashboard
///     coordinate transform.
/// </summary>
internal sealed class WindowsGeometryDashboardOsuConfigProvider
{
    private readonly object gate = new();
    private readonly ITextFileStore files;
    private readonly ApplicationSettings settings;
    private DateTime lastWriteTimeUtc;
    private string? loadedPath;
    private WindowsGeometryDashboardOsuDisplaySettings? loadedSettings;
    private string? status;

    internal WindowsGeometryDashboardOsuConfigProvider(
        ApplicationSettings settings,
        ITextFileStore files)
    {
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.files = files ?? throw new ArgumentNullException(nameof(files));
    }

    internal string? Status
    {
        get
        {
            lock (gate) return status;
        }
    }

    internal WindowsGeometryDashboardOsuDisplaySettings Read()
    {
        lock (gate)
        {
            string path = settings.OsuConfigPath;
            if (string.IsNullOrWhiteSpace(path))
            {
                loadedPath = null;
                loadedSettings = null;
                lastWriteTimeUtc = default;
                status = "Specify your osu! user configuration file in Mapping Tools Preferences.";
                return WindowsGeometryDashboardOsuDisplaySettings.Defaults;
            }

            DateTime writeTime = GetLastWriteTimeUtc(path);
            if (loadedPath == path && loadedSettings is not null && writeTime == lastWriteTimeUtc)
                return loadedSettings;

            try
            {
                var values = ReadValues(path);
                bool fullscreen = GetBool(values, "Fullscreen", true);
                loadedSettings = new WindowsGeometryDashboardOsuDisplaySettings(
                    new Core.MathUtil.Vector2(
                        GetDouble(values, fullscreen ? "WidthFullscreen" : "Width", 1920),
                        GetDouble(values, fullscreen ? "HeightFullscreen" : "Height", 1080)),
                    fullscreen,
                    GetBool(values, "Letterboxing", true),
                    new Core.MathUtil.Vector2(
                        GetDouble(values, "LetterboxPositionX", 0.5),
                        GetDouble(values, "LetterboxPositionY", 0.5)));
                loadedPath = path;
                lastWriteTimeUtc = writeTime;
                status = null;
                return loadedSettings;
            }
            catch (Exception exception)
            {
                loadedPath = path;
                lastWriteTimeUtc = writeTime;
                status = "Could not read osu! configuration: " + exception.Message;
                return loadedSettings ?? WindowsGeometryDashboardOsuDisplaySettings.Defaults;
            }
        }
    }

    private Dictionary<string, string> ReadValues(string path)
    {
        return new Dictionary<string, string>(
            files.ReadAllLines(path)
                .Select(line => line.Split(['=', ':'], 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(
                    parts => parts[0].Trim(),
                    parts => parts[1].Trim(),
                    StringComparer.OrdinalIgnoreCase),
            StringComparer.OrdinalIgnoreCase);
    }

    private static DateTime GetLastWriteTimeUtc(string path)
    {
        try
        {
            return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
        }
        catch
        {
            return DateTime.MinValue;
        }
    }

    private static bool GetBool(Dictionary<string, string> values, string key, bool fallback)
    {
        return values.TryGetValue(key, out string? value)
            ? value == "1" || bool.TryParse(value, out bool result) && result
            : fallback;
    }

    private static double GetDouble(Dictionary<string, string> values, string key, double fallback)
    {
        return values.TryGetValue(key, out string? value)
               && double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result)
            ? result
            : fallback;
    }
}
