using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapping_Tools.Classes.BeatmapHelper;

namespace Mapping_Tools.Classes.SnappingTools {
    /// <summary>
    /// Container for a list of HitObjects
    /// </summary>
    public class HitObjectLayer : ObjectLayer<HitObject> {
        public void Add(HitObject hitObject) {
            ObjectList.Add(hitObject);

            // Sort the object list
            // Redo any generators that need concurrent HitObjects
            // Generate objects to add to the next layer
            // Invoke event so the layer collection can update the next layer
        }
    }
}
