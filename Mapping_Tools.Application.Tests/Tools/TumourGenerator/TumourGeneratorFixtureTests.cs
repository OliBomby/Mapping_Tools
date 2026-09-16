using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tools.TumourGenerator;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.TumourGenerator;

[TestClass]
public sealed class TumourGeneratorFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task RunAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using FixtureContext fixture = CreateFixture("tumour-generator", fixtureName);
        TumourGeneratorServiceOptions project = fixture.ReadProject<TumourGeneratorServiceOptions>();
        project.TumourLayers = project.TumourLayers
            .Take(fixture.IntProperty("LayerCount"))
            .ToList();
        TumourGeneratorService service = new(fixture.Gateway, new ApplicationSettings());

        // Act
        await service.RunAsync(
            [fixture.TargetPath],
            project,
            false,
            cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Tumour Generator 2");
        fixture.AssertTextOutput(actual);
    }
}
