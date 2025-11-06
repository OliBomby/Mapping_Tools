using System.Windows.Input;
using Mapping_Tools.Classes.SystemTools;
using NUnit.Framework;

namespace Mapping_Tools.Tests.Classes.SystemTools;

[TestFixture]
public class ListenerManagerTests {
    [Test]
    public void RemoveActiveHotkeyTest() {
        var listenerManager = new ListenerManager();

        listenerManager.AddActiveHotkey("testKey", new ActionHotkey(new Hotkey(Key.A, ModifierKeys.Alt), () => { }));

        Assert.That(listenerManager.ActiveHotkeys.ContainsKey("testKey"), Is.True);

        listenerManager.RemoveActiveHotkey("testKey");

        Assert.That(listenerManager.ActiveHotkeys.ContainsKey("testKey"), Is.False);
    }
}