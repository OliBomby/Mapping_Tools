using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.ComboColourStudio;

[TestClass]
public sealed class ComboColourStudioServiceTests
{
    [TestMethod]
    public async Task ApplyAsync_WithMapAndValidProject_UsesLivePreferenceAndSavesProgress()
    {
        // Arrange
        BeatmapEditingSession editor = new(
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Beatmaps", "standard-feature-rich.osu")).ToList(),
            new NoOpTextFileStore
            {
                ParentFolderResolver = _ => @"C:\set",
                CombinePathResolver = Path.Combine,
            }) { Path = @"C:\set\map.osu" };
        RecordingBeatmapEditingGateway gateway = new(editor);
        ComboColourServiceOptions project = new();
        project.ComboColours.Clear();
        project.ComboColours.Add(new SpecialColour(RgbaColour.FromRgb(10, 20, 30), "Combo1"));
        project.AddColourPoint(0, [project.ComboColours[0]]);
        RecordingProgress<double> progress = new();
        ComboColourStudioService service = new(gateway, new ApplicationSettings());

        // Act
        var result = await service.ApplyAsync(
            [editor.Path],
            project,
            false,
            progress);

        // Assert
        result.ProcessedCount.Should().Be(1);
        gateway.OpenRequests.Single().Preference.Should().Be(LiveBeatmapPreference.PreferLive);
        gateway.SessionSaveRequests.Single().Session.Should().BeSameAs(editor);
        gateway.SessionSaveRequests.Single().ReloadEditor.Should().BeFalse();
        editor.Beatmap.ComboColours.Single().Color.Should().Be(RgbaColour.FromRgb(10, 20, 30));
        progress.Values.Should().Equal(1);
    }

    [TestMethod]
    public async Task ApplyAsync_WithoutTargetMaps_ThrowsValidationException()
    {
        // Arrange
        BeatmapEditingSession editor = new(
            File.ReadAllLines(Path.Combine(
                AppContext.BaseDirectory,
                "Fixtures",
                "Beatmaps",
                "standard-feature-rich.osu")).ToList(),
            new NoOpTextFileStore
            {
                ParentFolderResolver = _ => @"C:\set",
                CombinePathResolver = Path.Combine,
            });
        RecordingBeatmapEditingGateway gateway = new(editor);
        ComboColourServiceOptions project = new();
        project.AddComboColour();
        ComboColourStudioService service = new(gateway, new ApplicationSettings());

        // Act
        Func<Task> act = () => service.ApplyAsync([], project);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task ImportComboColoursAsync_WithLiveMap_ReturnsPaletteAndUsesPreferLive()
    {
        // Arrange
        BeatmapEditingSession editor = CreateSession();
        RecordingBeatmapEditingGateway gateway = new(editor);
        ComboColourStudioService service = new(gateway, new ApplicationSettings());

        // Act
        ComboColourEngineOptions result = await service.ImportComboColoursAsync(editor.Path);

        // Assert
        gateway.OpenRequests.Single().Preference.Should().Be(LiveBeatmapPreference.PreferLive);
        result.ComboColours.Should().NotBeEmpty();
        result.ComboColours.Should().NotContain(colour => ReferenceEquals(colour, editor.Beatmap.ComboColours.FirstOrDefault()));
    }

    [TestMethod]
    public async Task ImportColourHaxAsync_WithLiveMap_ReturnsInferredProjectAndUsesPreferLive()
    {
        // Arrange
        BeatmapEditingSession editor = CreateSession();
        RecordingBeatmapEditingGateway gateway = new(editor);
        ComboColourStudioService service = new(gateway, new ApplicationSettings());

        // Act
        ComboColourEngineOptions result = await service.ImportColourHaxAsync(editor.Path, 2);

        // Assert
        gateway.OpenRequests.Single().Preference.Should().Be(LiveBeatmapPreference.PreferLive);
        result.MaxBurstLength.Should().Be(2);
        result.ComboColours.Should().NotBeEmpty();
    }

    private static BeatmapEditingSession CreateSession()
    {
        return new BeatmapEditingSession(
            File.ReadAllLines(Path.Combine(
                AppContext.BaseDirectory,
                "Fixtures",
                "Beatmaps",
                "standard-feature-rich.osu")).ToList(),
            new NoOpTextFileStore
            {
                ParentFolderResolver = _ => @"C:\set",
                CombinePathResolver = Path.Combine,
            }) { Path = @"C:\set\map.osu" };
    }

}
