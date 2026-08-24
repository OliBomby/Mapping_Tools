using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.MathUtil;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.TumourGenerating;

/// <summary>Marks templates that require initialization after their dimensions are set.</summary>
public interface IRequireInit
{
    /// <summary>Recomputes any cached shape values from the current template properties.</summary>
    void Init();
}

