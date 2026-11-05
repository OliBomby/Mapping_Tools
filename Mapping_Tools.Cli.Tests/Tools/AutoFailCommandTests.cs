using AwesomeAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Cli.Tests.Tools;

[TestClass]
public sealed class AutoFailCommandTests
{
    [TestMethod]
    public async Task RunAsync_DetectedAutoFail_ReturnsSuccessUnlessFailureIsExplicitlyRequested()
    {
        // Arrange
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "standard-autofail-2b.osu");
        string original = await File.ReadAllTextAsync(path);

        // Act
        var normal = await CliRunnerTests.RunAsync("auto-fail-detector", path, "--editor-reader", "disabled");
        var requestedFailure = await CliRunnerTests.RunAsync("auto-fail-detector", path, "--editor-reader", "disabled", "--fail-on-auto-fail");

        // Assert
        normal.Code.Should().Be(0, normal.Error);
        normal.Output.Should().Contain("Auto-fail detected");
        requestedFailure.Code.Should().Be(3, requestedFailure.Error);
        (await File.ReadAllTextAsync(path)).Should().Be(original);
    }
}
