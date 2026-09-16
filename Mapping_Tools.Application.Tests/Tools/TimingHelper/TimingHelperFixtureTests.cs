using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tools.TimingHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.TimingHelper;

[TestClass]
public sealed class TimingHelperFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task AdjustAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using FixtureContext fixture = CreateFixture("timing-helper", fixtureName);
        TimingHelperServiceOptions project = fixture.ReadProject<TimingHelperServiceOptions>();
        TimingHelperService service = new(fixture.Gateway, new ApplicationSettings());

        // Act
        await service.AdjustAsync(
            [fixture.TargetPath],
            project,
            cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Timing Helper");
        fixture.AssertTextOutput(actual);
    }
}
