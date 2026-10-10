using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tests.TestDoubles;
using Mapping_Tools.Application.Tools.MetadataManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.MetadataManager;

[TestClass]
public sealed class MetadataManagerFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task ExportAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using var fixture = CreateFixture("metadata-manager", fixtureName);
        var project = fixture.ReadProject<MetadataManagerServiceOptions>();
        MetadataManagerService service = new(fixture.Gateway, new TestBeatmapBackupService());

        // Act
        var result = await service.ExportAsync(project, cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new(result.ProcessedPaths);

        // Assert
        fixture.AssertAccepted("Metadata Manager");
        fixture.AssertTextOutput(actual);
    }
}
