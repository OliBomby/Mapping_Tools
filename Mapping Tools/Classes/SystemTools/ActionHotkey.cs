using System;

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
