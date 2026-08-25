using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.Backups.Models;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.MetadataManager;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.MetadataManager;

[TestClass]
public sealed class MetadataManagerServiceTests
{
    [TestMethod]
    public async Task ExportAsync_WithMetadataConfiguration_BackupsRenamesAndWritesTarget()
    {
        // Arrange
        using TemporaryDirectory directory = new();
        string fixture = Path.Combine(
            AppContext.BaseDirectory,
            "Fixtures",
            "Beatmaps",
            "standard-feature-rich.osu");
        string target = Path.Combine(directory.Path, "original.osu");
        File.Copy(fixture, target);
        FileSystemFileStore fileStore = new();
        TestBeatmapBackupService backup = new();
        MetadataManagerService service = new(
            new TestEditingGateway(fileStore),
            backup);
        MetadataManagerServiceOptions options = new()
        {
            ExportPath = target,
            Artist = "Wave Zero Artist",
            RomanisedArtist = "Wave Zero Artist",
            Title = "Wave Zero Metadata Baseline",
            RomanisedTitle = "Wave Zero Metadata Baseline",
            BeatmapCreator = "Fixture Mapper",
            Source = "Wave 0",
            Tags = "wave zero wave",
            ResetIds = true,
            PreviewTime = 12345,
        };

        // Act
        var result = await service.ExportAsync(options);

        // Assert
        result.ProcessedPaths.Should().ContainSingle();
        result.ProcessedPaths[0].Should().NotBe(target);
        File.Exists(result.ProcessedPaths[0]).Should().BeTrue();
        File.Exists(target).Should().BeFalse();
        backup.CreateRequests.Should().ContainSingle(request =>
            request.Paths.SequenceEqual(new[] { target }) && request.Reason == BeatmapBackupReason.Automatic && !request.Force);
        Beatmap output = new(File.ReadAllLines(result.ProcessedPaths[0]).ToList());
        output.Metadata["Artist"].Value.Should().Be("Wave Zero Artist");
        output.Metadata["Tags"].Value.Should().Be("wave zero");
        output.Metadata["BeatmapID"].Value.Should().Be("0");
        output.General["PreviewTime"].DoubleValue.Should().Be(12345);
    }

    private sealed class TestEditingGateway : IBeatmapEditingGateway
    {
        private readonly ITextFileStore fileStore;

        public TestEditingGateway(ITextFileStore fileStore)
        {
            this.fileStore = fileStore;
        }

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(
                new BeatmapEditingSession(
                    new BeatmapEditor(path, fileStore),
                    BeatmapEditingSource.Disk,
                    []));
        }

        public Task<StoryboardEditor> OpenStoryboardAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
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

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MappingToolsMetadataManagerTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, true);
        }
    }
}
