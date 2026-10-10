using Mapping_Tools.Application.Tests.Execution;
using Mapping_Tools.Application.Tools.TimingCopier;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Tools.TimingCopier;

[TestClass]
public sealed class TimingCopierFixtureTests : TransformationFixtureTestBase
{
    [DataTestMethod]
    [DataRow("000")]
    public async Task CopyAsync_AcceptedFixture_ProducesEquivalentOutput(string fixtureName)
    {
        // Arrange
        using var fixture = CreateFixture("timing-copier", fixtureName);
        var project = fixture.ReadProject<TimingCopierServiceOptions>();
        TimingCopierService service = new(fixture.Gateway);

        // Act
        await service.CopyAsync(project, cancellationToken: CancellationToken.None);
        FixtureExecutionResult actual = new([fixture.TargetPath]);

        // Assert
        fixture.AssertAccepted("Timing Copier");
        fixture.AssertTextOutput(actual);
    }
}
