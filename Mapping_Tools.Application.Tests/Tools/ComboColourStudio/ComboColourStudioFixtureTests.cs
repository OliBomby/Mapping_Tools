using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tools.ComboColourStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.ComboColourStudio;

[TestClass]
public sealed class ComboColourStudioFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task ApplyAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using var fixture = CreateFixture("combo-colour-studio", fixtureName);
        var project = fixture.ReadProject<ComboColourServiceOptions>();
        ComboColourStudioService service = new(fixture.Gateway, new ApplicationSettings());

        // Act
        await service.ApplyAsync(
            [fixture.TargetPath],
            project,
            cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Combo Colour Studio");
        fixture.AssertTextOutput(actual);
    }
}
