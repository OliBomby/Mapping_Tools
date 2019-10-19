using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.SnappingTools.DataStructure.Layers;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.Allocation;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection {
    public class RelevantObjectsGeneratorCollection : List<RelevantObjectsGenerator> {
        public IEnumerable<RelevantObjectsGenerator> GetActiveGenerators() {
            return this.Where(o => o.IsActive);
        }
    }
}
