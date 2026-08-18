using System;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;

/// <summary>Marks a public method as a generator operation discovered by reflection.</summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RelevantObjectsGeneratorMethodAttribute : Attribute
{
}
