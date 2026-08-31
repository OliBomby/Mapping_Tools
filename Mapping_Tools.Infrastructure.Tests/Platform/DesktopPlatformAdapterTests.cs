using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Platform;

[TestClass]
public sealed class DesktopPlatformAdapterTests
{
    [TestMethod]
    public void ApplicationDirectories_WithRoot_PreservesLegacyLayout()
    {
        // Arrange
        // Act
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        ApplicationDirectories directories = new(root);

        // Assert
        directories.ApplicationData.Should().Be(Path.Combine(Path.GetFullPath(root), "Mapping Tools"));
        directories.LocalApplicationData.Should().Be(Path.GetFullPath(root));
        directories.Exports.Should().Be(Path.Combine(Path.GetFullPath(root), "Mapping Tools", "Exports"));
        directories.ConfigurationFile.Should().Be(Path.Combine(Path.GetFullPath(root), "Mapping Tools", "config.json"));
        directories.PreferencesFile.Should().Be(Path.Combine(Path.GetFullPath(root), "Mapping Tools", "preferences.json"));
    }

    [TestMethod]
    public void EnsureCreated_WithMissingFolders_CreatesApplicationAndExportFolders()
    {
        // Arrange
        string root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        ApplicationDirectories directories = new(root);

        // Act
        try
        {
            directories.EnsureCreated();

            // Assert
            Directory.Exists(directories.ApplicationData).Should().BeTrue();
            Directory.Exists(directories.Exports).Should().BeTrue();
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
