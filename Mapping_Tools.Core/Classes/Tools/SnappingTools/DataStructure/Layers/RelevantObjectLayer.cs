using System.Reflection;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;
using RelevantObjectCollectionType = Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection.RelevantObjectCollection;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.Layers;

/// <summary>Contains one ordered layer of root or generated geometry objects.</summary>
public sealed class RelevantObjectLayer
{
    /// <summary>Creates an empty layer.</summary>
    /// <param name="parentCollection">The owning layer collection.</param>
    /// <param name="generatorCollection">The generator catalog, or <see langword="null" /> for the root layer.</param>
    public RelevantObjectLayer(LayerCollection parentCollection, RelevantObjectsGeneratorCollection? generatorCollection)
    {
        ParentCollection = parentCollection;
        GeneratorCollection = generatorCollection;
        Objects = new RelevantObjectCollectionType();
    }

    /// <summary>Gets the objects in this layer, grouped by concrete type.</summary>
    public RelevantObjectCollectionType Objects { get; }

    /// <summary>Gets the generators used to populate this layer.</summary>
    public RelevantObjectsGeneratorCollection? GeneratorCollection { get; }

    /// <summary>Gets the owning layer collection.</summary>
    public LayerCollection ParentCollection { get; }

    /// <summary>Gets or sets the immediately preceding layer.</summary>
    public RelevantObjectLayer? PreviousLayer { get; set; }

    /// <summary>Gets or sets the immediately following layer.</summary>
    public RelevantObjectLayer? NextLayer { get; set; }

    /// <summary>Sorts each concrete-type group by timestamp.</summary>
    public void SortTimes()
    {
        Objects.SortTimes();
    }

    /// <summary>Adds objects and optionally regenerates the next layer.</summary>
    /// <param name="relevantObjects">The objects to add.</param>
    /// <param name="propagate">Whether to regenerate descendants after adding.</param>
    public void Add(IEnumerable<IRelevantObject> relevantObjects, bool propagate = true)
    {
        // Check any relevant objects get added at all
        bool addedAny = false;
        foreach (var relevantObject in relevantObjects) addedAny |= AddCore(relevantObject);

        if (propagate && addedAny)
            // Propagate changes if stuff got added
            NextLayer?.GenerateNewObjects();
    }

    /// <summary>Adds one object after applying duplicate and inheritance rules.</summary>
    /// <param name="relevantObject">The object to add.</param>
    /// <param name="propagate">Whether to regenerate descendants after adding.</param>
    public void Add(IRelevantObject relevantObject, bool propagate = true)
    {
        if (!AddCore(relevantObject)) return;

        if (propagate && NextLayer is not null)
            // Propagate changes
            NextLayer.GenerateNewObjects();
    }

    private bool AddCore(IRelevantObject relevantObject)
    {
        // Don't add if this layer is over the max
        if (Objects.GetCount() >= ParentCollection.MaxObjects)
        {
            relevantObject.Dispose();
            return false;
        }

        // Check if this object or something similar exists anywhere in the context or in this layer
        var previousCollection = GetAllPreviousLayersCollection();
        if (Objects.FindSimilar(relevantObject, ParentCollection.AcceptableDifference, out var similarObject))
        {
            // Consume object
            similarObject!.Consume(relevantObject);
            // Dispose this relevant object
            relevantObject.Dispose();
            // Set DoNotDispose for the GenerateNewObjects method
            if (!similarObject.DoNotDispose
                && !similarObject.DefinitelyDispose
                && previousCollection is not null
                && previousCollection.FindSimilar(similarObject, ParentCollection.AcceptableDifference, out _))
                similarObject.DefinitelyDispose = true;
            else
                similarObject.DoNotDispose = true;

            return false;
        }

        if (previousCollection is not null && previousCollection.FindSimilar(relevantObject, ParentCollection.AcceptableDifference, out _))
        {
            // Don't consume because that causes inheritance issues
            // Dispose this relevant object
            relevantObject.Dispose();
            return false;
        }

        // Set DoNotDispose for the GenerateNewObjects method
        relevantObject.DoNotDispose = true;
        // Insert the new object
        Objects.SortedInsert(relevantObject);
        // Set layer variable in object
        relevantObject.Layer = this;

        return true;
    }

    private RelevantObjectCollectionType? GetAllPreviousLayersCollection()
    {
        if (PreviousLayer is null) return null;

        var collection = PreviousLayer.GetAllPreviousLayersCollection();
        return collection is null
            ? PreviousLayer.Objects
            : RelevantObjectCollectionType.Merge(collection, PreviousLayer.Objects);
    }

