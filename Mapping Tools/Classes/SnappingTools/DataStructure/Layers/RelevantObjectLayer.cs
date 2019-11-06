using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.Layers {
    /// <summary>
    /// Container for a list of relevant objects
    /// </summary>
    public class RelevantObjectLayer {
        public RelevantObjectCollection.RelevantObjectCollection Objects { get; set; }

        [CanBeNull]
        public RelevantObjectsGeneratorCollection GeneratorCollection { get; set; }

        public LayerCollection ParentCollection { get; set; }

        [CanBeNull] 
        public RelevantObjectLayer PreviousLayer { get; set; }

        [CanBeNull]
        public RelevantObjectLayer NextLayer { get; set; }

        public RelevantObjectLayer(LayerCollection parentCollection, RelevantObjectsGeneratorCollection generatorCollection) {
            ParentCollection = parentCollection;
            GeneratorCollection = generatorCollection;

            Objects = new RelevantObjectCollection.RelevantObjectCollection();
        }

        public void SortTimes() {
            Objects.SortTimes();
        }

        public void Add(IEnumerable<IRelevantObject> relevantObjects, bool propagate = true) {
            bool addedAny = false;  // Check any relevant objects get added at all

            // Check if this object or something similar exists anywhere in the context or in this layer
            foreach (var relevantObject in relevantObjects) {
                Add(relevantObject, false);
                addedAny = true;
            }

            // Propagate changes if stuff got added
            if (propagate && addedAny) {
                NextLayer?.GenerateNewObjects();
            }
        }

        public void Add(IRelevantObject relevantObject, bool propagate = true) {
            // Don't add if this layer is over the max
            if (Objects.GetCount() > ParentCollection.MaxObjects) {
                relevantObject.Dispose();
                return;
            }

            if (Objects.FindSimilar(relevantObject, ParentCollection.AcceptableDifference, out var similarObject)) {
                // Consume object
                similarObject.Consume(relevantObject);
                // Dispose this relevant object
                relevantObject.Dispose();
                return;  // return so the relevant object doesn't get added
            }

            var previousCollection = GetAllPreviousLayersCollection();
            if (previousCollection != null && previousCollection.FindSimilar(relevantObject, ParentCollection.AcceptableDifference, out _)) {
                // Don't consume because that causes inheritance issues
                // Dispose this relevant object
                relevantObject.Dispose();
                return;  // return so the relevant object doesn't get added
            }
              
            // Insert the new object
            Objects.SortedInsert(relevantObject);

            // Set layer variable in object
            relevantObject.Layer = this;

            // Propagate changes
            if (propagate) {
                NextLayer?.GenerateNewObjects();
            }
        }

        private RelevantObjectCollection.RelevantObjectCollection GetAllPreviousLayersCollection() {
            if (PreviousLayer == null) return null;

            var collection = PreviousLayer.GetAllPreviousLayersCollection();

            return collection == null ? 
                PreviousLayer.Objects : 
                RelevantObjectCollection.RelevantObjectCollection.Merge(collection, PreviousLayer.Objects);
        }

        /// <summary>
        /// Generates relevant objects and adds them to this layer.
        /// </summary>
        public void GenerateNewObjects(bool forcePropagate = false) {
            if (GeneratorCollection == null) return;

            // Remove all relevant objects generated from a sequential generator
            foreach (var objectLayerObject in Objects.Values) {
                for (var i = 0; i < objectLayerObject.Count; i++) {
                    var obj = objectLayerObject[i];
                    if (!(obj.Generator != null && obj.Generator.Settings.IsSequential)) continue;
                    obj.Dispose();
                    i--;
                }
            }
            
            var addedSomething = false;
            var activeGenerators = GeneratorCollection.GetActiveGenerators().ToArray();

            // Get the previous layers objects
            var deepObjects = activeGenerators.Any(o => o.Settings.IsDeep) ? GetAllPreviousLayersCollection() : null;

            var objectsToAdd = new List<IRelevantObject>();
            foreach (var generator in activeGenerators) {
                var methods = generator.GetGeneratorMethods();

                var objects = generator.Settings.IsDeep ? deepObjects : PreviousLayer?.Objects;

                foreach (var method in methods) {
                    var dependencies = RelevantObjectsGenerator.GetDependencies(method);

                    // Continue if there are dependencies but nothing to get the values from
                    if (dependencies.Length > 0 && PreviousLayer == null) {
                        continue;
                    }

                    //Console.WriteLine(generator.Name.ToUpper());
                    var parametersList =
                        RelevantObjectPairGenerator.GetParametersList(dependencies, objects,
                            generator.Settings.IsSequential);

                    foreach (var parameters in parametersList) {
                        // Generate the new relevant object(s)
                        var result = method.Invoke(generator, parameters);

                        // Cast parameters to relevant objects
                        var relevantParents = new HashSet<IRelevantObject>(parameters.Cast<IRelevantObject>());

                        switch (result) {
                            case IEnumerable<IRelevantObject> newRelevantObjectsEnumerable: {
                                // Enumerate to array
                                var newRelevantObjectsArray =
                                    newRelevantObjectsEnumerable as IRelevantObject[] ??
                                    newRelevantObjectsEnumerable.ToArray();

                                // Add the new relevant objects to the children of the parents
                                foreach (var relevantParent in relevantParents) {
                                    relevantParent.ChildObjects.UnionWith(newRelevantObjectsArray);
                                }

                                // Add parents and generator to the new relevant objects
                                foreach (var relevantObject in newRelevantObjectsArray) {
                                    relevantObject.Generator = generator;  // Generator has to be set before parents, otherwise temporal position will go wrong
                                    relevantObject.ParentObjects = relevantParents;
                                }

                                // Add the new relevant objects to this layer
                                objectsToAdd.AddRange(newRelevantObjectsArray);
                                addedSomething = true;
                                break;
                            }
                            case IRelevantObject newRelevantObject:
                                // Add the new relevant object to the children of the parents
                                foreach (var relevantParent in relevantParents) {
                                    relevantParent.ChildObjects.Add(newRelevantObject);
                                }

                                // Add parents and generator to the new relevant object
                                newRelevantObject.Generator = generator;  // Generator has to be set before parents, otherwise temporal position will go wrong
                                newRelevantObject.ParentObjects = relevantParents;

                                // Add the new relevant objects to this layer
                                objectsToAdd.Add(newRelevantObject);
                                addedSomething = true;
                                break;
                        }
                    }
                }
            }

            // Add objects to this layer
            Add(objectsToAdd, false);

            // Don't propagate if this layer is over the max
            if (Objects.GetCount() > ParentCollection.MaxObjects) {
                return;
            }

            // Propagate if anything was added to this layer
            if (addedSomething || forcePropagate) {
                NextLayer?.GenerateNewObjects(forcePropagate);
            }
        }

        public void Remove(IEnumerable<IRelevantObject> relevantObjects, bool propagate = true) {
            foreach (var relevantObject in relevantObjects) {
                Remove(relevantObject, propagate);
            }
        }

        public void Remove(IRelevantObject relevantObject, bool propagate = true) {
            // Remove relevant object from this layer
            Objects.RemoveRelevantObject(relevantObject);

            if (!propagate) return;
            // Return if there are no children
            if (relevantObject.ChildObjects == null) {
                return;
            }

            // Kill all children
            foreach (var relevantObjectChildObject in relevantObject.ChildObjects) {
                relevantObjectChildObject.Layer?.Remove(relevantObjectChildObject);
            }
        }

        /// <summary>
        /// Disposes all relevant objects in this layer
        /// </summary>
        public void Clear() {
            foreach (var relevantObject in Objects.Select(kvp => kvp.Value.ToArray()).SelectMany(toDispose => toDispose)) {
                relevantObject.Dispose();
            }
        }
    }
}
