using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Mapping_Tools.Components.Domain;

/// <summary>
/// Converts the <see cref="Enum"/> type to visible if the enum matches the parameter and collapsed otherwise.
/// </summary>
public class EnumToVisibilityConverter : IValueConverter {
    /// <inheritdoc />
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
        return value != null && value.Equals(parameter) ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <inheritdoc />
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
        return value != null && value.Equals(Visibility.Visible) ? parameter : Binding.DoNothing;
    }
}