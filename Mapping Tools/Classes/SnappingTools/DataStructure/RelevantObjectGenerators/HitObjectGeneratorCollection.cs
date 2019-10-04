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

        public IEnumerable<RelevantObjectsGenerator> GetActiveGenerators() {
            return Generators.Where(o => o.IsActive);
        }

        /// <summary>
        /// Generates new objects for the next layer based on the new object in the previous layer.
        /// </summary>
        /// <param name="thisLayer"></param>
        /// <param name="nextLayer">The layer to generate new objects for</param>
        /// <param name="nextContext">Context of the next layer</param>
        /// <param name="hitObject">The new object of the previous layer</param>
        public void GenerateNewObjects(HitObjectLayer thisLayer, ObjectLayer nextLayer, RelevantHitObject hitObject) {
            // Only generate objects using the new object and the rest and redo all concurrent generators

            // Get the dependencies of all the active generators
            // Check the preferences for generating preferences
            // Get the necessary layers
            // Take into account the situation. The changes of the layer
            // Generate pairs with a set of layers and a dependency
            // Return the new pairs

            var activeGenerators = GetActiveGenerators();
            foreach (var generator in activeGenerators) {
                var method = generator.GetGeneratorMethod();
                var dependencies = generator.GetDependencies();
                var concurrent = generator.IsConcurrent;
                var needsHitObjects = generator.NeedsHitObjects();

                var parametersList = RelevantObjectPairGenerator.GetParametersList(dependencies, thisLayer.HitObjects, hitObject);
                
                foreach (var parameters in parametersList) {
                    nextLayer.Add(method.Invoke(generator, parameters));
                }
            }
        }
    }
}
