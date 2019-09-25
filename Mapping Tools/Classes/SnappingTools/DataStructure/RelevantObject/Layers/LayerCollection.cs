using System;
using System.Collections.Generic;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.Layers {
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
