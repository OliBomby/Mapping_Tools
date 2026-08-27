using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tools.MapsetMerger;
using Mapping_Tools.Application.Tools.MapsetMerger.Models;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.MapsetMerger;

[TestClass]
public sealed class MapsetMergerServiceTests : IDisposable
{
    private readonly DisposableFixture fixture = new();

    public void Dispose()
    {
        fixture.Dispose();
    }

    [TestMethod]
    public async Task MergeAsync_WithDuplicateNamesAndReferences_CommitsResolvedExport()
    {
        // Arrange
        string first = fixture.CreateMapset("first");
        string second = fixture.CreateMapset("second");
        string exportPath = Path.Combine(fixture.Root, "export");
        MapsetMergerServiceOptions project = new()
        {
            ExportPath = exportPath,
            Mapsets =
            [
                new MapsetMergerServiceOptions.MapsetItem { Name = "Pack", Path = first },
                new MapsetMergerServiceOptions.MapsetItem { Name = "Pack", Path = second },
            ],
        };
        MapsetMergerService service = new(
            new FixtureEditingGateway(),
            new PhysicalBeatmapsetFileSystem());

        // Act
        var result = await service.MergeAsync(project);

        // Assert
        result.MapsetsMerged.Should().Be(2);
        result.BeatmapsWritten.Should().Be(2);
        Directory.GetFiles(exportPath, "*.osu").Should().HaveCount(2);
        Directory.GetFiles(exportPath, "*.osb").Should().HaveCount(2);
        File.ReadAllText(Directory.GetFiles(exportPath, "*.osu").Single(path =>
                File.ReadAllText(path).Contains("Pack\\audio.mp3", StringComparison.Ordinal)))
            .Should().Contain("Pack\\audio.mp3");
        Directory.GetFiles(exportPath, "soft-hitfinish*.wav").Should().HaveCount(2);
        Directory.GetFiles(exportPath, "background.jpg", SearchOption.AllDirectories).Should().HaveCount(2);
        Directory.GetFiles(exportPath, "sb.wav", SearchOption.AllDirectories).Should().HaveCount(2);
    }

