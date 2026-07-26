using System;
using Mapping_Tools.Properties;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation {
    /// <summary>
    /// Marks this method as the (potentially asynchronous) generator for relevant objects.
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class RelevantObjectsGeneratorMethodAttribute : Attribute {
    }
}