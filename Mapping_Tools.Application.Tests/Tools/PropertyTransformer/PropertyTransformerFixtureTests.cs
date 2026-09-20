using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tools.PropertyTransformer;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.PropertyTransformer;

[TestClass]
public sealed class PropertyTransformerFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task TransformAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using var fixture = CreateFixture("property-transformer", fixtureName);
        var project = fixture.ReadProject<PropertyTransformerServiceOptions>();
        PropertyTransformerService service = new(
            fixture.Gateway,
            new ApplicationSettings());

        // Act
        await service.TransformAsync(
            [fixture.TargetPath],
            project,
            cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Property Transformer");
        fixture.AssertTextOutput(actual);
    }
}
