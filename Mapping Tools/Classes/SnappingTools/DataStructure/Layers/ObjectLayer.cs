namespace Mapping_Tools.Classes.SnappingTools.DataStructure.Layers {
    /// <summary>
    /// Container for a list of objects
    /// </summary>
    public abstract class ObjectLayer {

        /// <summary>
        /// 
        /// </summary>
        public RelevantObjectCollection.RelevantObjectCollection Objects { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public RelevantObjectCollection.RelevantObjectCollection NextContext { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public void SortTimes() {
            Objects.SortTimes();
            NextContext.SortTimes();
        }
    }
}