    [TestMethod]
    public async Task MergeAsync_WhenCancelledBeforeProcessing_LeavesExistingExportUntouched()
    {
        // Arrange
        string source = fixture.CreateMapset("cancelled");
        string exportPath = Path.Combine(fixture.Root, "export");
        Directory.CreateDirectory(exportPath);
        string existing = Path.Combine(exportPath, "keep.txt");
        File.WriteAllText(existing, "keep");
        MapsetMergerServiceOptions project = new()
        {
            ExportPath = exportPath,
            Mapsets = [new MapsetMergerServiceOptions.MapsetItem { Name = "Cancelled", Path = source }],
        };
        MapsetMergerService service = new(
            new FixtureEditingGateway(),
            new PhysicalBeatmapsetFileSystem());
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Func<Task> act = () => service.MergeAsync(project, cancellationToken: cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        File.ReadAllText(existing).Should().Be("keep");
        Directory.GetFiles(exportPath, "*", SearchOption.AllDirectories)
            .Should().ContainSingle()
            .Which.Should().Be(existing);
    }

    [TestMethod]
    public async Task MergeAsync_WithNestedStoryboardAssets_RewritesAndCopiesRelativePaths()
    {
        // Arrange
        string source = fixture.CreateMapset("nested");
        string exportPath = Path.Combine(fixture.Root, "export");
        MapsetMergerServiceOptions project = new()
        {
            ExportPath = exportPath,
            Mapsets = [new MapsetMergerServiceOptions.MapsetItem { Name = "Nested", Path = source }],
        };
        MapsetMergerService service = new(
            new FixtureEditingGateway(),
            new PhysicalBeatmapsetFileSystem());

        // Act
        await service.MergeAsync(project);

        // Assert
        string beatmapPath = Directory.GetFiles(exportPath, "*.osu").Single();
        string storyboardPath = Directory.GetFiles(exportPath, "*.osb").Single();
        string nestedReference(string filename) => Path.Combine("Nested", $"nested/{filename}");
        File.ReadAllText(beatmapPath).Should().Contain(nestedReference("background.jpg"));
        File.ReadAllText(storyboardPath).Should().Contain(nestedReference("story.png"));
        File.ReadAllText(storyboardPath).Should().Contain(nestedReference("sb.wav"));
        File.ReadAllText(storyboardPath).Should().Contain(nestedReference("video.mp4"));
        File.Exists(Path.Combine(exportPath, "Nested", "nested", "anim0.png")).Should().BeTrue();
        File.Exists(Path.Combine(exportPath, "Nested", "nested", "anim1.png")).Should().BeTrue();
        File.Exists(Path.Combine(exportPath, "Nested", "nested", "background.jpg")).Should().BeTrue();
        File.Exists(Path.Combine(exportPath, "Nested", "nested", "story.png")).Should().BeTrue();
        File.Exists(Path.Combine(exportPath, "Nested", "nested", "sb.wav")).Should().BeTrue();
        File.Exists(Path.Combine(exportPath, "Nested", "nested", "video.mp4")).Should().BeTrue();
    }

    [TestMethod]
    public async Task MergeAsync_WhenExportIsInsideSource_RejectsMutationBeforeTransaction()
    {
        // Arrange
        string source = fixture.CreateMapset("overlap");
        string exportPath = Path.Combine(source, "export");
        MapsetMergerServiceOptions project = new()
        {
            ExportPath = exportPath,
            Mapsets = [new MapsetMergerServiceOptions.MapsetItem { Name = "Overlap", Path = source }],
        };
        MapsetMergerService service = new(
            new FixtureEditingGateway(),
            new PhysicalBeatmapsetFileSystem());

        // Act
        Func<Task> act = () => service.MergeAsync(project);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*must not be the same as or inside*");
        Directory.Exists(exportPath).Should().BeFalse();
    }

    [TestMethod]
    public async Task MergeAsync_WhenMoveStoryboardToBeatmapIsEnabled_EmbedsFirstStoryboardWithoutOsbOutput()
    {
        // Arrange
        string source = fixture.CreateMapset("embedded");
        string exportPath = Path.Combine(fixture.Root, "export");
        MapsetMergerServiceOptions project = new()
        {
            ExportPath = exportPath,
            MoveSbToBeatmap = true,
            Mapsets = [new MapsetMergerServiceOptions.MapsetItem { Name = "Embedded", Path = source }],
        };
        MapsetMergerService service = new(
            new FixtureEditingGateway(),
            new PhysicalBeatmapsetFileSystem());

        // Act
        var result = await service.MergeAsync(project);

        // Assert
        result.StoryboardsWritten.Should().Be(0);
        Directory.GetFiles(exportPath, "*.osb").Should().BeEmpty();
        File.ReadAllText(Directory.GetFiles(exportPath, "*.osu").Single())
            .Should().Contain("Embedded\\nested/story.png");
    }

    [TestCleanup]
    public void Cleanup()
    {
        Dispose();
    }

    private sealed class FixtureEditingGateway : IBeatmapEditingGateway
    {
        private static readonly PhysicalBeatmapsetFileSystem files = new();

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            BeatmapEditor editor = new(path, files);
            return Task.FromResult(new BeatmapEditingSession(
                editor,
                BeatmapEditingSource.Disk,
                []));
        }

        public Task<StoryboardEditor> OpenStoryboardAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new StoryboardEditor(path, files));
        }

        public Task SaveAsync(
            Editor editor,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task SaveAsync(
            BeatmapEditingSession session,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class DisposableFixture : IDisposable
    {
        public DisposableFixture()
        {
            Root = Path.Combine(Path.GetTempPath(), "mapping-tools-mapset-merger-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }

        public string CreateMapset(string name)
        {
            string path = Path.Combine(Root, name);
            Directory.CreateDirectory(path);
            File.WriteAllText(Path.Combine(path, "audio.mp3"), "audio");
            string nested = Path.Combine(path, "nested");
            Directory.CreateDirectory(nested);
            File.WriteAllText(Path.Combine(nested, "background.jpg"), "image");
            File.WriteAllText(Path.Combine(nested, "story.png"), "story-image");
            File.WriteAllText(Path.Combine(nested, "sb.wav"), "story-audio");
            File.WriteAllText(Path.Combine(nested, "video.mp4"), "story-video");
            File.WriteAllText(Path.Combine(nested, "anim0.png"), "animation-frame-0");
            File.WriteAllText(Path.Combine(nested, "anim1.png"), "animation-frame-1");
            File.WriteAllText(Path.Combine(path, "soft-hitfinish.wav"), "hit");
            File.WriteAllText(Path.Combine(path, "map.osu"), CreateBeatmap());
            File.WriteAllText(Path.Combine(path, "story.osb"), CreateStoryboard());
            return path;
        }

        private static string CreateBeatmap()
        {
            return """
                   osu file format v14

                   [General]
                   AudioFilename: audio.mp3
                   Mode: 0
                   StackLeniency: 0.7

                   [Editor]

                   [Metadata]
                   Title:Test
                   Artist:Artist
                   Creator:Mapper
                   Version:Normal
                   BeatmapID:1
                   BeatmapSetID:2

                   [Difficulty]
                   HPDrainRate:5
                   CircleSize:4
                   OverallDifficulty:5
                   ApproachRate:5
                   SliderMultiplier:1.4
                   SliderTickRate:1

                   [Events]
                   //Background and Video events
                   0,0,"nested/background.jpg"
                   //Storyboard Layer 0 (Background)
                   //Storyboard Layer 1 (Fail)
                   //Storyboard Layer 2 (Pass)
                   //Storyboard Layer 3 (Foreground)
                   //Storyboard Layer 4 (Overlay)
                   //Storyboard Sound Samples

                   [TimingPoints]
                   0,500,4,2,1,50,1,0

                   [HitObjects]
                   64,192,0,1,4,2:0:1:1:
                   """;
        }

        private static string CreateStoryboard()
        {
            return """
                   [Events]
                   //Background and Video events
                   //Storyboard Layer 0 (Background)
                   Sprite,Background,Centre,"nested/story.png",0,0
                   //Storyboard Layer 1 (Fail)
                   //Storyboard Layer 2 (Pass)
                   //Storyboard Layer 3 (Foreground)
                   //Storyboard Layer 4 (Overlay)
                   //Storyboard Sound Samples
                   Sample,0,Background,"nested/sb.wav",100
                   Video,0,"nested/video.mp4"
                   Animation,Background,Centre,"nested/anim.png",0,0,2,100,LoopForever
                   """;
        }
    }
}
