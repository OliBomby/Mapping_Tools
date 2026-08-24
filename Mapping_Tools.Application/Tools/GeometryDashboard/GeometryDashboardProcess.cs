using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools;
using Mapping_Tools.Core.Tools.SnappingTools.Serialization;

namespace Mapping_Tools.Application.Tools.GeometryDashboard;

/// <summary>
///     Describes the stable osu! process selected by the Windows process adapter.
/// </summary>
/// <param name="ProcessId">The operating-system process identifier.</param>
/// <param name="MainWindow">The process's main top-level window identifier.</param>
/// <param name="MainWindowTitle">The title observed when the process was discovered.</param>
public sealed record GeometryDashboardProcess(
    long ProcessId,
    PlatformWindowId MainWindow,
    string MainWindowTitle);

