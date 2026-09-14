using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;

namespace Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;

/// <summary>Contains the accepted Desktop-owned dashboard preferences.</summary>
/// <param name="Preferences">The accepted engine preferences.</param>
/// <param name="KeepRunning">Whether the Desktop should keep the service running when hidden.</param>
public sealed record GeometryDashboardPreferencesDialogResult(
    GeometryDashboardPreferences Preferences,
    bool KeepRunning);
