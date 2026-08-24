using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorTypes;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.SnappingTools.DataStructure.RelevantObject;

/// <summary>Describes a generated object that can participate in geometric hit testing.</summary>
public interface IRelevantDrawable : IRelevantObject
{
    /// <summary>Gets the preference-group name used by the later overlay renderer.</summary>
    string PreferencesName { get; }

    /// <summary>Measures the distance from this geometry to an editor-space point.</summary>
    /// <param name="point">The editor-space point.</param>
    /// <returns>The shortest distance in editor pixels.</returns>
    double DistanceTo(Vector2 point);

    /// <summary>Finds the point on this geometry nearest to an editor-space point.</summary>
    /// <param name="point">The editor-space point.</param>
    /// <returns>The nearest point in editor coordinates.</returns>
    Vector2 NearestPoint(Vector2 point);

    /// <summary>Finds intersections with another compatible generated object.</summary>
    /// <param name="other">The other generated object.</param>
    /// <param name="intersections">The intersection points, including a value when the query fails.</param>
    /// <returns><see langword="true" /> when at least one intersection exists.</returns>
    bool Intersection(IRelevantObject other, out Vector2[] intersections);
}

