using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools.Viewmodels {
    public class SliderMergerVM {
        public SliderMergerVM() {

        }

        public IEnumerable<ConnectionMode> ConnectionModes => Enum.GetValues(typeof(ConnectionMode)).Cast<ConnectionMode>();
    }

    public enum ConnectionMode {
        Move,
        Linear,
    }
}
