using System.Globalization;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Desktop.Converters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Converters;

[TestClass]
public sealed class PreferenceModeConverterTests
{
    [TestMethod]
    public void Convert_CurrentBeatmapFetchingModes_ReturnsUserFacingLabels()
    {
        // Arrange
        CurrentBeatmapFetchingModeConverter converter = new();

        // Act
        object[] labels =
        [
            converter.Convert(CurrentBeatmapFetchingMode.Disabled, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(CurrentBeatmapFetchingMode.MemoryRead, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(CurrentBeatmapFetchingMode.Mtipc, typeof(string), null, CultureInfo.InvariantCulture),
        ];

        // Assert
        labels.Should().Equal("Disabled", "Memory read", "MTIPC");
    }

    [TestMethod]
    public void Convert_BeatmapLiveStateReadingModes_ReturnsUserFacingLabels()
    {
        // Arrange
        BeatmapLiveStateReadingModeConverter converter = new();

        // Act
        object[] labels =
        [
            converter.Convert(BeatmapLiveStateReadingMode.Disabled, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(BeatmapLiveStateReadingMode.EditorReader, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(BeatmapLiveStateReadingMode.Mtipc, typeof(string), null, CultureInfo.InvariantCulture),
        ];

        // Assert
        labels.Should().Equal("Disabled", "Memory read", "MTIPC");
    }

    [TestMethod]
    public void Convert_EditorReloadModes_ReturnsUserFacingLabels()
    {
        // Arrange
        EditorReloadModeConverter converter = new();

        // Act
        object[] labels =
        [
            converter.Convert(EditorReloadMode.Disabled, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(EditorReloadMode.SimulatedKeypress, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(EditorReloadMode.Mtipc, typeof(string), null, CultureInfo.InvariantCulture),
        ];

        // Assert
        labels.Should().Equal("Disabled", "Simulated keypress", "MTIPC");
    }
}
