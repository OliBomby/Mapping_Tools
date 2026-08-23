using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;

namespace Mapping_Tools.Core.Classes.Tools.SnappingTools.DataStructure;

/// <summary>Owns the ordered root and generated layers for one calculation session.</summary>
public sealed class LayerCollection
{
    /// <summary>Creates a layer collection with one empty root layer.</summary>
    /// <param name="generators">The generator catalog.</param>
    /// <param name="acceptableDifference">The duplicate-distance tolerance.</param>
    public LayerCollection(RelevantObjectsGeneratorCollection generators, double acceptableDifference)
    {
        AllGenerators = generators;
        AcceptableDifference = acceptableDifference;
        // Generate 1 layer
        ObjectLayers.Add(new RelevantObjectLayer(this, null));
    }

    /// <summary>Gets the mutable layers in root-to-deepest order.</summary>
    public List<RelevantObjectLayer> ObjectLayers { get; } = [];

    /// <summary>Gets the generator catalog used by generated layers.</summary>
    public RelevantObjectsGeneratorCollection AllGenerators { get; }

    /// <summary>Gets or sets the strict duplicate-distance tolerance.</summary>
    public double AcceptableDifference { get; set; }

    /// <summary>Gets the maximum number of objects permitted in one layer.</summary>
    public int MaxObjects => 1000;

    /// <summary>Changes the number of layers and generates newly added layers.</summary>
    /// <param name="inceptionLevel">The requested number of layers.</param>
    /// <exception cref="ArgumentException">The requested count is negative.</exception>
    public void SetInceptionLevel(int inceptionLevel)
    {
        if (inceptionLevel < 0) throw new ArgumentException("Inception level can't be less than 0.", nameof(inceptionLevel));

        if (ObjectLayers.Count < inceptionLevel)
        {
            // Add more layers
            int layersToAdd = inceptionLevel - ObjectLayers.Count;
            for (int i = 0; i < layersToAdd; i++)
            {
                var lastLayer = ObjectLayers.LastOrDefault();
                RelevantObjectLayer newLayer = new(this, AllGenerators) { PreviousLayer = lastLayer };
                if (lastLayer is not null) lastLayer.NextLayer = newLayer;

                ObjectLayers.Add(newLayer);
                // Derive new relevant objects in the new layer
                newLayer.GenerateNewObjects();
            }
        }
        else if (ObjectLayers.Count > inceptionLevel)
        {
            // Remove layers
            int layersToRemove = ObjectLayers.Count - inceptionLevel;
            for (int i = 0; i < layersToRemove; i++)
            {
                // Dispose all objects from last layer
                ObjectLayers[^1].Clear();
                ObjectLayers.RemoveAt(ObjectLayers.Count - 1);
                var lastLayer = ObjectLayers.LastOrDefault();
                if (lastLayer is not null) lastLayer.NextLayer = null;
            }
        }
    }

    /// <summary>Gets every object across every layer.</summary>
    /// <returns>The live graph objects.</returns>
    public IEnumerable<IRelevantObject> GetAllRelevantObjects()
    {
        return ObjectLayers.SelectMany(layer => layer.Objects.Values.SelectMany(objects => objects));
    }

    /// <summary>Gets every object that exposes geometric hit-testing operations.</summary>
    /// <returns>The drawable geometry objects.</returns>
    public IEnumerable<IRelevantDrawable> GetAllRelevantDrawables()
    {
        return GetAllRelevantObjects().OfType<IRelevantDrawable>();
    }

    /// <summary>Gets the root layer.</summary>
    /// <returns>The first layer.</returns>
    public RelevantObjectLayer GetRootLayer()
    {
        return ObjectLayers[0];
    }

    /// <summary>Gets root hit-object wrappers in timestamp order.</summary>
    /// <returns>The root hit objects.</returns>
    public IEnumerable<RelevantHitObject> GetRootRelevantHitObjects()
    {
        return GetRootLayer().Objects.TryGetValue(typeof(RelevantHitObject), out var list)
            ? list.Cast<RelevantHitObject>()
            : [];
    }
}
