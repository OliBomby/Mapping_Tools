using Avalonia;
using Avalonia.Styling;
using Mapping_Tools.ApplicationServices.Settings;
using Material.Styles.Themes;
using Material.Styles.Themes.Base;

namespace Mapping_Tools.Desktop.Platform;

/// <summary>
/// Maps the frontend-neutral palette choice to Avalonia theme variants.
/// </summary>
public sealed class AvaloniaApplicationThemeService : IApplicationThemeService
{
    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">
    /// Avalonia application initialization has not completed.
    /// </exception>
    public void Apply(ApplicationTheme theme)
    {
        Application application = Application.Current
            ?? throw new InvalidOperationException(
                "The application theme cannot be changed before Avalonia initializes.");
        application.RequestedThemeVariant = theme == ApplicationTheme.Light
            ? ThemeVariant.Light
            : ThemeVariant.Dark;
        MaterialTheme materialTheme = application.Styles
            .OfType<MaterialTheme>()
            .Single();
        materialTheme.BaseTheme = theme == ApplicationTheme.Light
            ? BaseThemeMode.Light
            : BaseThemeMode.Dark;
    }
}
