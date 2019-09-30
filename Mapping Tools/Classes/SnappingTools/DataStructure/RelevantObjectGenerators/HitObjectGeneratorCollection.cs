using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mapping_Tools.Classes.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators {
    public class HitObjectGeneratorCollection {
        public List<RelevantObjectsGenerator> Generators;

        /// <summary>
        /// Generates new objects for the next layer based on the new object in the previous layer.
        /// </summary>
        /// <param name="nextLayer">The layer to generate new objects for</param>
        /// <param name="nextContext">Context of the next layer</param>
        /// <param name="hitObject">The new object of the previous layer</param>
        public void GenerateNewObjects(ObjectLayer nextLayer, HitObjectCollection nextContext, RelevantHitObject hitObject) {
            // Only generate objects using the new object and the rest and redo all concurrent generators
            throw new NotImplementedException();
        }

        public Type[] GetDependencies() {
            Generators.Where(o => o.IsActive).Select(o => o.GetType()).SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic).Where(m => m.GetCustomAttribute<RelevantObjectGeneratorAttribute>() != null)).Select(o => o.GetGenericArguments()[0]);
        }
    }
}
