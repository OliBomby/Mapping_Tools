using Mapping_Tools.Application.Workspace;

namespace Mapping_Tools.Application.Settings;

/// <summary>
///     Identifies the persisted application palette selected in Preferences.
/// </summary>
public enum ApplicationTheme
{
    /// <summary>
    ///     Uses dark surfaces with light foreground content.
    /// </summary>
    Dark,

    /// <summary>
    ///     Uses light surfaces with dark foreground content.
    /// </summary>
    Light,
}
