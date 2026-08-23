using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Core.Tools.TimingCopier;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
///     Converts Timing Copier's framework-independent resnap modes to the labels
///     shown by the desktop mode picker.
/// </summary>
public sealed class TimingCopierResnapModeConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is TimingCopierResnapMode mode
            ? mode switch
            {
                TimingCopierResnapMode.PreserveBeatSpacing =>
                    "Number of beats between objects stays the same",
                TimingCopierResnapMode.Resnap => "Just resnap",
                TimingCopierResnapMode.KeepObjectsFixed => "Don't move objects",
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
