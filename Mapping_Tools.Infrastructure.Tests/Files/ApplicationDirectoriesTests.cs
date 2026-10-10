using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Files;

[TestClass]
public sealed class ApplicationDirectoriesTests
{
    [TestMethod]
    public void Constructor_UsesPlatformApplicationDataForCurrentDirectory()
    {
        // Arrange
        string applicationDataRoot = Environment.GetFolderPath(
            Environment.SpecialFolder.ApplicationData,
            Environment.SpecialFolderOption.Create);
        string legacyApplicationDataRoot = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData,
            Environment.SpecialFolderOption.DoNotVerify);

        // Act
        ApplicationDirectories directories = new();

        // Assert
        directories.LocalApplicationData.Should().Be(Path.GetFullPath(legacyApplicationDataRoot));
        directories.ApplicationData.Should().Be(Path.Combine(
            Path.GetFullPath(applicationDataRoot),
            "Mapping Tools"));
        directories.LegacyApplicationData.Should().Be(Path.Combine(
            Path.GetFullPath(legacyApplicationDataRoot),
            "Mapping Tools"));
    }
}
