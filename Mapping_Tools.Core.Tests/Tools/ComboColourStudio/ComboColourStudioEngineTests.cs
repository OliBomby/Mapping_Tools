using FluentAssertions;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.MathUtil;
using Mapping_Tools.Core.Tools.ComboColourStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.ComboColourStudio;

[TestClass]
public sealed class ComboColourStudioEngineTests
{
    [TestMethod]
    public void AddColourPoint_AfterPaletteEdit_UpdatesSequenceReference()
    {
        // Arrange
        ComboColourProject project = new();
        project.AddComboColour();
        project.AddComboColour();
        ColourPoint point = project.AddColourPoint(
            100,
            [project.ComboColours[0], project.ComboColours[1]],
            ColourPointMode.Normal);

        // Act
        project.ComboColours[0].Color = RgbaColour.FromRgb(1, 2, 3);

        // Assert
        point.ColourSequence[0].Color.Should().Be(RgbaColour.FromRgb(1, 2, 3));
        point.ColourSequence.Should().HaveCount(2);
    }

    [TestMethod]
    public void Apply_WithSequencePoint_SetsPaletteAndLegacyComboSkipBits()
    {
        // Arrange
        ComboColourProject project = new();
        project.AddComboColour();
        project.AddComboColour();
        project.ComboColours[0].Color = RgbaColour.FromRgb(10, 20, 30);
        project.ComboColours[1].Color = RgbaColour.FromRgb(40, 50, 60);
        project.AddColourPoint(0, [project.ComboColours[0], project.ComboColours[1]]);

        Beatmap beatmap = new();
        beatmap.HitObjects.Add(CreateCircle(0, true, 0));
        beatmap.HitObjects.Add(CreateCircle(100, true, 1));

        // Act
        ComboColourStudioEngine.Apply(beatmap, project);

        // Assert
        beatmap.ComboColours.Select(colour => colour.Color).Should().Equal(
            RgbaColour.FromRgb(10, 20, 30),
            RgbaColour.FromRgb(40, 50, 60));
        beatmap.HitObjects.Select(hitObject => hitObject.ComboSkip).Should().Equal(1, 0);
    }

    [TestMethod]
    public void Apply_WithReorderedPalette_UsesCollectionOrderForComboIndices()
    {
        // Arrange
        ComboColourProject project = new();
        project.AddComboColour();
        project.AddComboColour();
        SpecialColour firstColour = project.ComboColours[0];
        SpecialColour movedColour = project.ComboColours[0];
        project.ComboColours.RemoveAt(0);
        project.ComboColours.Insert(1, movedColour);
        project.AddColourPoint(0, [firstColour]);

        Beatmap beatmap = new();
        beatmap.HitObjects.Add(CreateCircle(0, true, 0));

        // Act
        ComboColourStudioEngine.Apply(beatmap, project);

        // Assert
        beatmap.ComboColours[1].Color.Should().Be(firstColour.Color);
        beatmap.HitObjects[0].ComboSkip.Should().Be(0);
    }

    [TestMethod]
    public void Apply_WithTransparentPaletteColour_PreservesAlphaWhenSerialized()
    {
        // Arrange
        ComboColourProject project = new();
        project.AddComboColour();
        project.ComboColours[0].Color = RgbaColour.FromArgb(128, 10, 20, 30);

        Beatmap beatmap = new();

        // Act
        ComboColourStudioEngine.Apply(beatmap, project);

        // Assert
        beatmap.ComboColours.Single().ToString().Should().Be("10,20,30,128");
    }

    [TestMethod]
    public void ValidateForExport_WithMissingPaletteReference_ReportsValidationError()
    {
        // Arrange
        ComboColourProject project = new();
        project.AddComboColour();
        project.AddColourPoint(5, [new SpecialColour(RgbaColour.White, "Combo2")]);

        // Act
        IReadOnlyList<string> errors = project.ValidateForExport();

        // Assert
        errors.Should().ContainSingle()
            .Which.Should().Contain("missing colour");
    }

    [TestMethod]
    public void ValidateForExport_WithDuplicatePaletteNames_ReportsValidationError()
    {
        // Arrange
        ComboColourProject project = new();
        project.AddComboColour();
        project.AddComboColour();
        project.ComboColours[1].Name = project.ComboColours[0].Name;

        // Act
        IReadOnlyList<string> errors = project.ValidateForExport();

        // Assert
        errors.Should().ContainSingle().Which.Should().Contain("unique");
    }

    [TestMethod]
    public void IsSubSequence_WithPrefixAndNonPrefixInputs_ReturnsExpectedMatches()
    {
        // Arrange
        int[] prefix = [1, 2];
        int[] nonPrefix = [1, 3];

        // Act
        bool prefixResult = ComboColourStudioEngine.IsSubSequence(prefix, [1, 2, 3]);
        bool nonPrefixResult = ComboColourStudioEngine.IsSubSequence(nonPrefix, [1, 2, 3]);

        // Assert
        prefixResult.Should().BeTrue();
        nonPrefixResult.Should().BeFalse();
    }

    private static HitObject CreateCircle(double time, bool newCombo, int colourIndex)
    {
        HitObject hitObject = new(
            new Vector2(0, 0),
            time,
            HitObjectType.Circle,
            newCombo,
            0,
            normal: true,
            whistle: false,
            finish: false,
            clap: false,
            SampleSet.None,
            SampleSet.None,
            index: 0,
            volume: 0,
            filename: string.Empty);
        hitObject.ActualNewCombo = newCombo;
        hitObject.ColourIndex = colourIndex;
        return hitObject;
    }
}
