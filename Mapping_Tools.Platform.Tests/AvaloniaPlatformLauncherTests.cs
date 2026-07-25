using Mapping_Tools.Desktop.Platform;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class AvaloniaPlatformLauncherTests
{
    [TestMethod]
    public async Task OpenUriAsync_WithoutTopLevel_ThrowsInvalidOperationException()
    {
        // Arrange
        AvaloniaPlatformLauncher launcher = new(() => null);

        // Act
        Func<Task> act1 = () => launcher.OpenUriAsync(new Uri("https://mappingtools.github.io"));

        // Assert
        await act1.Should().ThrowAsync<InvalidOperationException>();
    }

    [TestMethod]
    public async Task OpenUriAsync_WithRelativeUri_ThrowsBeforePlatformAccess()
    {
        // Arrange
        bool accessed = false;
        AvaloniaPlatformLauncher launcher = new(() =>
        {
            accessed = true;
            return null;
        });

        // Act
        Func<Task> act2 = () => launcher.OpenUriAsync(new Uri("relative", UriKind.Relative));

        // Assert
        await act2.Should().ThrowAsync<ArgumentException>();

        accessed.Should().BeFalse();
    }

    [TestMethod]
    public async Task OpenFileAsync_WithMissingFile_ThrowsBeforePlatformAccess()
    {
        // Arrange
        bool accessed = false;
        AvaloniaPlatformLauncher launcher = new(() =>
        {
            accessed = true;
            return null;
        });
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".missing");

        // Act
        Func<Task> act3 = () => launcher.OpenFileAsync(path);

        // Assert
        await act3.Should().ThrowAsync<FileNotFoundException>();

        accessed.Should().BeFalse();
    }

    [TestMethod]
    public async Task OpenFolderAsync_WithMissingFolder_ThrowsBeforePlatformAccess()
    {
        // Arrange
        bool accessed = false;
        AvaloniaPlatformLauncher launcher = new(() =>
        {
            accessed = true;
            return null;
        });
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        Func<Task> act4 = () => launcher.OpenFolderAsync(path);

        // Assert
        await act4.Should().ThrowAsync<DirectoryNotFoundException>();

        accessed.Should().BeFalse();
    }
}
