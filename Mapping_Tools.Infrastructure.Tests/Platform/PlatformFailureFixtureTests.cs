using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Platform;

[TestClass]
public sealed class PlatformFailureFixtureTests
{
    [TestMethod]
    public void LoadPlatformFailureScenariosFixture_ContainsUniqueScenarioIdsAndEffects()
    {
        // Arrange
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "PlatformFailures", "scenarios.json");

        // Act
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var scenarios = document.RootElement.GetProperty("scenarios").EnumerateArray().ToArray();

        // Assert
        document.RootElement.GetProperty("schemaVersion").GetInt32().Should().Be(1);
        scenarios.Should().HaveCount(8);
        scenarios.Select(scenario => scenario.GetProperty("id").GetString())
            .Should().OnlyHaveUniqueItems()
            .And.OnlyContain(id => !string.IsNullOrWhiteSpace(id));
        scenarios.Select(scenario => scenario.GetProperty("effect").GetString())
            .Should().OnlyContain(effect => !string.IsNullOrWhiteSpace(effect));
    }
}
