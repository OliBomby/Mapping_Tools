using System;

namespace Mapping_Tools.Classes.SystemTools {
    /// <summary>
    /// Links a spesified user Keyboard <see cref="Hotkey"/> with a Action
    /// to the spesified Mapping Tool.
    /// </summary>
    public class ActionHotkey {
        /// <summary>
        /// The spesified user keyboard <see cref="Hotkey"/>
        /// </summary>
        public Hotkey Hotkey;

        /// <summary>
        /// The spesified <see cref="Action"/> linked to the Mapping Tool.
        /// </summary>
        public Action Action;

        /// <summary>
        /// Constructer that links a spesified <see cref="Hotkey"/> 
        /// to the Mapping Tools <see cref="Action"/>.
        /// </summary>
        /// <param name="hotkey">The spesified user keyboard <see cref="Hotkey"/></param>
        /// <param name="action">The spesified <see cref="Action"/> linked to the Mapping Tool.</param>
        public ActionHotkey(Hotkey hotkey, Action action) {
            Hotkey = hotkey;
            Action = action;
        }
    }
}
