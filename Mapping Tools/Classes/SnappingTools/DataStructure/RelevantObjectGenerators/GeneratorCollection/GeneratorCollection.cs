using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection {
    public abstract class GeneratorCollection {
        public List<RelevantObjectsGenerator> Generators;

        public IEnumerable<RelevantObjectsGenerator> GetActiveGenerators() {
            return Generators.Where(o => o.IsActive);
        }

        public abstract void GenerateNewObjects(ObjectLayer thisLayer, ObjectLayer nextLayer,
            IRelevantObject newRelevantObject);

        public abstract void GenerateNewObjects(ObjectLayer thisLayer, ObjectLayer nextLayer,
            IRelevantObject[] newRelevantObjects);
    }
}
