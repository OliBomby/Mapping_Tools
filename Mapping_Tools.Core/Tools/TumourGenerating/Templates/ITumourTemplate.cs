using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Core.Tools.TumourGenerating.Templates;

/// <summary>Provides the shape contract consumed by the tumour path algorithm.</summary>
public interface ITumourTemplate
{
    /// <summary>Gets or sets the longitudinal length in osu! pixels.</summary>
    double Length { get; set; }

    /// <summary>Gets or sets the signed protrusion width in osu! pixels.</summary>
    double Width { get; set; }

    /// <summary>Gets or sets the optional shape parameter.</summary>
    double Parameter { get; set; }

    /// <summary>Gets whether <see cref="Parameter" /> changes this template.</summary>
    bool NeedsParameter { get; }

    /// <summary>Gets the offset at normalized template progress.</summary>
    /// <param name="t">The template progress from zero to one.</param>
    /// <returns>The offset from the underlying slider path.</returns>
    Vector2 GetOffset(double t);

    /// <summary>Gets the approximated curve length of the configured template.</summary>
    double GetLength();

    /// <summary>Gets the default endpoint span for this template.</summary>
    double GetDefaultSpan();

    /// <summary>Gets the minimum detail level used for path approximation.</summary>
    int GetDetailLevel();

    /// <summary>Gets normalized points that must be retained as path points.</summary>
    IEnumerable<double> GetCriticalPoints();

    /// <summary>Gets reconstruction anchors in the template's local coordinates.</summary>
    List<Vector2>? GetReconstructionHint();

    /// <summary>Gets the path type for the reconstruction hint.</summary>
    PathType GetReconstructionHintPathType();

    /// <summary>Gets the cumulative-distance relation for a reconstruction hint.</summary>
    Func<double, double>? GetDistanceRelation();
}

