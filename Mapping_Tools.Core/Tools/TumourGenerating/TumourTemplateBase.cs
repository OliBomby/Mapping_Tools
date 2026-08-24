using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.Graph.Interpolation.Interpolators;
using Mapping_Tools.Core.MathUtil;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.TumourGenerating;

internal abstract class TumourTemplateBase : ITumourTemplate
{
    [JsonIgnore] public double Length { get; set; }
    [JsonIgnore] public double Width { get; set; }
    [JsonIgnore] public double Parameter { get; set; }
    public virtual bool NeedsParameter => false;
    public abstract Vector2 GetOffset(double t);
    public abstract double GetLength();
    public abstract double GetDefaultSpan();
    public abstract int GetDetailLevel();
    public abstract IEnumerable<double> GetCriticalPoints();
    public abstract List<Vector2>? GetReconstructionHint();
    public abstract PathType GetReconstructionHintPathType();
    public abstract Func<double, double>? GetDistanceRelation();
}

