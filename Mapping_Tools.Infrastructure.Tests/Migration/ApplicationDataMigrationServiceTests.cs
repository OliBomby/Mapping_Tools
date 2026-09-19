using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Migration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Migration;

[TestClass]
public sealed class ApplicationDataMigrationServiceTests
{
    [TestMethod]
    public async Task CopyLegacyDataAsync_CopiesAutosavesAndProjectsWithoutRemovingLegacyFiles()
    {
        // Arrange
        using var test = TestDirectory.Create();
        string legacyAutosave = Path.Combine(test.Directories.ApplicationData, "patterngalleryproject.json");
        string legacyProjectFile = Path.Combine(
            test.Directories.ApplicationData,
            "Pattern Gallery Projects",
            "My collection",
            "Pattern Files",
            "pattern.osu");
        File.WriteAllText(legacyAutosave, "legacy autosave");
        Directory.CreateDirectory(Path.GetDirectoryName(legacyProjectFile)!);
        File.WriteAllText(legacyProjectFile, "legacy project");
        File.WriteAllText(test.Directories.ConfigurationFile, "legacy configuration");
        ApplicationDataMigrationService service = new(test.Directories);

        // Act
        var result = await service.CopyLegacyDataAsync();

        // Assert
        result.AutosavesCopied.Should().Be(1);
        result.ProjectFilesCopied.Should().Be(1);
        result.ExistingFilesSkipped.Should().Be(0);
        File.ReadAllText(Path.Combine(
                test.Directories.ApplicationData,
                "Autosaves",
                "patterngalleryproject.json"))
            .Should().Be("legacy autosave");
        File.ReadAllText(Path.Combine(
                test.Directories.ApplicationData,
                "Projects",
                "Pattern Gallery Projects",
                "My collection",
                "Pattern Files",
                "pattern.osu"))
            .Should().Be("legacy project");
        File.Exists(legacyAutosave).Should().BeTrue();
        File.Exists(legacyProjectFile).Should().BeTrue();
        File.Exists(test.Directories.ConfigurationFile).Should().BeTrue();
    }

    [TestMethod]
    public async Task CopyLegacyDataAsync_WhenDestinationFileExists_DoesNotOverwriteIt()
    {
        // Arrange
        using var test = TestDirectory.Create();
        string legacyAutosave = Path.Combine(test.Directories.ApplicationData, "patterngalleryproject.json");
        string modernAutosave = Path.Combine(
            test.Directories.ApplicationData,
            "Autosaves",
            "patterngalleryproject.json");
        File.WriteAllText(legacyAutosave, "legacy");
        File.WriteAllText(test.Directories.ConfigurationFile, "legacy configuration");
        Directory.CreateDirectory(Path.GetDirectoryName(modernAutosave)!);
        File.WriteAllText(modernAutosave, "existing");
        ApplicationDataMigrationService service = new(test.Directories);

        // Act
        var result = await service.CopyLegacyDataAsync();

        // Assert
        result.AutosavesCopied.Should().Be(0);
        result.ExistingFilesSkipped.Should().Be(1);
        File.ReadAllText(modernAutosave).Should().Be("existing");
        File.Exists(legacyAutosave).Should().BeTrue();
    }

    [TestMethod]
    public void RequiresMigration_WhenPreferencesExist_IsFalse()
    {
        // Arrange
        using var test = TestDirectory.Create();
        File.WriteAllText(test.Directories.ConfigurationFile, "legacy configuration");
        File.WriteAllText(test.Directories.PreferencesFile, "modern configuration");
        ApplicationDataMigrationService service = new(test.Directories);

        // Act and Assert
        service.RequiresMigration.Should().BeFalse();
    }

    private sealed class TestDirectory : IDisposable
    {
        private TestDirectory()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                "MappingToolsMigrationTests",
                Guid.NewGuid().ToString("N"));
            Directories = new ApplicationDirectories(Root);
            Directories.EnsureCreated();
        }

        public string Root { get; }

        public ApplicationDirectories Directories { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }

        public static TestDirectory Create()
        {
            return new TestDirectory();
        }
    }
}
