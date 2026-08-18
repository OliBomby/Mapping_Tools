using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;
using RelevantObjectCollectionType = Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection.RelevantObjectCollection;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.Layers;

/// <summary>Contains one ordered layer of root or generated geometry objects.</summary>
public sealed class RelevantObjectLayer
{
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

    /// <summary>Creates an empty layer.</summary>
    /// <param name="parentCollection">The owning layer collection.</param>
    /// <param name="generatorCollection">The generator catalog, or <see langword="null"/> for the root layer.</param>
    public RelevantObjectLayer(LayerCollection parentCollection, RelevantObjectsGeneratorCollection? generatorCollection)
    {
        ParentCollection = parentCollection;
        GeneratorCollection = generatorCollection;
        Objects = new RelevantObjectCollectionType();
    }

    /// <summary>Sorts each concrete-type group by timestamp.</summary>
    public void SortTimes() => Objects.SortTimes();

    /// <summary>Adds objects and optionally regenerates the next layer.</summary>
    /// <param name="relevantObjects">The objects to add.</param>
    /// <param name="propagate">Whether to regenerate descendants after adding.</param>
    public void Add(IEnumerable<IRelevantObject> relevantObjects, bool propagate = true)
    {
        bool addedAny = false;
        foreach (IRelevantObject relevantObject in relevantObjects)
        {
            addedAny |= AddCore(relevantObject);
        }

        if (propagate && addedAny)
        {
            NextLayer?.GenerateNewObjects();
        }
    }

    /// <summary>Adds one object after applying duplicate and inheritance rules.</summary>
    /// <param name="relevantObject">The object to add.</param>
    /// <param name="propagate">Whether to regenerate descendants after adding.</param>
    public void Add(IRelevantObject relevantObject, bool propagate = true)
    {
        if (!AddCore(relevantObject))
        {
            return;
        }

        if (propagate && NextLayer is not null)
        {
            NextLayer.GenerateNewObjects();
        }
    }

    private bool AddCore(IRelevantObject relevantObject)
    {
        if (Objects.GetCount() >= ParentCollection.MaxObjects)
        {
            relevantObject.Dispose();
            return false;
        }

        RelevantObjectCollectionType? previousCollection = GetAllPreviousLayersCollection();
        if (Objects.FindSimilar(relevantObject, ParentCollection.AcceptableDifference, out IRelevantObject? similarObject))
        {
            similarObject!.Consume(relevantObject);
            relevantObject.Dispose();
            if (!similarObject.DoNotDispose && !similarObject.DefinitelyDispose &&
                previousCollection is not null &&
                previousCollection.FindSimilar(similarObject, ParentCollection.AcceptableDifference, out _))
            {
                similarObject.DefinitelyDispose = true;
            }
            else
            {
                similarObject.DoNotDispose = true;
            }

            return false;
        }

        if (previousCollection is not null &&
            previousCollection.FindSimilar(relevantObject, ParentCollection.AcceptableDifference, out _))
        {
            relevantObject.Dispose();
            return false;
        }

        relevantObject.DoNotDispose = true;
        Objects.SortedInsert(relevantObject);
        relevantObject.Layer = this;

        return true;
    }

    private RelevantObjectCollectionType? GetAllPreviousLayersCollection()
    {
        if (PreviousLayer is null)
        {
            return null;
        }

        RelevantObjectCollectionType? collection = PreviousLayer.GetAllPreviousLayersCollection();
        return collection is null
            ? PreviousLayer.Objects
            : RelevantObjectCollectionType.Merge(collection, PreviousLayer.Objects);
    }

