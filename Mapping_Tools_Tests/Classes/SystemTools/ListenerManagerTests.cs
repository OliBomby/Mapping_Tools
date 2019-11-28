using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mapping_Tools.Classes.SystemTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mapping_Tools.Classes.SystemTools.Tests {
    [TestClass]
    public class ListenerManagerTests {
        [TestMethod]
        public void RemoveActiveHotkeyTest() {
            var listenerManager = new ListenerManager();

            listenerManager.AddActiveHotkey("testKey", new ActionHotkey(new Hotkey(Key.A, ModifierKeys.Alt), () => {}));

            Assert.IsTrue(listenerManager.ActiveHotkeys.ContainsKey("testKey"));

            listenerManager.RemoveActiveHotkey("testKey");

            Assert.IsFalse(listenerManager.ActiveHotkeys.ContainsKey("testKey"));
        }
    }
}