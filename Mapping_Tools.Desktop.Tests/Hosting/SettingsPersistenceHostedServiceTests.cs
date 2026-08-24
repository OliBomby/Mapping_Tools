using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Settings.Contracts;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Desktop.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Hosting;

[TestClass]
public sealed class SettingsPersistenceHostedServiceTests
{
    [TestMethod]
    public async Task StopAsync_WithChangedSettings_SavesSharedDocumentOnce()
    {
        // Arrange
        ApplicationSettings settings = new()
        {
            SongsPath = @"D:\osu!\Songs",
        };
        RecordingSettingsService settingsService = new();
        SettingsPersistenceHostedService service = new(settings, settingsService);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert
        settingsService.SaveCount.Should().Be(1);
        settingsService.LastSaved.Should().BeSameAs(settings);
    }

    [TestMethod]
    public async Task StopAsync_AfterSuppressSave_DoesNotPersistSettings()
    {
        // Arrange
        RecordingSettingsService settingsService = new();
        SettingsPersistenceHostedService service = new(new ApplicationSettings(), settingsService);
        service.SuppressSave();

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert
        settingsService.SaveCount.Should().Be(0);
    }

    private sealed class RecordingSettingsService : ISettingsService
    {
        public int SaveCount { get; private set; }

        public ApplicationSettings? LastSaved { get; private set; }

        public SettingsLoadResult LoadOrCreate()
        {
            return new SettingsLoadResult(new ApplicationSettings(), false, false);
        }

        public void Save(ApplicationSettings settings)
        {
            SaveCount++;
            LastSaved = settings;
        }
    }
}
