using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tools.SliderMerger;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.SliderMerger;

[TestClass]
public sealed class SliderMergerFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task MergeAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using FixtureContext fixture = CreateFixture("slider-merger", fixtureName);
        SliderMergerServiceOptions project = fixture.ReadProject<SliderMergerServiceOptions>();
        SliderMergerService service = new(fixture.Gateway, new ApplicationSettings());

        // Act
        await service.MergeAsync(
            [fixture.TargetPath],
            project,
            cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Slider Merger");
        fixture.AssertTextOutput(actual);
    }
}
