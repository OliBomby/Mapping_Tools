using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.HitsoundCopier;

[TestClass]
public sealed class HitsoundCopierServiceTests
{
    [TestMethod]
    public async Task CopyAsync_WithMultipleTargets_UsesSourceAndSavesEveryTarget()
    {
        // Arrange
        string fixture = Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "Beatmaps", "standard-feature-rich.osu");
        RecordingBeatmapEditingGateway gateway = CreateGateway(fixture);
        HitsoundCopierService service = new(gateway, new StubSampleService(), new ApplicationSettings());
        HitsoundCopierServiceOptions options = new()
        {
            PathFrom = "source.osu",
            PathTo = "first.osu|second.osu",
        };

        // Act
        var result = await service.CopyAsync(options);

        // Assert
        result.ProcessedPaths.Should().Equal("first.osu", "second.osu");
        gateway.OpenRequests.Select(request => request.Path)
            .Should().Equal("source.osu", "first.osu", "second.osu");
        gateway.SessionSaveRequests.Select(request => request.Session.Editor.Path)
            .Should().Equal("first.osu", "second.osu");
    }

    private sealed class StubSampleService : IHitsoundSampleService
    {
        public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
            string directory,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        public HitsoundSampleAssignment? TryCreateAssignment(
            string directory,
            IReadOnlyList<string> sourceFilenames,
            IReadOnlyDictionary<string, string> firstSamples,
            string role,
            SampleSet sampleSet,
            int startIndex,
            SampleSchema existingSchema)
        {
            return null;
        }

        public Task ExportAsync(SampleSchema schema, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private static RecordingBeatmapEditingGateway CreateGateway(string fixture)
    {
        return new RecordingBeatmapEditingGateway
        {
            OpenBeatmapFactory = (path, _) =>
            {
                BeatmapEditor editor = new(
                    File.ReadAllLines(fixture).ToList(),
                    new NoOpTextFileStore { ReadResult = [] })
                {
                    Path = path,
                };
                return new BeatmapEditingSession(editor, BeatmapEditingSource.Disk, []);
            },
        };
    }

}
