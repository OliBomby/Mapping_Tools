namespace Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObject.Layers {
    /// <summary>
    /// Container for a list of objects
    /// </summary>
    public abstract class ObjectLayer {
        public RelevantObjectContext Objects { get; set; }
        public RelevantObjectContext NextContext { get; set; }
        public void SortTimes() {
            Objects.SortTimes();
            NextContext.SortTimes();
        }
    }
}
