using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.RhythmGuide;

[TestClass]
public sealed class RhythmGuideFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task GenerateAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using var fixture = CreateFixture("rhythm-guide", fixtureName);
        var project = fixture.ReadProject<RhythmGuideServiceOptions>();
        RhythmGuideService service = new(
            fixture.Gateway,
            new TestBeatmapBackupService(),
            new PhysicalBeatmapsetFileSystem(),
            new PhysicalBeatmapsetFileSystem());

        // Act
        var result = await service.GenerateAsync(
            project.GuideGeneratorArgs,
            CancellationToken.None);
        FixtureExecutionResult actual = new([result.ExportPath]);

        // Assert
        fixture.AssertAccepted("Rhythm Guide");
        fixture.AssertTextOutput(actual);
    }
}
