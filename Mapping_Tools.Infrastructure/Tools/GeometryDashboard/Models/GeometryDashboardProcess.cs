namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;

/// <summary>Describes the stable osu! process selected by the Windows adapter.</summary>
/// <param name="ProcessId">The operating-system process identifier.</param>
/// <param name="MainWindow">The process's main top-level window identifier.</param>
/// <param name="MainWindowTitle">The title observed when the process was discovered.</param>
public sealed record GeometryDashboardProcess(
    long ProcessId,
    PlatformWindowId MainWindow,
    string MainWindowTitle);
