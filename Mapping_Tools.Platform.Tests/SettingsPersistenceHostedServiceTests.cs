using Mapping_Tools.Application.Settings;
using Mapping_Tools.Desktop.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class SettingsPersistenceHostedServiceTests
{
    [TestMethod]
    public async Task StopAsync_WithChangedSettings_SavesSharedDocumentOnce()
    {
        // Arrange
        ApplicationSettings settings = new()
        {
            SongsPath = @"D:\osu!\Songs"
        };
        RecordingSettingsService settingsService = new();
        SettingsPersistenceHostedService service = new(settings, settingsService);

        // Act
        await service.StopAsync(CancellationToken.None);

        // Assert
        settingsService.SaveCount.Should().Be(1);
        settingsService.LastSaved.Should().BeSameAs(settings);
    }

    private sealed class RecordingSettingsService : ISettingsService
    {
        public int SaveCount { get; private set; }

        public ApplicationSettings? LastSaved { get; private set; }

        public SettingsLoadResult LoadOrCreate() =>
            new(new ApplicationSettings(), false, false);

        public void Save(ApplicationSettings settings)
        {
            SaveCount++;
            LastSaved = settings;
        }
    }
}
