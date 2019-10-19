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

        public void Add(IEnumerable<IRelevantObject> relevantObjects) {
            // Check if this object or something similar exists anywhere in the context or in this layer
            foreach (var relevantObject in relevantObjects) {
                Add(relevantObject, false);
            }

            // Propagate changes
            NextLayer.GenerateNewObjects();
        }

        public void Add(IRelevantObject relevantObject, bool propagate = true) {
            if (Objects.FindSimilar(relevantObject, ParentCollection.AcceptableDifference, out _)) {
                return;  // return so the relevant object doesn't get added
            }

            // Insert the new object
            Objects.SortedInsert(relevantObject);

            // Propagate changes
            if (propagate) {
                NextLayer.GenerateNewObjects();
            }
        }

        /// <summary>
        /// Generates relevant objects and adds them to this layer.
        /// </summary>
        public void GenerateNewObjects() {
            var activeGenerators = GeneratorCollection.GetActiveGenerators();
            foreach (var generator in activeGenerators) {
                var concurrent = generator.IsConcurrent;

                var methods = generator.GetGeneratorMethods();

                foreach (var method in methods) {
                    var dependencies = RelevantObjectsGenerator.GetDependencies(method);
                    var needsHitObjects = RelevantObjectsGenerator.NeedsHitObjects(method);

                    var parametersList = RelevantObjectPairGenerator.GetParametersList(dependencies, PreviousLayer.Objects);
                    
                    foreach (var parameters in parametersList) {
                        // Generate the new relevant object(s)
                        var result = method.Invoke(generator, parameters);

                        // Cast parameters to relevant objects
                        var relevantParents = parameters.Cast<IRelevantObject>().ToList();

                        switch (result) {
                            case IEnumerable<IRelevantObject> newRelevantObjectsEnumerable: {
                                // Enumerate to array
                                var newRelevantObjectsArray = newRelevantObjectsEnumerable as IRelevantObject[] ?? newRelevantObjectsEnumerable.ToArray();

                                // Add parents to the new relevant objects
                                foreach (var relevantObject in newRelevantObjectsArray) {
                                    relevantObject.ParentObjects = relevantParents;
                                }

                                // Add the new relevant objects to the children of the parents
                                relevantParents.ForEach(o => o.ChildObjects.AddRange(newRelevantObjectsArray));

                                // Add the new relevant objects to this layer
                                Add(newRelevantObjectsArray);
                                break;
                            }
                            case IRelevantObject newRelevantObject:
                                // Add parents to the new relevant object
                                newRelevantObject.ParentObjects = relevantParents;

                                // Add the new relevant object to the children of the parents
                                relevantParents.ForEach(o => o.ChildObjects.Add(newRelevantObject));

                                // Add the new relevant objects to this layer
                                Add(newRelevantObject);
                                break;
                        }
                    }
                }
            }
        }

        public void Remove(IEnumerable<IRelevantObject> relevantObjects) {
            foreach (var relevantObject in relevantObjects) {
                Remove(relevantObject);
            }
        }

        public void Remove(IRelevantObject relevantObject) {
            // Remove relevant object from this layer
            Objects.RemoveRelevantObject(relevantObject);

            // Kill all children
            foreach (var relevantObjectChildObject in relevantObject.ChildObjects) {
                relevantObjectChildObject.Layer.Remove(relevantObjectChildObject);
            }
        }
    }
}
