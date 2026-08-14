using Mapping_Tools.Infrastructure.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class WindowsOsuEditorReloadServiceTests
{
    [TestMethod]
    public async Task ReloadAsync_WithoutRunningOsu_CompletesSuccessfully()
    {
        // Arrange
        WindowsOsuEditorReloadService service = new(() => null);

        // Act
        Func<Task> act = () => service.ReloadAsync();

        // Assert
        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public void NativeInputSize_MatchesWin32InputStructure()
    {
        // Arrange
        int expected = IntPtr.Size == 8 ? 40 : 28;

        // Act
        int actual = WindowsOsuEditorReloadService.NativeInputSize;

        // Assert
        actual.Should().Be(expected);
    }
}
