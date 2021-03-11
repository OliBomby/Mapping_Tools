namespace Mapping_Tools_Core_Tests.SystemTools {
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