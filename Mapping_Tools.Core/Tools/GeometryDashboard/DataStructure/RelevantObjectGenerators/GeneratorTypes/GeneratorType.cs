namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorTypes;

/// <summary>Groups generators by the amount of geometry they derive.</summary>
public enum GeneratorType
{
    /// <summary>Directly derived geometry.</summary>
    Basic,

    /// <summary>Geometry derived from other generated geometry.</summary>
    Intermediate,

    /// <summary>Advanced multi-step or configurable geometry.</summary>
    Advanced,
}
