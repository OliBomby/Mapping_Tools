using System.Globalization;
using Avalonia.Data.Converters;
using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Desktop.Converters;

/// <summary>
///     Converts the application theme to the checked state of the dark-theme toggle.
/// </summary>
public sealed class DarkThemeConverter : IValueConverter
{
    /// <inheritdoc />
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not ApplicationTheme theme
            || targetType != typeof(bool) && targetType != typeof(bool?))
            return ValueConverterHelper.TypeError(
                typeof(ApplicationTheme),
                targetType);

        return theme == ApplicationTheme.Dark;
    }

    /// <inheritdoc />
    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        if (value is not bool isDark
            || targetType != typeof(ApplicationTheme)
            && targetType != typeof(object))
            return ValueConverterHelper.TypeError(
                typeof(bool),
                targetType);

        return isDark
            ? ApplicationTheme.Dark
            : ApplicationTheme.Light;
    }
}