    /// <summary>Regenerates this layer from active generators and its previous layers.</summary>
    /// <param name="forcePropagate">Whether to regenerate descendants even without new objects.</param>
    public void GenerateNewObjects(bool forcePropagate = false)
    {
        if (GeneratorCollection is null)
        {
            return;
        }

        RelevantObjectsGenerator[] activeGenerators = GeneratorCollection.GetActiveGenerators().ToArray();
        RelevantObjectCollectionType? deepObjects = activeGenerators.Any(o => o.Settings.IsDeep)
            ? GetAllPreviousLayersCollection()
            : null;
        int addedCount = 0;

        foreach (IRelevantObject relevantObject in Objects.Values.SelectMany(list => list))
        {
            relevantObject.DoNotDispose = false;
            relevantObject.Relevancy = 0;
        }

        foreach (RelevantObjectsGenerator generator in activeGenerators)
        {
            IEnumerable<System.Reflection.MethodInfo> methods = generator.GetGeneratorMethods();
            RelevantObjectCollectionType? objects = generator.Settings.IsDeep
                ? deepObjects?.GetSubset(generator.Settings.InputPredicate, generator)
                : PreviousLayer?.Objects.GetSubset(generator.Settings.InputPredicate, generator);

            foreach (System.Reflection.MethodInfo method in methods)
            {
                Type[] dependencies = RelevantObjectsGenerator.GetDependencies(method);
                if (dependencies.Length > 0 && PreviousLayer is null)
                {
                    continue;
                }

                IEnumerable<object[]> parametersList = RelevantObjectPairGenerator.GetParametersList(
                    dependencies, objects, generator.Settings.IsSequential);

                foreach (object[] parameterObjects in parametersList)
                {
                    IRelevantObject[] parameters = parameterObjects.Cast<IRelevantObject>().ToArray();
                    object? result = method.Invoke(generator, parameterObjects);
                    HashSet<IRelevantObject> relevantParents = new(parameters);

                    switch (result)
                    {
                        case IEnumerable<IRelevantObject> newRelevantObjectsEnumerable:
                            IRelevantObject[] newRelevantObjectsArray = newRelevantObjectsEnumerable.ToArray();
                            foreach (IRelevantObject relevantParent in relevantParents)
                            {
                                relevantParent.ChildObjects.UnionWith(newRelevantObjectsArray);
                            }

                            foreach (IRelevantObject relevantObject in newRelevantObjectsArray)
                            {
                                relevantObject.Generator = generator;
                                relevantObject.ParentObjects = relevantParents;
                                relevantObject.IsInheritable = generator.Settings.GeneratesInheritable;
                            }

                            Add(newRelevantObjectsArray, false);
                            addedCount += newRelevantObjectsArray.Length;
                            break;

                        case IRelevantObject newRelevantObject:
                            foreach (IRelevantObject relevantParent in relevantParents)
                            {
                                relevantParent.ChildObjects.Add(newRelevantObject);
                            }

                            newRelevantObject.Generator = generator;
                            newRelevantObject.ParentObjects = relevantParents;
                            newRelevantObject.IsInheritable = generator.Settings.GeneratesInheritable;
                            Add(newRelevantObject, false);
                            addedCount++;
                            break;
                    }

                    if (Objects.GetCount() >= ParentCollection.MaxObjects)
                    {
                        goto generationEnd;
                    }
                }
            }
        }

        generationEnd:
        foreach (List<IRelevantObject> objectLayerObjects in Objects.Values)
        {
            for (int i = 0; i < objectLayerObjects.Count; i++)
            {
                IRelevantObject relevantObject = objectLayerObjects[i];
                if (!relevantObject.DefinitelyDispose &&
                    (relevantObject.Generator is null || relevantObject.DoNotDispose) &&
                    relevantObject.Relevancy > 0)
                {
                    continue;
                }

                relevantObject.Dispose();
                i--;
            }
        }

        if (Objects.GetCount() > ParentCollection.MaxObjects)
        {
            return;
        }

        if (addedCount > 0 || forcePropagate)
        {
            NextLayer?.GenerateNewObjects(forcePropagate);
        }
    }

    /// <summary>Removes objects and optionally removes their descendants.</summary>
    /// <param name="relevantObjects">The objects to remove.</param>
    /// <param name="propagate">Whether removal should continue through descendants.</param>
    public void Remove(IEnumerable<IRelevantObject> relevantObjects, bool propagate = true)
    {
        foreach (IRelevantObject relevantObject in relevantObjects)
        {
            Remove(relevantObject, propagate);
        }
    }

    /// <summary>Removes one object and optionally removes its descendants.</summary>
    /// <param name="relevantObject">The object to remove.</param>
    /// <param name="propagate">Whether removal should continue through descendants.</param>
    public void Remove(IRelevantObject relevantObject, bool propagate = true)
    {
        Objects.RemoveRelevantObject(relevantObject);
        if (!propagate || relevantObject.ChildObjects is null)
        {
            return;
        }

        foreach (IRelevantObject child in relevantObject.ChildObjects)
        {
            child.Layer?.Remove(child);
        }
    }

    /// <summary>Disposes every object currently held by the layer.</summary>
    public void Clear()
    {
        foreach (IRelevantObject relevantObject in Objects.SelectMany(kvp => kvp.Value.ToArray()))
        {
            relevantObject.Dispose();
        }
    }
}
