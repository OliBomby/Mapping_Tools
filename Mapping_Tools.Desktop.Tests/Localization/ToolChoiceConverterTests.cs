using System.Globalization;
using Avalonia.Data;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;
using Mapping_Tools.Core.Tools.TimingCopier.Models;
using Mapping_Tools.Desktop.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Localization;

[TestClass]
public sealed class ToolChoiceConverterTests
{
    [TestMethod]
    public void Convert_CurrentBeatmapFetchingModes_ReturnsUserFacingLabels()
    {
        // Arrange
        ToolChoiceConverter converter = ToolChoiceConverter.Instance;

        // Act
        object[] labels =
        [
            converter.Convert(CurrentBeatmapFetchingMode.Disabled, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(CurrentBeatmapFetchingMode.MemoryRead, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(CurrentBeatmapFetchingMode.Mtipc, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(CurrentBeatmapFetchingMode.Gosumemory, typeof(string), null, CultureInfo.InvariantCulture),
        ];

        // Assert
        labels.Should().Equal(DesktopStrings.Shell_Disabled, DesktopStrings.Shell_MemoryRead, "MTIPC", "Gosumemory/Tosu");
    }

    [TestMethod]
    public void Convert_BeatmapLiveStateReadingModes_ReturnsUserFacingLabels()
    {
        // Arrange
        ToolChoiceConverter converter = ToolChoiceConverter.Instance;

        // Act
        object[] labels =
        [
            converter.Convert(BeatmapLiveStateReadingMode.Disabled, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(BeatmapLiveStateReadingMode.EditorReader, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(BeatmapLiveStateReadingMode.Mtipc, typeof(string), null, CultureInfo.InvariantCulture),
        ];

        // Assert
        labels.Should().Equal(DesktopStrings.Shell_Disabled, DesktopStrings.Shell_MemoryRead, "MTIPC");
    }

    [TestMethod]
    public void Convert_EditorReloadModes_ReturnsUserFacingLabels()
    {
        // Arrange
        ToolChoiceConverter converter = ToolChoiceConverter.Instance;

        // Act
        object[] labels =
        [
            converter.Convert(EditorReloadMode.Disabled, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(EditorReloadMode.SimulatedKeypress, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(EditorReloadMode.Mtipc, typeof(string), null, CultureInfo.InvariantCulture),
        ];

        // Assert
        labels.Should().Equal(DesktopStrings.Shell_Disabled, DesktopStrings.Shell_SimulatedKeypress, "MTIPC");
    }

    [TestMethod]
    public void Convert_TimingCopierResnapModes_ReturnsUserFacingLabels()
    {
        // Arrange
        ToolChoiceConverter converter = ToolChoiceConverter.Instance;

        // Act
        object[] labels = Enum.GetValues<TimingCopierResnapMode>()
            .Select(mode => converter.Convert(mode, typeof(string), null, CultureInfo.InvariantCulture))
            .ToArray();

        // Assert
        labels.Should().Equal(
            DesktopStrings.TimingCopier_ModeBeats,
            DesktopStrings.TimingCopier_ModeResnap,
            DesktopStrings.TimingCopier_ModeFixed);
    }

    [TestMethod]
    public void Convert_HitsoundStudioChoices_ReturnsUserFacingLabelsAndRawIdentifiers()
    {
        // Arrange
        ToolChoiceConverter converter = ToolChoiceConverter.Instance;

        // Act
        object[] labels =
        [
            converter.Convert(ImportType.None, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(HitsoundStudioExportMode.Standard, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(HitsoundStudioSampleExportFormat.Default, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(SampleSet.Normal, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(GameMode.Standard, typeof(string), null, CultureInfo.InvariantCulture),
        ];

        // Assert
        labels.Should().Equal(
            DesktopStrings.HitsoundStudio_ImportNone,
            DesktopStrings.HitsoundStudio_ExportStandard,
            DesktopStrings.HitsoundStudio_FormatDefault,
            "Normal",
            "Standard");
    }

    [TestMethod]
    public void Convert_CopyAndColourModes_ReturnsUserFacingLabels()
    {
        // Arrange
        ToolChoiceConverter converter = ToolChoiceConverter.Instance;

        // Act
        object[] labels =
        [
            converter.Convert(HitsoundCopierCopyMode.OverwriteEverything, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(HitsoundCopierCopyMode.OverwriteOnlyDefined, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(ColourPointMode.Normal, typeof(string), null, CultureInfo.InvariantCulture),
            converter.Convert(ColourPointMode.Burst, typeof(string), null, CultureInfo.InvariantCulture),
        ];

        // Assert
        labels.Should().Equal(
            DesktopStrings.HitsoundCopier_OverwriteEverything,
            DesktopStrings.HitsoundCopier_OverwriteDefined,
            DesktopStrings.ComboColourStudio_ModeNormal,
            DesktopStrings.ComboColourStudio_ModeBurst);
    }

    [TestMethod]
    public void Convert_QuickRunPlaceholderAndUnknownEnum_ReturnsLocalizedPlaceholderAndRawValue()
    {
        // Arrange
        ToolChoiceConverter converter = ToolChoiceConverter.Instance;

        // Act
        object placeholder = converter.Convert("<Current Tool>", typeof(string), null, CultureInfo.InvariantCulture);
        object rawProperNoun = converter.Convert("Auto", typeof(string), null, CultureInfo.InvariantCulture);
        object undefinedValue = converter.Convert((EditorReloadMode)99, typeof(string), null, CultureInfo.InvariantCulture);

        // Assert
        placeholder.Should().Be(DesktopStrings.Shell_CurrentTool);
        rawProperNoun.Should().Be("Auto");
        undefinedValue.Should().Be("99");
    }

    [TestMethod]
    public void ConvertBack_DisplayChoice_ReturnsDoNothing()
    {
        // Arrange
        ToolChoiceConverter converter = ToolChoiceConverter.Instance;

        // Act
        object result = converter.ConvertBack("translated label", typeof(EditorReloadMode), null, CultureInfo.InvariantCulture);

        // Assert
        result.Should().Be(BindingOperations.DoNothing);
    }
}
