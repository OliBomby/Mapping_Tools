using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Mapping_Tools.Annotations;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation {
    /// <summary>
    /// Marks this method as the (potentially asynchronous) generator for relevant objects.
    /// </summary>
    [MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class RelevantObjectGeneratorAttribute : Attribute {
    }
}