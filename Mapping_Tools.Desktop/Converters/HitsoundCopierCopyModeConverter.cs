using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>Converts Hitsound Copier copy modes to their existing desktop labels.</summary>
public sealed class HitsoundCopierCopyModeConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is HitsoundCopierCopyMode mode
            ? mode switch
            {
                HitsoundCopierCopyMode.OverwriteEverything => "Overwrite everything",
                HitsoundCopierCopyMode.OverwriteOnlyDefined => "Overwrite only defined",
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
