using System;
using JetBrains.Annotations;

namespace Mapping_Tools_Core.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation {
    /// <summary>
    /// Marks this method as the (potentially asynchronous) generator for relevant objects.
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class RelevantObjectsGeneratorMethodAttribute : Attribute {
    }
}