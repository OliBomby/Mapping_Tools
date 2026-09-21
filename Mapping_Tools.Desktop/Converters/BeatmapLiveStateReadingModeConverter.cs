using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Application.Settings.Models;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>Converts beatmap live-state reading modes to their user-facing labels.</summary>
public sealed class BeatmapLiveStateReadingModeConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is BeatmapLiveStateReadingMode mode
            ? mode switch
            {
                BeatmapLiveStateReadingMode.Disabled => "Disabled",
                BeatmapLiveStateReadingMode.EditorReader => "Memory read",
                BeatmapLiveStateReadingMode.Mtipc => "MTIPC",
                _ => string.Empty,
            }
            : string.Empty;
    }

    /// <inheritdoc />
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
