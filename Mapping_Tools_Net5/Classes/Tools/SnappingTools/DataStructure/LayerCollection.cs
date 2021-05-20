using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObject.RelevantObjects;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;

namespace Mapping_Tools.Classes.Tools.SnappingTools.DataStructure {
    public class LayerCollection {
        public List<RelevantObjectLayer> ObjectLayers;

        public RelevantObjectsGeneratorCollection AllGenerators;

        public RelevantObjectLayer LockedLayer;

        public double AcceptableDifference { get; set; }

        /// <summary>
        /// This is the maximum number of relevant objects any layer may hold
        /// </summary>
        public int MaxObjects { get; } = 1000;

        public LayerCollection(RelevantObjectsGeneratorCollection generators, double acceptableDifference) {
            ObjectLayers = new List<RelevantObjectLayer>();
            AllGenerators = generators;
            AcceptableDifference = acceptableDifference;
            LockedLayer = new RelevantObjectLayer(this, null);

            // Generate 1 layer
            ObjectLayers.Add(new RelevantObjectLayer(this, AllGenerators));

            // Set the previous layer of the rootlayer to the locked layer so every layer has the locked layer
            GetRootLayer().PreviousLayer = LockedLayer;
            LockedLayer.NextLayer = GetRootLayer();
        }

        public void SetInceptionLevel(int inceptionLevel) {
            if (inceptionLevel < 0) {
                throw new ArgumentException("Inception level can't be less than 0.");
            }

            if (ObjectLayers.Count < inceptionLevel) {
                // Add more layers
                var layersToAdd = inceptionLevel - ObjectLayers.Count;
                for (var i = 0; i < layersToAdd; i++) {
                    var lastLayer = ObjectLayers.LastOrDefault();
                    var newLayer = new RelevantObjectLayer(this, AllGenerators) {PreviousLayer = lastLayer};
                    if (lastLayer != null) lastLayer.NextLayer = newLayer;
                    ObjectLayers.Add(newLayer);

                    // Derive new relevant objects in the new layer
                    newLayer.GenerateNewObjects();
                }
            } else if (ObjectLayers.Count > inceptionLevel) {
                // Remove layers
                var layersToRemove = ObjectLayers.Count - inceptionLevel;
                for (var i = 0; i < layersToRemove; i++) {
                    // Dispose all objects from last layer
                    ObjectLayers[ObjectLayers.Count - 1].Clear();

                    ObjectLayers.RemoveAt(ObjectLayers.Count - 1);
                    var lastLayer = ObjectLayers.LastOrDefault();
                    if (lastLayer != null) lastLayer.NextLayer = null;
                }
            }
        }

        public IEnumerable<IRelevantObject> GetAllRelevantObjects() {
            return ObjectLayers.Concat(new []{LockedLayer}).SelectMany(a => a.Objects.Values.SelectMany(b => b));
        }

        /// <summary>
        /// Gets all objects of all layers that implement IRelevantDrawable
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IRelevantDrawable> GetAllRelevantDrawables() {
            return ObjectLayers.Concat(new []{LockedLayer})
                .SelectMany(layer =>
                    layer.Objects.Where(kvp => typeof(IRelevantDrawable).IsAssignableFrom(kvp.Key))
                        .SelectMany(kvp => kvp.Value)).Cast<IRelevantDrawable>();
        }

        public RelevantObjectLayer GetRootLayer() {
            return ObjectLayers[0];
        }

        public IEnumerable<RelevantHitObject> GetRootRelevantHitObjects() {
            return GetRootLayer().Objects.TryGetValue(typeof(RelevantHitObject), out var list)
                ? list.Cast<RelevantHitObject>()
                : new RelevantHitObject[0];
        }
    }
}
