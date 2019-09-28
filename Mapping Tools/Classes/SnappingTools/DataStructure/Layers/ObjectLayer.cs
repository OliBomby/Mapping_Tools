namespace Mapping_Tools.Classes.SnappingTools.DataStructure.Layers {
    /// <summary>
    /// Container for a list of objects
    /// </summary>
    public abstract class ObjectLayer {
        public RelevantObjectCollection.RelevantObjectCollection Objects { get; set; }
        public RelevantObjectCollection.RelevantObjectCollection NextContext { get; set; }
        public void SortTimes() {
            Objects.SortTimes();
            NextContext.SortTimes();
        }
    }
}
