using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorCollection;

namespace Mapping_Tools.Classes.SnappingTools.DataStructure.Layers {
    /// <summary>
    /// Container for a list of objects
    /// </summary>
    public abstract class ObjectLayer {
        public RelevantObjectCollection.RelevantObjectCollection Objects { get; set; }
        public abstract HitObjectGeneratorCollection GeneratorCollection { get; set; }
        public LayerCollection ParentCollection { get; set; }

        public void SortTimes() {
            Objects.SortTimes();
        }

        public abstract void Add(object obj);
    }
}
