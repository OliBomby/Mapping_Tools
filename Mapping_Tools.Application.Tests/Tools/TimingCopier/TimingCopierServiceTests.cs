using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.TimingCopier;
using Mapping_Tools.Core.Tools.TimingCopier.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.TimingCopier;

[TestClass]
public sealed class TimingCopierServiceTests
{
    [TestMethod]
    public async Task CopyAsync_WithMultipleTargets_UsesLivePreferenceAndSavesEachTarget()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingBeatmapEditingGateway gateway = CreateGateway(fixture);
        TimingCopierService service = new(gateway);
        TimingCopierProject options = new()
        {
            ImportPath = "source.osu",
            ExportPath = "first.osu|second.osu",
            ResnapMode = TimingCopierResnapMode.KeepObjectsFixed,
        };
        RecordingProgress<double> progress = new();

        // Act
        var result = await service.CopyAsync(options, progress);

        // Assert
        result.ProcessedPaths.Should().Equal("first.osu", "second.osu");
        gateway.OpenRequests.Select(request => request.Path)
            .Should().Equal("source.osu", "first.osu", "second.osu");
        gateway.OpenRequests.Select(request => request.Preference)
            .Should().OnlyContain(preference => preference == LiveBeatmapPreference.PreferLive);
        gateway.SessionSaveRequests.Select(request => request.Session.Editor.Path)
            .Should().Equal("first.osu", "second.osu");
        progress.Values.Last().Should().Be(1);
    }

    [TestMethod]
    public async Task CopyAsync_WhenSecondTargetSaveFails_LeavesFirstTargetSaved()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingBeatmapEditingGateway gateway = CreateGateway(fixture);
        gateway.SaveSessionAction = (_, _) =>
        {
            if (gateway.SessionSaveRequests.Count == 2)
                throw new IOException("The test target could not be written.");
        };
        TimingCopierService service = new(gateway);
        TimingCopierProject options = new()
        {
            ImportPath = "source.osu",
            ExportPath = "first.osu|second.osu",
            ResnapMode = TimingCopierResnapMode.KeepObjectsFixed,
        };

        // Act
        Func<Task> act = () => service.CopyAsync(options);

        // Assert
        await act.Should().ThrowAsync<IOException>();
        gateway.CompletedSessionSaveRequests.Select(request => request.Session.Editor.Path)
            .Should().ContainSingle().Which.Should().Be("first.osu");
    }

    [TestMethod]
    public async Task CopyAsync_WithEmptyBeatDivisors_ThrowsBeforeOpeningBeatmaps()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingBeatmapEditingGateway gateway = CreateGateway(fixture);
        TimingCopierService service = new(gateway);
        TimingCopierProject options = new()
        {
            ImportPath = "source.osu",
            ExportPath = "target.osu",
            BeatDivisors = [],
        };

        // Act
        Func<Task> act = () => service.CopyAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        gateway.OpenRequests.Should().BeEmpty();
    }

    [TestMethod]
    public async Task CopyAsync_WithUndefinedResnapMode_ThrowsBeforeOpeningBeatmaps()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        RecordingBeatmapEditingGateway gateway = CreateGateway(fixture);
        TimingCopierService service = new(gateway);
        TimingCopierProject options = new()
        {
            ImportPath = "source.osu",
            ExportPath = "target.osu",
            ResnapMode = (TimingCopierResnapMode)999,
        };

        // Act
        Func<Task> act = () => service.CopyAsync(options);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        gateway.OpenRequests.Should().BeEmpty();
    }

    private static RecordingBeatmapEditingGateway CreateGateway(string fixture)
    {
        return new RecordingBeatmapEditingGateway
        {
            OpenBeatmapFactory = (path, _) =>
            {
                BeatmapEditor editor = new(
                    File.ReadAllLines(fixture).ToList(),
                    new NoOpTextFileStore())
                {
                    Path = path,
                };
                return new BeatmapEditingSession(editor, BeatmapEditingSource.Disk, []);
            },
        };
    }

}
