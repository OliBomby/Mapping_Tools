namespace Mapping_Tools.Application.Tools.GeometryDashboard.Models;

/// <summary>Describes how a dashboard bulk operation changes its targeted objects.</summary>
public enum GeometryDashboardTargetingMode
{
    /// <summary>Toggles each targeted object's current state.</summary>
    Toggle,

    /// <summary>Enables the targeted objects.</summary>
    Enable,

    /// <summary>Disables the targeted objects.</summary>
    Disable,
}
