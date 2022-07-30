using System.Windows.Input;
using Mapping_Tools.Classes.SystemTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.SystemTools {
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