using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SnappingTools {
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
