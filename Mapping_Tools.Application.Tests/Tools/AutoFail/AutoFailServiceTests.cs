using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Core.Tools.AutoFail;
using Mapping_Tools.Core.Tools.AutoFail.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.AutoFail;

[TestClass]
public sealed class AutoFailServiceTests
{
    [TestMethod]
    public async Task AnalyzeAsync_WithAcceptedFixture_UsesLiveAwareGatewayAndPreservesCounts()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = new(
            new BeatmapEditingSession(CreateEditor(), BeatmapEditingSource.Disk, []));
        AutoFailService service = new(gateway);

        // Act
        var run = await service.AnalyzeAsync(new AutoFailOptions("accepted.osu"));

        // Assert
        gateway.OpenRequests.Single().Preference.Should().Be(LiveBeatmapPreference.PreferLive);
        run.Analysis.UnloadingObjects.Should().HaveCount(20);
        run.Analysis.PotentialUnloadingObjects.Should().HaveCount(63);
    }

    [TestMethod]
    public async Task ApplyFixAsync_WithProposedFix_SavesThroughBackupGateway()
    {
        // Arrange
        RecordingBeatmapEditingGateway gateway = new(
            new BeatmapEditingSession(CreateEditor(), BeatmapEditingSource.Disk, []));
        AutoFailService service = new(gateway);
        var run = await service.AnalyzeAsync(new AutoFailOptions("accepted.osu"));
        AutoFailFixPlan plan = new(
            Enumerable.Repeat(0, run.Analysis.PotentialUnloadingObjects.Count + 1).ToArray(),
            "No-op persistence-boundary probe");

        // Act
        await service.ApplyFixAsync(run, plan);

        // Assert
        gateway.SessionSaveRequests.Single().Session.Editor.Should().BeSameAs(gateway.Session!.Editor);
    }

    private static BeatmapEditor CreateEditor()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Beatmaps", "standard-autofail-2b.osu");
        return new BeatmapEditor(File.ReadAllLines(path).ToList(), new NoOpTextFileStore())
        {
            Path = "accepted.osu",
        };
    }

}
