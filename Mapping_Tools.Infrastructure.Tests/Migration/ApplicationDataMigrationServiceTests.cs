using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Migration;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Migration;

[TestClass]
public sealed class ApplicationDataMigrationServiceTests
{
    [TestMethod]
    public async Task CopyLegacyDataAsync_WhenLegacyFilesExist_CopiesConfigAutosavesAndProjectsWithoutRemovingLegacyFiles()
    {
        // Arrange
        using var test = TestDirectory.Create();
        string legacyAutosave = Path.Combine(test.Directories.LegacyApplicationData, "patterngalleryproject.json");
        string legacyProjectFile = Path.Combine(
            test.Directories.LegacyApplicationData,
            "Pattern Gallery Projects",
            "My collection",
            "Pattern Files",
            "pattern.osu");
        string legacyConfiguration = Path.Combine(test.Directories.LegacyApplicationData, "config.json");
        await File.WriteAllTextAsync(legacyAutosave, "legacy autosave");
        Directory.CreateDirectory(Path.GetDirectoryName(legacyProjectFile)!);
        await File.WriteAllTextAsync(legacyProjectFile, "legacy project");
        await File.WriteAllTextAsync(legacyConfiguration, "legacy configuration");
        ApplicationDataMigrationService service = new(test.Directories);

        // Act
        var result = await service.CopyLegacyDataAsync();

        // Assert
        result.AutosavesCopied.Should().Be(1);
        result.ProjectFilesCopied.Should().Be(1);
        result.ExistingFilesSkipped.Should().Be(0);
        (await File.ReadAllTextAsync(test.Directories.ConfigurationFile)).Should().Be("legacy configuration");
        (await File.ReadAllTextAsync(Path.Combine(
                test.Directories.ApplicationData,
                "Autosaves",
                "patterngalleryproject.json")))
            .Should().Be("legacy autosave");
        (await File.ReadAllTextAsync(Path.Combine(
                test.Directories.ApplicationData,
                "Projects",
                "Pattern Gallery Projects",
                "My collection",
                "Pattern Files",
                "pattern.osu")))
            .Should().Be("legacy project");
        File.Exists(legacyAutosave).Should().BeTrue();
        File.Exists(legacyProjectFile).Should().BeTrue();
        File.Exists(legacyConfiguration).Should().BeTrue();
    }

    [TestMethod]
    public async Task CopyLegacyDataAsync_WhenDestinationFileExists_DoesNotOverwriteIt()
    {
        // Arrange
        using var test = TestDirectory.Create();
        string legacyAutosave = Path.Combine(test.Directories.LegacyApplicationData, "patterngalleryproject.json");
        string modernAutosave = Path.Combine(
            test.Directories.ApplicationData,
            "Autosaves",
            "patterngalleryproject.json");
        await File.WriteAllTextAsync(legacyAutosave, "legacy");
        await File.WriteAllTextAsync(
            Path.Combine(test.Directories.LegacyApplicationData, "config.json"),
            "legacy configuration");
        Directory.CreateDirectory(Path.GetDirectoryName(modernAutosave)!);
        await File.WriteAllTextAsync(modernAutosave, "existing");
        ApplicationDataMigrationService service = new(test.Directories);

        // Act
        var result = await service.CopyLegacyDataAsync();

        // Assert
        result.AutosavesCopied.Should().Be(0);
        result.ExistingFilesSkipped.Should().Be(1);
        (await File.ReadAllTextAsync(modernAutosave)).Should().Be("existing");
        File.Exists(legacyAutosave).Should().BeTrue();
    }

    [TestMethod]
    public async Task CopyLegacyDataAsync_WhenCurrentPreferencesAndLegacyConfigExist_PreservesCurrentSettings()
    {
        // Arrange
        using var test = TestDirectory.Create();
        File.WriteAllText(
            Path.Combine(test.Directories.LegacyApplicationData, "config.json"),
            "legacy configuration");
        File.WriteAllText(test.Directories.ConfigurationFile, "stale configuration");
        string preferencesFile = Path.Combine(test.Directories.ApplicationData, "preferences.json");
        File.WriteAllText(preferencesFile, "modern configuration");
        ApplicationDataMigrationService service = new(test.Directories);

        // Act
        var result = await service.CopyLegacyDataAsync();

        // Assert
        (await File.ReadAllTextAsync(test.Directories.ConfigurationFile)).Should().Be("modern configuration");
        File.Exists(preferencesFile).Should().BeFalse();
        service.LastMigrationResult.Should().Be(result);
    }

    [TestMethod]
    public async Task CopyLegacyDataAsync_WhenPreferencesExistWithoutLegacyDirectory_CopiesPreferences()
    {
        // Arrange
        using var test = TestDirectory.Create();
        Directory.Delete(test.Directories.LegacyApplicationData, recursive: true);
        string preferencesFile = Path.Combine(test.Directories.ApplicationData, "preferences.json");
        await File.WriteAllTextAsync(preferencesFile, "modern configuration");
        ApplicationDataMigrationService service = new(test.Directories);

        // Act
        await service.CopyLegacyDataAsync();

        // Assert
        (await File.ReadAllTextAsync(test.Directories.ConfigurationFile)).Should().Be("modern configuration");
    }

    [TestMethod]
    public void RequiresMigration_WhenCurrentConfigurationExists_IsFalse()
    {
        // Arrange
        using var test = TestDirectory.Create();
        File.WriteAllText(
            Path.Combine(test.Directories.LegacyApplicationData, "config.json"),
            "legacy configuration");
        File.WriteAllText(test.Directories.ConfigurationFile, "current configuration");
        ApplicationDataMigrationService service = new(test.Directories);

        // Act
        bool requiresMigration = service.RequiresMigration;

        // Assert
        requiresMigration.Should().BeFalse();
    }

    private sealed class TestDirectory : IDisposable
    {
        private TestDirectory()
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                "MappingToolsMigrationTests",
                Guid.NewGuid().ToString("N"));
            Directories = new ApplicationDirectories(
                Path.Combine(Root, "Roaming"),
                legacyApplicationDataRoot: Path.Combine(Root, "Local"));
            Directories.EnsureCreated();
            Directory.CreateDirectory(Directories.LegacyApplicationData);
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
