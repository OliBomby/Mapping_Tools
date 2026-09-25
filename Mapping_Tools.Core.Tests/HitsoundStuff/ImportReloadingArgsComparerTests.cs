using Mapping_Tools.Core.HitsoundStuff;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.HitsoundStuff;

[TestClass]
public sealed class ImportReloadingArgsComparerTests
{
    private readonly ImportReloadingArgsComparer comparer = new();

    [TestMethod]
    public void Equals_WithNullAndSameReference_HandlesIdentity()
    {
        // Arrange
        ImportReloadingArgs args = Create(ImportType.Stack);

        // Act
        bool bothNull = comparer.Equals(null, null);
        bool sameReference = comparer.Equals(args, args);
        bool oneNull = comparer.Equals(args, null);

        // Assert
        bothNull.Should().BeTrue();
        sameReference.Should().BeTrue();
        oneNull.Should().BeFalse();
    }

    [TestMethod]
    public void Equals_WithDifferentImportTypes_RejectsSamePath()
    {
        // Arrange
        ImportReloadingArgs stack = Create(ImportType.Stack);
        ImportReloadingArgs hitsounds = Create(ImportType.Hitsounds);

        // Act
        bool equal = comparer.Equals(stack, hitsounds);

        // Assert
        equal.Should().BeFalse();
    }

    [TestMethod]
    public void Equals_WithStackImport_UsesPathAndBothCoordinatesOnly()
    {
        // Arrange
        ImportReloadingArgs original = Create(ImportType.Stack);
        ImportReloadingArgs ignoredSettings = Create(ImportType.Stack, length: 99, offset: 99);
        ImportReloadingArgs otherX = Create(ImportType.Stack, x: 99);
        ImportReloadingArgs otherY = Create(ImportType.Stack, y: 99);
        ImportReloadingArgs otherPath = Create(ImportType.Stack, path: "other.osu");

        // Act
        bool equal = comparer.Equals(original, ignoredSettings);
        bool xEqual = comparer.Equals(original, otherX);
        bool yEqual = comparer.Equals(original, otherY);
        bool pathEqual = comparer.Equals(original, otherPath);

        // Assert
        equal.Should().BeTrue();
        comparer.GetHashCode(original).Should().Be(comparer.GetHashCode(ignoredSettings));
        xEqual.Should().BeFalse();
        yEqual.Should().BeFalse();
        pathEqual.Should().BeFalse();
    }

    [TestMethod]
    public void Equals_WithHitsoundImport_UsesPathOnly()
    {
        // Arrange
        ImportReloadingArgs original = Create(ImportType.Hitsounds);
        ImportReloadingArgs changedSettings = Create(ImportType.Hitsounds, x: 99, y: 99, length: 99, velocity: 99, offset: 99);
        ImportReloadingArgs otherPath = Create(ImportType.Hitsounds, path: "other.osu");

        // Act
        bool equal = comparer.Equals(original, changedSettings);
        bool otherEqual = comparer.Equals(original, otherPath);

        // Assert
        equal.Should().BeTrue();
        comparer.GetHashCode(original).Should().Be(comparer.GetHashCode(changedSettings));
        otherEqual.Should().BeFalse();
    }

    [TestMethod]
    public void Equals_WithMidiImport_UsesPathRoughnessAndOffset()
    {
        // Arrange
        ImportReloadingArgs original = Create(ImportType.MIDI);
        ImportReloadingArgs ignoredCoordinates = Create(ImportType.MIDI, x: 99, y: 99);
        ImportReloadingArgs otherLength = Create(ImportType.MIDI, length: 99);
        ImportReloadingArgs otherVelocity = Create(ImportType.MIDI, velocity: 99);
        ImportReloadingArgs otherOffset = Create(ImportType.MIDI, offset: 99);

        // Act
        bool equal = comparer.Equals(original, ignoredCoordinates);
        bool lengthEqual = comparer.Equals(original, otherLength);
        bool velocityEqual = comparer.Equals(original, otherVelocity);
        bool offsetEqual = comparer.Equals(original, otherOffset);

        // Assert
        equal.Should().BeTrue();
        comparer.GetHashCode(original).Should().Be(comparer.GetHashCode(ignoredCoordinates));
        lengthEqual.Should().BeFalse();
        velocityEqual.Should().BeFalse();
        offsetEqual.Should().BeFalse();
    }

    [TestMethod]
    public void Equals_WithNoneImport_IgnoresAllSettings()
    {
        // Arrange
        ImportReloadingArgs original = Create(ImportType.None);
        ImportReloadingArgs changed = Create(ImportType.None, path: "other.osu", x: 99, y: 99, length: 99, velocity: 99, offset: 99);

        // Act
        bool equal = comparer.Equals(original, changed);

        // Assert
        equal.Should().BeTrue();
        comparer.GetHashCode(original).Should().Be(comparer.GetHashCode(changed));
    }

    private static ImportReloadingArgs Create(
        ImportType type, string path = "map.osu", double x = 1, double y = 2,
        double length = 3, double velocity = 4, double offset = 5)
    {
        return new ImportReloadingArgs(type, path, x, y, length, velocity, false, false, false, offset);
    }
}