    /// <summary>Regenerates this layer from active generators and its previous layers.</summary>
    /// <param name="forcePropagate">Whether to regenerate descendants even without new objects.</param>
    public void GenerateNewObjects(bool forcePropagate = false)
    {
        if (GeneratorCollection is null) return;

        // Get all active generators for this layer
        var activeGenerators = GeneratorCollection.GetActiveGenerators().ToArray();
        // Get the previous layers objects if any generators are deep
        var deepObjects = activeGenerators.Any(o => o.Settings.IsDeep)
            ? GetAllPreviousLayersCollection()
            : null;
        // Keep track of count to avoid adding too many objects
        int addedCount = 0;

        // Set all DoNotDispose to false
        foreach (var relevantObject in Objects.Values.SelectMany(list => list))
        {
            relevantObject.DoNotDispose = false;
            relevantObject.Relevancy = 0;
        }

        // Loop through all active generators
        foreach (var generator in activeGenerators)
        {
            // Get the generator methods
            IEnumerable<MethodInfo> methods = generator.GetGeneratorMethods();
            // Get the required relevant object collection for this generator
            var objects = generator.Settings.IsDeep
                ? deepObjects?.GetSubset(generator.Settings.InputPredicate, generator)
                : PreviousLayer?.Objects.GetSubset(generator.Settings.InputPredicate, generator);

            // Loop through all generator methods in this generator
            foreach (var method in methods)
            {
                // Get the dependencies for this generator method
                var dependencies = RelevantObjectsGenerator.GetDependencies(method);
                // Continue if there are dependencies but nothing to get the values from
                if (dependencies.Length > 0 && PreviousLayer is null) continue;

                // Get all the combinations of relevant objects to use this generator method on
                var parametersList = RelevantObjectPairGenerator.GetParametersList(
                    dependencies, objects, generator.Settings.IsSequential);

                // Generate all the new relevant objects
                foreach (object[] parameterObjects in parametersList)
                {
                    // Generate the new relevant object(s)
                    // Cast parameters to relevant objects
                    var parameters = parameterObjects.Cast<IRelevantObject>().ToArray();
                    object? result = method.Invoke(generator, parameterObjects);
                    HashSet<IRelevantObject> relevantParents = new(parameters);

                    // Handle different return types
                    switch (result)
                    {
                        case IEnumerable<IRelevantObject> newRelevantObjectsEnumerable:
                            // Enumerate to array
                            var newRelevantObjectsArray = newRelevantObjectsEnumerable.ToArray();
                            // Add the new relevant objects to the children of the parents
                            foreach (var relevantParent in relevantParents) relevantParent.ChildObjects.UnionWith(newRelevantObjectsArray);

                            // Add parents and generator to the new relevant objects
                            foreach (var relevantObject in newRelevantObjectsArray)
                            {
                                relevantObject.Generator = generator; // Generator has to be set before parents, otherwise temporal position will go wrong
                                relevantObject.ParentObjects = relevantParents;
                                // Set the IsInheritable setting according to the generator settings
                                relevantObject.IsInheritable = generator.Settings.GeneratesInheritable;
                            }

                            // Add the new relevant objects to this layer
                            Add(newRelevantObjectsArray, false);
                            addedCount += newRelevantObjectsArray.Length;
                            break;

                        case IRelevantObject newRelevantObject:
                            // Add the new relevant object to the children of the parents
                            foreach (var relevantParent in relevantParents) relevantParent.ChildObjects.Add(newRelevantObject);

                            // Add parents and generator to the new relevant object
                            newRelevantObject.Generator = generator; // Generator has to be set before parents, otherwise temporal position will go wrong
                            newRelevantObject.ParentObjects = relevantParents;
                            // Set the IsInheritable setting according to the generator settings
                            newRelevantObject.IsInheritable = generator.Settings.GeneratesInheritable;
                            // Add the new relevant objects to this layer
                            Add(newRelevantObject, false);
                            addedCount++;
                            break;
                    }

                    if (Objects.GetCount() >= ParentCollection.MaxObjects) goto generationEnd;
                }
            }
        }

        // Dispose all relevant objects in this layer that were generated from a generator, but not generated now.
        generationEnd:
        foreach (var objectLayerObjects in Objects.Values)
            for (int i = 0; i < objectLayerObjects.Count; i++)
            {
                var relevantObject = objectLayerObjects[i];
                // Continue for relevant objects with no generator or DoNotDispose
                if (!relevantObject.DefinitelyDispose && (relevantObject.Generator is null || relevantObject.DoNotDispose) && relevantObject.Relevancy > 0)
                    continue;

                relevantObject.Dispose();
                i--;
            }

        // Don't propagate if this layer has more than the max number of relevant objects
        if (Objects.GetCount() > ParentCollection.MaxObjects) return;

        // Propagate if anything was added to this layer
        if (addedCount > 0 || forcePropagate) NextLayer?.GenerateNewObjects(forcePropagate);
    }

    /// <summary>Removes objects and optionally removes their descendants.</summary>
    /// <param name="relevantObjects">The objects to remove.</param>
    /// <param name="propagate">Whether removal should continue through descendants.</param>
    public void Remove(IEnumerable<IRelevantObject> relevantObjects, bool propagate = true)
    {
        foreach (var relevantObject in relevantObjects) Remove(relevantObject, propagate);
    }

    /// <summary>Removes one object and optionally removes its descendants.</summary>
    /// <param name="relevantObject">The object to remove.</param>
    /// <param name="propagate">Whether removal should continue through descendants.</param>
    public void Remove(IRelevantObject relevantObject, bool propagate = true)
    {
        // Remove relevant object from this layer
        Objects.RemoveRelevantObject(relevantObject);
        // Return if there are no children
        if (!propagate || relevantObject.ChildObjects is null) return;

        // Kill all children
        foreach (var child in relevantObject.ChildObjects) child.Layer?.Remove(child);
    }

    /// <summary>Disposes every object currently held by the layer.</summary>
    public void Clear()
    {
        // Dispose all relevant objects in this layer
        foreach (var relevantObject in Objects.SelectMany(kvp => kvp.Value.ToArray())) relevantObject.Dispose();
    }
}
