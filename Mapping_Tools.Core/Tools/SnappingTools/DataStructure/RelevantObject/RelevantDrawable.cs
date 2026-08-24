using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;

/// <summary>Base class for point, line, and circle objects used by the geometry engine.</summary>
public abstract class RelevantDrawable : RelevantObject, IRelevantDrawable
{
    /// <inheritdoc />
    public abstract string PreferencesName { get; }

    /// <inheritdoc />
    public abstract double DistanceTo(Vector2 point);

    /// <inheritdoc />
    public abstract Vector2 NearestPoint(Vector2 point);

    /// <inheritdoc />
    public abstract bool Intersection(IRelevantObject other, out Vector2[] intersections);
}

