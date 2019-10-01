using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure {
    public class LayerCollection {
        public HitObjectLayer HitObjectLayer;
        public List<RelevantObjectLayer> RelevantObjectLayers;
        public double AcceptableDifference { get; set; }

        public void SetInceptionLevel(int inceptionLevel) {
            throw new NotImplementedException();
        }

        public List<IRelevantObject> GetAllRelevantObjects() {
            throw new NotImplementedException();
        }
    }
}
