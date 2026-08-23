using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.AutoFail;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Core.Tools.AutoFail;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.AutoFail;

[TestClass]
public sealed class AutoFailServiceTests
{
    [TestMethod]
    public async Task AnalyzeAsync_WithAcceptedFixture_UsesLiveAwareGatewayAndPreservesCounts()
    {
        // Arrange
        RecordingGateway gateway = new(CreateEditor());
        AutoFailService service = new(gateway);

        // Act
        var run = await service.AnalyzeAsync(new AutoFailOptions("accepted.osu"));

        // Assert
        gateway.OpenPreference.Should().Be(LiveBeatmapPreference.PreferLive);
        run.Analysis.UnloadingObjects.Should().HaveCount(20);
        run.Analysis.PotentialUnloadingObjects.Should().HaveCount(63);
    }

    [TestMethod]
    public async Task ApplyFixAsync_WithProposedFix_SavesThroughBackupGateway()
    {
        // Arrange
        RecordingGateway gateway = new(CreateEditor());
        AutoFailService service = new(gateway);
        var run = await service.AnalyzeAsync(new AutoFailOptions("accepted.osu"));
        AutoFailFixPlan plan = new(
            Enumerable.Repeat(0, run.Analysis.PotentialUnloadingObjects.Count + 1).ToArray(),
            "No-op persistence-boundary probe");

        // Act
        await service.ApplyFixAsync(run, plan);

        // Assert
        gateway.SavedEditor.Should().BeSameAs(gateway.Session.Editor);
    }

    private static BeatmapEditor2 CreateEditor()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Beatmaps", "standard-autofail-2b.osu");
        return new BeatmapEditor2(File.ReadAllLines(path).ToList(), new MemoryStore())
        {
            Path = "accepted.osu",
        };
    }

    private sealed class RecordingGateway : IBeatmapEditingGateway
    {
        public RecordingGateway(BeatmapEditor2 editor)
        {
            Session = new BeatmapEditingSession(
                editor,
                BeatmapEditingSource.Disk,
                []);
        }

        public BeatmapEditingSession Session { get; }
        public LiveBeatmapPreference? OpenPreference { get; private set; }
        public Editor2? SavedEditor { get; private set; }

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            OpenPreference = livePreference;
            return Task.FromResult(Session);
        }

        public Task<StoryboardEditor2> OpenStoryboardAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveAsync(
            Editor2 editor,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            SavedEditor = editor;
            return Task.CompletedTask;
        }

        public Task SaveAsync(
            BeatmapEditingSession session,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            return SaveAsync(session.Editor, reloadEditor, cancellationToken);
        }
    }

    private sealed class MemoryStore : ITextFileStore
    {
        public IReadOnlyList<string> ReadAllLines(string path)
        {
            throw new NotSupportedException();
        }

        public void WriteAllLines(string path, IEnumerable<string> lines) { }
        public void Delete(string path) { }

        public string GetParentFolder(string path)
        {
            return string.Empty;
        }

        public string CombinePath(string parent, string child)
        {
            return child;
        }
    }
}
