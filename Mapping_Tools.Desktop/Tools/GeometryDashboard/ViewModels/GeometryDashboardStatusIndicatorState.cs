namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;

/// <summary>Identifies the visual state of the Geometry Dashboard status indicator.</summary>
public enum GeometryDashboardStatusIndicatorState
{
    /// <summary>Indicates that the dashboard is waiting or stopped.</summary>
    Waiting,

    /// <summary>Indicates that the dashboard is running.</summary>
    Running,

    /// <summary>Indicates that the dashboard cannot run or encountered an error.</summary>
    Error,
}
