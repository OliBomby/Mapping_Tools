namespace Mapping_Tools.Application.Tools.GeometryDashboard.Models;

/// <summary>Describes the latest externally observable dashboard service state.</summary>
/// <param name="Status">The current connection, validation, or empty-state message.</param>
/// <param name="IsConnected">Whether a live editor snapshot is currently displayed.</param>
/// <param name="DrawableCount">The number of generated drawable objects.</param>
/// <param name="SelectedCount">The number of selected virtual objects.</param>
public sealed record GeometryDashboardServiceState(
    string Status,
    bool IsConnected,
    int DrawableCount,
    int SelectedCount);
