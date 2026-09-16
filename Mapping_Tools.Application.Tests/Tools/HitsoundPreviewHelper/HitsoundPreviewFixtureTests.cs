using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.HitsoundPreviewHelper;

[TestClass]
public sealed class HitsoundPreviewFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task ApplyAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using FixtureContext fixture = CreateFixture("hitsound-preview", fixtureName);
        HitsoundPreviewHelperServiceOptions project = fixture.ReadProject<HitsoundPreviewHelperServiceOptions>();
        HitsoundPreviewHelperService service = new(fixture.Gateway, new ApplicationSettings());

        // Act
        await service.ApplyAsync(
            [fixture.TargetPath],
            project,
            cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Hitsound Preview Helper");
        fixture.AssertTextOutput(actual);
    }
}
