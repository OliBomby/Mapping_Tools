using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SystemTools {
    public class RunToolCompletedEventArgs : EventArgs {
        public bool Success;
        public bool NeedReload;

        public RunToolCompletedEventArgs(bool success, bool needReload) {
            Success = success;
            NeedReload = needReload;
        }
    }
}
