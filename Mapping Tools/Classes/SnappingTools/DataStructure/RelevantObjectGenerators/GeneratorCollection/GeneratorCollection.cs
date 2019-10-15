using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection {
    public abstract class GeneratorCollection {
        public List<RelevantObjectsGenerator> Generators;

        public IEnumerable<RelevantObjectsGenerator> GetActiveGenerators() {
            return Generators.Where(o => o.IsActive);
        }

        /// <summary>
        /// Adds new objects to the next layer for on the new object in the previous layer.
        /// </summary>
        /// <param name="thisLayer">The layer which the new relevant object was added to</param>
        /// <param name="nextLayer">The layer to generate new objects for</param>
        /// <param name="newRelevantObject">The relevant object that just got added</param>
        public virtual void GenerateNewObjects(ObjectLayer thisLayer, ObjectLayer nextLayer,
            IRelevantObject newRelevantObject) {
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

                var parametersList = RelevantObjectPairGenerator.GetParametersList(dependencies, thisLayer.Objects, newRelevantObject);
                
                foreach (var parameters in parametersList) {
                    // Generate new objects and cast parents
                    var ro = ((IEnumerable<IRelevantObject>)method.Invoke(generator, parameters)).ToArray();
                    var relevantParents = parameters.Cast<IRelevantObject>().ToList();
                    // Add parents and children
                    foreach (var relevantObject in ro) {
                        relevantObject.ParentObjects = relevantParents;
                    }
                    relevantParents.ForEach(o => o.ChildObjects.AddRange(ro));
                    // Add to next layer
                    nextLayer.Add(ro);
                }
            }
        }

        public virtual void GenerateNewObjects(ObjectLayer thisLayer, ObjectLayer nextLayer,
            IRelevantObject[] newRelevantObjects) {
            throw new NotImplementedException();
        }
    }
}
