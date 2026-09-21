using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Application.Settings.Models;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>Converts editor reload modes to their user-facing labels.</summary>
public sealed class EditorReloadModeConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is EditorReloadMode mode
            ? mode switch
            {
                EditorReloadMode.Disabled => "Disabled",
                EditorReloadMode.SimulatedKeypress => "Simulated keypress",
                EditorReloadMode.Mtipc => "MTIPC",
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
