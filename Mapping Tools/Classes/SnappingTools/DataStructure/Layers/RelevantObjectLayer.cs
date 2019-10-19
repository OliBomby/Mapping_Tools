using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.Layers {
    /// <summary>
    /// Container for a list of relevant objects
    /// </summary>
    public class RelevantObjectLayer {
        public RelevantObjectCollection.RelevantObjectCollection Objects { get; set; }
        public RelevantObjectsGeneratorCollection GeneratorCollection { get; set; }
        public LayerCollection ParentCollection { get; set; }

        public RelevantObjectLayer PreviousLayer;
        public RelevantObjectLayer NextLayer;

        public RelevantObjectLayer(LayerCollection parentCollection, RelevantObjectsGeneratorCollection generatorCollection) {
            ParentCollection = parentCollection;
            GeneratorCollection = generatorCollection;

            Objects = new RelevantObjectCollection.RelevantObjectCollection();
        }

        public void SortTimes() {
            Objects.SortTimes();
        }

        public virtual void Add(IEnumerable<IRelevantObject> relevantObjects) {
            // Check if this object or something similar exists anywhere in the context or in this layer
            foreach (var relevantObject in relevantObjects) {
                if (Objects.FindSimilar(relevantObject, ParentCollection.AcceptableDifference, out _)) {
                    continue;  // Continue so the relevant object doesn't get added
                }

                // Insert the new object
                Objects.SortedInsert(relevantObject);
            }

            NextLayer.GenerateNewObjects();
        }

        /// <summary>
        /// Generates relevant objects and adds them to this layer.
        /// </summary>
        public void GenerateNewObjects() {
            var activeGenerators = GeneratorCollection.GetActiveGenerators();
            foreach (var generator in activeGenerators) {
                var method = generator.GetGeneratorMethod();
                var dependencies = generator.GetDependencies();
                var concurrent = generator.IsConcurrent;
                var needsHitObjects = generator.NeedsHitObjects();

                var parametersList = RelevantObjectPairGenerator.GetParametersList(dependencies, PreviousLayer.Objects);
                
                foreach (var parameters in parametersList) {
                    // Generate new relevant objects
                    var newRelevantObjects = ((IEnumerable<IRelevantObject>)method.Invoke(generator, parameters)).ToArray();

                    // Cast parameters to relevant objects
                    var relevantParents = parameters.Cast<IRelevantObject>().ToList();

                    // Add parents to the new relevant objects
                    foreach (var relevantObject in newRelevantObjects) {
                        relevantObject.ParentObjects = relevantParents;
                    }

                    // Add the new relevant objects to the children of the parents
                    relevantParents.ForEach(o => o.ChildObjects.AddRange(newRelevantObjects));

                    // Add the new relevant objects to this layer
                    Add(newRelevantObjects);
                }
            }
        }

        public virtual void Remove(IEnumerable<IRelevantObject> relevantObjects) {
            foreach (var relevantObject in relevantObjects) {
                Remove(relevantObject);
            }
        }

        public virtual void Remove(IRelevantObject relevantObject) {
            // Remove relevant object from this layer
            Objects.RemoveRelevantObject(relevantObject);

            // Kill all children
            foreach (var relevantObjectChildObject in relevantObject.ChildObjects) {
                relevantObjectChildObject.Layer.Remove(relevantObjectChildObject);
            }
        }
    }
}
