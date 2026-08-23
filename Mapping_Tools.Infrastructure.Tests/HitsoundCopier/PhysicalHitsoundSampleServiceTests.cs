using Mapping_Tools.Application.MapCleaner;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.HitsoundCopier;

[TestClass]
public sealed class PhysicalHitsoundSampleServiceTests
{
    [TestMethod]
    public void TryCreateAssignment_WithNestedSourcePathAndExistingSchema_PreservesPathAndAllocatesNextIndex()
    {
        // Arrange
        using TestDirectory directory = new();
        string firstPath = Path.Combine(directory.Root, "samples", "kick.wav");
        string secondPath = Path.Combine(directory.Root, "samples", "snare.wav");
        Directory.CreateDirectory(Path.GetDirectoryName(firstPath)!);
        File.WriteAllBytes(firstPath, [1]);
        File.WriteAllBytes(secondPath, [2]);
        PhysicalHitsoundSampleService service = new(new StubMapCleanerSampleService());
        SampleSchema schema = new();
        IReadOnlyDictionary<string, string> firstSamples = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(directory.Root, "samples", "kick")] = firstPath,
            [Path.Combine(directory.Root, "samples", "snare")] = secondPath,
        };

        // Act
        var first = service.TryCreateAssignment(
            directory.Root,
            ["samples/kick.wav"],
            firstSamples,
            "slidertick",
            SampleSet.Normal,
            100,
            schema);
        var second = service.TryCreateAssignment(
            directory.Root,
            ["samples/snare.wav"],
            firstSamples,
            "slidertick",
            SampleSet.Normal,
            100,
            schema);

        // Assert
        first.Should().NotBeNull();
        second.Should().NotBeNull();
        first!.Index.Should().Be(100);
        second!.Index.Should().Be(101);
        schema.Should().ContainKey("normal-slidertick100");
        schema.Should().ContainKey("normal-slidertick101");
        schema["normal-slidertick100"].Should().ContainSingle().Which.Path.Should().Be(firstPath);
        schema["normal-slidertick101"].Should().ContainSingle().Which.Path.Should().Be(secondPath);
    }

    private sealed class StubMapCleanerSampleService : IMapCleanerSampleService
    {
        public Task<IReadOnlyDictionary<string, string>> AnalyzeAsync(
            string directory,
            bool detectDuplicates,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyDictionary<string, string>>(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        }

        public Task<int> MoveUnusedToRecoveryAsync(
            string directory,
            string currentBeatmapPath,
            Beatmap currentBeatmap,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "MappingToolsHitsoundCopier", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }
}
