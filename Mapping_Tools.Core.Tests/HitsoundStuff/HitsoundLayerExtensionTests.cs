using Mapping_Tools.Core.HitsoundStuff;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.HitsoundStuff;

[TestClass]
public sealed class HitsoundLayerExtensionTests
{
    [TestMethod]
    public void AllToStringOrDefault_NullProjectedValues_ReturnsEmptyText()
    {
        // Arrange
        List<string?> selection = [null, null];

        // Act
        string result = selection.AllToStringOrDefault(value => value);

        // Assert
        result.Should().BeEmpty();
    }

    [TestMethod]
    public void AllToStringOrDefault_MixedNullProjectedValues_DoesNotInvokeConverter()
    {
        // Arrange
        List<string?> selection = [null, "sample"];
        bool converted = false;

        // Act
        string result = selection.AllToStringOrDefault(value => value, value =>
        {
            converted = true;
            return value ?? "none";
        });

        // Assert
        result.Should().BeEmpty();
        converted.Should().BeFalse();
    }
}
