using Mapping_Tools.Application.Tools.MapCleaner;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.MapCleaner;

[TestClass]
public sealed class MapCleanerFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task CleanAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using FixtureContext fixture = CreateFixture("map-cleaner", fixtureName);
        MapCleanerServiceOptions project = fixture.ReadProject<MapCleanerServiceOptions>();
        MapCleanerService service = new(
            fixture.Gateway,
            new PhysicalBeatmapsetFileSystem(),
            new EmptyMapCleanerSampleService(),
            new ApplicationSettings());

        // Act
        await service.CleanAsync(
            [fixture.TargetPath],
            project.MapCleanerArgs,
            cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Map Cleaner");
        fixture.AssertTextOutput(actual);
    }

    private sealed class EmptyMapCleanerSampleService : IMapCleanerSampleService
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
}
