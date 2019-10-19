using Mapping_Tools.Annotations;
using System;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation {
    /// <summary>
    /// Marks this method as the (potentially asynchronous) generator for relevant objects.
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class RelevantObjectsGeneratorMethodAttribute : Attribute {
    }
}