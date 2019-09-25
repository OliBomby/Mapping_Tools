using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mapping_Tools.Classes.SnappingTools.RelevantObjectGenerators;

namespace Mapping_Tools.Classes.SnappingTools {
    public class HitObjectGeneratorCollection {
        public List<RelevantObjectsGenerator> Generators;

        /// <summary>
        /// Generates new objects for the next layer based on the new object in the previous layer.
        /// </summary>
        /// <param name="nextLayer">The layer to generate new objects for</param>
        /// <param name="nextContext">Context of the next layer</param>
        /// <param name="hitObject">The new object of the previous layer</param>
        public void GenerateNewObjects(ObjectLayer nextLayer, HitObjectContext nextContext, RelevantHitObject hitObject) {
            // Only generate objects using the new object and the rest and redo all concurrent generators
            throw new NotImplementedException();
        }
    }
}
