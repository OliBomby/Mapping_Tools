using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.MathUtil;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.TumourGenerating;

/// <summary>Identifies the geometric template used for one tumour.</summary>
public enum TumourTemplate
{
    /// <summary>A triangular protrusion.</summary>
    Triangle,

    /// <summary>A square-topped protrusion.</summary>
    Square,

    /// <summary>A circular-arc protrusion.</summary>
    Circle,

    /// <summary>A parabolic protrusion.</summary>
    Parabola,
}

