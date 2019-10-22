using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure {
    public class LayerCollection {
        public List<RelevantObjectLayer> ObjectLayers;

        public RelevantObjectsGeneratorCollection AllGenerators;

        public double AcceptableDifference { get; set; }

        public LayerCollection(RelevantObjectsGeneratorCollection generators, double acceptableDifference) {
            ObjectLayers = new List<RelevantObjectLayer>();
            AllGenerators = generators;
            AcceptableDifference = acceptableDifference;

            // Generate 1 layer
            ObjectLayers.Add(new RelevantObjectLayer(this, AllGenerators));
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
                }
            } else if (ObjectLayers.Count > inceptionLevel) {
                // Remove layers
                var layersToRemove = ObjectLayers.Count - inceptionLevel;
                for (var i = 0; i < layersToRemove; i++) {
                    ObjectLayers.RemoveAt(ObjectLayers.Count - 1);
                    var lastLayer = ObjectLayers.LastOrDefault();
                    if (lastLayer != null) lastLayer.NextLayer = null;
                }
            }
        }

        public IEnumerable<IRelevantObject> GetAllRelevantObjects() {
            return ObjectLayers.SelectMany(a => a.Objects.Values.SelectMany(b => b));
        }

        /// <summary>
        /// Gets all objects of all layers that implement IRelevantDrawable
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IRelevantDrawable> GetAllRelevantDrawables() {
            return ObjectLayers
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
