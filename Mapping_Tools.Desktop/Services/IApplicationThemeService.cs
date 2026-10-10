using Mapping_Tools.Application.Settings.Models;

namespace Mapping_Tools.Desktop.Services;

/// <summary>
///     Applies a persisted palette choice to the live Avalonia resource tree.
/// </summary>
public interface IApplicationThemeService
{
    /// <summary>
    ///     Changes the active palette immediately for every open top-level.
    /// </summary>
    /// <param name="theme">The persisted light or dark palette choice.</param>
    void Apply(ApplicationTheme theme);
}
