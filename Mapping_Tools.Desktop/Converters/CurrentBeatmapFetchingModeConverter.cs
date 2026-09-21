using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Application.Settings.Models;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>Converts current-beatmap fetching modes to their user-facing labels.</summary>
public sealed class CurrentBeatmapFetchingModeConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is CurrentBeatmapFetchingMode mode
            ? mode switch
            {
                CurrentBeatmapFetchingMode.Disabled => "Disabled",
                CurrentBeatmapFetchingMode.MemoryRead => "Memory read",
                CurrentBeatmapFetchingMode.Mtipc => "MTIPC",
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
