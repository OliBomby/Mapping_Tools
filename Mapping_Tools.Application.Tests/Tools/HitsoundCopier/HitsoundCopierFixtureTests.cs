using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tools.HitsoundCopier;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.HitsoundCopier;

[TestClass]
public sealed class HitsoundCopierFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task CopyAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using var fixture = CreateFixture("hitsound-copier", fixtureName);
        var project = fixture.ReadProject<HitsoundCopierServiceOptions>();
        HitsoundCopierService service = new(
            fixture.Gateway,
            new EmptyHitsoundSampleService(),
            new ApplicationDirectories(Path.GetTempPath()),
            new NoopFileRevealService(),
            new ApplicationSettings());

        // Act
        await service.CopyAsync(project, cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Hitsound Copier");
        fixture.AssertTextOutput(actual);
    }

    private sealed class EmptyHitsoundSampleService : IHitsoundSampleService
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

        public Task<int> ExportAsync(SampleSchema schema, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class NoopFileRevealService : IFileRevealService
    {
        public Task<bool> RevealAsync(string path, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
