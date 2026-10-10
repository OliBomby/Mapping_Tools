using Mapping_Tools.Core.Annotations;

namespace Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.Allocation;

/// <summary>Marks a public method as a generator operation discovered by reflection.</summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public sealed class RelevantObjectsGeneratorMethodAttribute : Attribute
{
}
