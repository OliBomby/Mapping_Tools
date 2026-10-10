using Avalonia.Styling;
using Mapping_Tools.Application.Settings.Models;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;

namespace Mapping_Tools.Desktop.Services;

/// <summary>
///     Maps the frontend-neutral palette choice to Avalonia theme variants.
/// </summary>
public sealed class ApplicationThemeService : IApplicationThemeService
{
    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">
    ///     Avalonia application initialization has not completed.
    /// </exception>
    public void Apply(ApplicationTheme theme)
    {
        var application = Avalonia.Application.Current
                          ?? throw new InvalidOperationException(
                              "The application theme cannot be changed before Avalonia initializes.");
        application.RequestedThemeVariant = theme == ApplicationTheme.Light
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
        var materialTheme = application.Styles
            .OfType<MaterialTheme>()
            .Single();
        materialTheme.BaseTheme = theme == ApplicationTheme.Light
            ? BaseThemeMode.Light
            : BaseThemeMode.Dark;
    }
}
