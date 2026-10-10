using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tools.SliderCompletionator;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.SliderCompletionator;

[TestClass]
public sealed class SliderCompletionatorFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task CompleteAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using var fixture = CreateFixture("slider-completionator", fixtureName);
        var project = fixture.ReadProject<SliderCompletionatorServiceOptions>();
        SliderCompletionatorService service = new(fixture.Gateway, new ApplicationSettings());

        // Act
        await service.CompleteAsync(
            [fixture.TargetPath],
            project,
            cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Slider Completionator");
        fixture.AssertTextOutput(actual);
    }
}
