using Mapping_Tools.Core.Tools.MapsetMerger;
using Mapping_Tools.Core.Tools.MapsetMerger.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

[TestClass]
public sealed class MapsetMergerEngineTests
{
    [TestMethod]
    public void ResolveDuplicateMapsetNames_WithRepeatedNames_AppendsAvailableSuffixes()
    {
        // Arrange
        List<MapsetMergerInput> inputs =
        [
            new("Pack", "one"),
            new("Pack", "two"),
            new("Pack", "three"),
            new("Pack1", "four"),
        ];

        // Act
        MapsetMergerEngine.ResolveDuplicateMapsetNames(inputs);

        // Assert
        inputs.Select(input => input.Name).Should().Equal("Pack", "Pack1", "Pack2", "Pack11");
    }

    [TestMethod]
    public void ResolveDuplicateDifficultyName_WithExistingVersion_UsesUniquePrefixedName()
    {
        // Arrange
        HashSet<string> used = new(StringComparer.OrdinalIgnoreCase) { "Normal", "Pack - Normal" };

        // Act
        string result = MapsetMergerEngine.ResolveDuplicateDifficultyName("Normal", "Pack - ", used);

        // Assert
        result.Should().Be("Pack - Normal1");
    }
}
