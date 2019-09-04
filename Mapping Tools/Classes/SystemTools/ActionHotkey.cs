using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SystemTools {
    public class ActionHotkey {
        public Hotkey Hotkey;
        public Action Action;

        public ActionHotkey(Hotkey hotkey, Action action) {
            Hotkey = hotkey;
            Action = action;
        }
    }
}
