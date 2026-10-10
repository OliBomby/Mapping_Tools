namespace Mapping_Tools.Application.Tools.GeometryDashboard.Models;

/// <summary>Identifies a displayable Geometry Dashboard status without encoding UI text.</summary>
public enum GeometryDashboardStatusCategory
{
    /// <summary>The status category is not specified, usually by an older caller.</summary>
    Unspecified,

    /// <summary>The dashboard is stopped or waiting for an editor snapshot.</summary>
    Stopped,

    /// <summary>The dashboard is starting.</summary>
    Starting,

    /// <summary>The dashboard is waiting for the osu! editor process.</summary>
    WaitingForEditor,

    /// <summary>The dashboard is waiting for osu! to start.</summary>
    WaitingForOsu,

    /// <summary>The dashboard is running while osu! has focus.</summary>
    Running,

    /// <summary>The dashboard is running while osu! does not have focus.</summary>
    Unfocused,

    /// <summary>The dashboard cannot run because overlay configuration is unavailable.</summary>
    ConfigurationUnavailable,

    /// <summary>The dashboard cannot run because the platform is unsupported.</summary>
    UnsupportedPlatform,

    /// <summary>The dashboard cannot run because live beatmap-state reading is disabled.</summary>
    LiveStateReadingDisabled,

    /// <summary>The dashboard encountered an error and will retry.</summary>
    RetryingAfterError,
}
