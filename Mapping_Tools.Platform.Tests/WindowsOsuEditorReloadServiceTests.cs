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
}
