using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
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
        BeatmapEditor editor = new(
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Fixtures", "Beatmaps", "standard-feature-rich.osu")).ToList(),
            new NoOpTextFileStore
            {
                ParentFolderResolver = _ => @"C:\set",
                CombinePathResolver = Path.Combine,
            }) { Path = @"C:\set\map.osu" };
        RecordingBeatmapEditingGateway gateway = new(
            new BeatmapEditingSession(editor, BeatmapEditingSource.LiveEditor, []));
        ComboColourProject project = new();
        project.ComboColours.Clear();
        project.ComboColours.Add(new SpecialColour(RgbaColour.FromRgb(10, 20, 30), "Combo1"));
        project.AddColourPoint(0, [project.ComboColours[0]]);
        List<double> progress = [];
        ComboColourStudioService service = new(gateway);

        // Act
        var result = await service.ApplyAsync(
            [editor.Path],
            project,
            new Progress<double>(progress.Add));

        // Assert
        result.ProcessedCount.Should().Be(1);
        gateway.OpenRequests.Single().Preference.Should().Be(LiveBeatmapPreference.PreferLive);
        gateway.SessionSaveRequests.Single().Session.Editor.Should().BeSameAs(editor);
        gateway.SessionSaveRequests.Single().ReloadEditor.Should().BeFalse();
        editor.Beatmap.ComboColours.Single().Color.Should().Be(RgbaColour.FromRgb(10, 20, 30));
        progress.Should().Equal(1);
    }

    [TestMethod]
    public async Task ApplyAsync_WithoutTargetMaps_ThrowsValidationException()
    {
        // Arrange
        BeatmapEditor editor = new(
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
        RecordingBeatmapEditingGateway gateway = new(
            new BeatmapEditingSession(editor, BeatmapEditingSource.LiveEditor, []));
        ComboColourProject project = new();
        project.AddComboColour();
        ComboColourStudioService service = new(gateway);

        // Act
        Func<Task> act = () => service.ApplyAsync([], project);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

}
