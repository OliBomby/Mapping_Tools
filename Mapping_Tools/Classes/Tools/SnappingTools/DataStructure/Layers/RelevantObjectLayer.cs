using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Annotations;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.Layers {
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
            
            var previousCollection = GetAllPreviousLayersCollection();
            if (Objects.FindSimilar(relevantObject, ParentCollection.AcceptableDifference, out var similarObject)) {
                // Consume object
                similarObject.Consume(relevantObject);
                // Dispose this relevant object
                relevantObject.Dispose();
                // Set DoNotDispose for the GenerateNewObjects method
                if (!similarObject.DoNotDispose && !similarObject.DefinitelyDispose && previousCollection.FindSimilar(similarObject, ParentCollection.AcceptableDifference, out _)) {
                    similarObject.DefinitelyDispose = true;
                } else {
                    similarObject.DoNotDispose = true;
                }
                return;  // return so the relevant object doesn't get added
            }

            if (previousCollection != null && previousCollection.FindSimilar(relevantObject, ParentCollection.AcceptableDifference, out _)) {
                // Don't consume because that causes inheritance issues
                // Dispose this relevant object
                relevantObject.Dispose();
                return;  // return so the relevant object doesn't get added
            }
            
            // Set DoNotDispose for the GenerateNewObjects method
            relevantObject.DoNotDispose = true;
              
            // Insert the new object
            Objects.SortedInsert(relevantObject);

            // Set layer variable in object
            relevantObject.Layer = this;

            // Propagate changes
            if (!propagate || NextLayer == null) return;
            NextLayer.GenerateNewObjects();
        }

        private RelevantObjectCollection.RelevantObjectCollection GetAllPreviousLayersCollection() {
            if (PreviousLayer == null) return null;

            var collection = PreviousLayer.GetAllPreviousLayersCollection();

            return collection == null ? 
                PreviousLayer.Objects : 
                RelevantObjectCollection.RelevantObjectCollection.Merge(collection, PreviousLayer.Objects);
        }

        /// <summary>
        /// Regenerates the relevant objects of this layer using the active generators. Any relevant objects that already exist do not get replaced and any relevant objects that should not exist get removed.
        /// </summary>
        public void GenerateNewObjects(bool forcePropagate = false) {
            if (GeneratorCollection == null) return;
            
            // Get all active generators for this layer
            var activeGenerators = GeneratorCollection.GetActiveGenerators().ToArray();

            // Get the previous layers objects if any generators are deep
            var deepObjects = activeGenerators.Any(o => o.Settings.IsDeep) ? GetAllPreviousLayersCollection() : null;

            // Initialize list for objects to add later
            var objectsToAdd = new List<IRelevantObject>();

            // Loop through all active generators
            foreach (var generator in activeGenerators) {
                // Get the generator methods
                var methods = generator.GetGeneratorMethods();

                // Get the required relevant object collection for this generator
                var objects = generator.Settings.IsDeep ? 
                    deepObjects.GetSubset(generator.Settings.InputPredicate, generator) : 
                    PreviousLayer?.Objects?.GetSubset(generator.Settings.InputPredicate, generator);

                // Loop through all generator methods in this generator
                foreach (var method in methods) {
                    // Get the dependencies for this generator method
                    var dependencies = RelevantObjectsGenerator.GetDependencies(method);

                    // Continue if there are dependencies but nothing to get the values from
                    if (dependencies.Length > 0 && PreviousLayer == null) {
                        continue;
                    }

                    // Get all the combinations of relevant objects to use this generator method on
                    var parametersList =
                        RelevantObjectPairGenerator.GetParametersList(dependencies, objects,
                            generator.Settings.IsSequential);

                    // Generate all the new relevant objects
                    foreach (var parameters in parametersList) {
                        // Generate the new relevant object(s)
                        var result = method.Invoke(generator, parameters);

                        // Cast parameters to relevant objects
                        var relevantParents = new HashSet<IRelevantObject>(parameters.Cast<IRelevantObject>());

                        // Handle different return types
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

                                    // Set the IsInheritable setting according to the generator settings
                                    relevantObject.IsInheritable = generator.Settings.GeneratesInheritable;
                                }

                                // Add the new relevant objects to this layer
                                objectsToAdd.AddRange(newRelevantObjectsArray);
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
                                
                                // Set the IsInheritable setting according to the generator settings
                                newRelevantObject.IsInheritable = generator.Settings.GeneratesInheritable;

                                // Add the new relevant objects to this layer
                                objectsToAdd.Add(newRelevantObject);
                                break;
                        }
                    }
                }
            }

            // Avoid adding too many objects
            var newCount = objectsToAdd.Count + Objects.GetCount();
            var overshot = newCount - ParentCollection.MaxObjects;
            if (overshot > 0) {
                return;
                //objectsToAdd.RemoveRange(objectsToAdd.Count - overshot, overshot);
            }

            // Set all DoNotDispose to false
            foreach (var relevantObject in Objects.Values.SelectMany(list => list)) {
                relevantObject.DoNotDispose = false;
            }

            // Add objects to this layer
            // This sets DoNotDispose for all the relevant objects that already exist in this layer and are consuming the objects to add.
            Add(objectsToAdd, false);

            // Dispose all relevant objects in this layer that were generated from a generator, but not generated now.
            foreach (var objectLayerObject in Objects.Values) {
                for (var i = 0; i < objectLayerObject.Count; i++) {
                    var obj = objectLayerObject[i];
                    // Continue for relevant objects with no generator or DoNotDispose
                    if (!obj.DefinitelyDispose && (obj.Generator == null || obj.DoNotDispose)) continue;
                    obj.Dispose();
                    i--;
                }
            }

            // Don't propagate if this layer has more than the max number of relevant objects
            if (Objects.GetCount() > ParentCollection.MaxObjects) {
                return;
            }

            // Propagate if anything was added to this layer
            if (objectsToAdd.Count > 0 || forcePropagate) {
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
