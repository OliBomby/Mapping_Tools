using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Viewmodels {
    public class SliderMergerVM {
        public SliderMergerVM() {

        }

        /// <summary>
        /// 
        /// </summary>
        public IEnumerable<ConnectionMode> ConnectionModes => Enum.GetValues(typeof(ConnectionMode)).Cast<ConnectionMode>();
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ConnectionMode {
        /// <summary>
        /// 
        /// </summary>
        Move,

        /// <summary>
        /// 
        /// </summary>
        Linear,
    }
}
