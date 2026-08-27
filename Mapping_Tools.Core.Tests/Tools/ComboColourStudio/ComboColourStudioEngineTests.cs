using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Core.Tools.ComboColourStudio;
using Mapping_Tools.Core.Tools.ComboColourStudio.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.ComboColourStudio;

[TestClass]
public sealed class ComboColourStudioEngineTests
{
    [TestMethod]
    public void AddColourPoint_AfterPaletteEdit_UpdatesSequenceReference()
    {
        // Arrange
        ComboColourEngineOptions project = new();
        project.AddComboColour();
        project.AddComboColour();
        var point = project.AddColourPoint(
            100,
            [project.ComboColours[0], project.ComboColours[1]]);

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
        ComboColourEngineOptions project = new();
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
        ComboColourEngineOptions project = new();
        project.AddComboColour();
        project.AddComboColour();
        var firstColour = project.ComboColours[0];
        var movedColour = project.ComboColours[0];
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
        ComboColourEngineOptions project = new();
        project.AddComboColour();
        project.ComboColours[0].Color = RgbaColour.FromArgb(128, 10, 20, 30);

        Beatmap beatmap = new();

        // Act
        ComboColourStudioEngine.Apply(beatmap, project);

        // Assert
        beatmap.ComboColours.Single().ToString().Should().Be("10,20,30,128");
    }

    [TestMethod]
    public void Validate_WithMissingPaletteReference_ThrowsValidationException()
    {
        // Arrange
        ComboColourEngineOptions project = new();
        project.AddComboColour();
        project.AddColourPoint(5, [new SpecialColour(RgbaColour.White, "Combo2")]);

        // Act
        Action act = () => ComboColourStudioEngine.Validate(project);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("missing colour");
    }

    [TestMethod]
    public void Validate_WithDuplicatePaletteNames_ThrowsValidationException()
    {
        // Arrange
        ComboColourEngineOptions project = new();
        project.AddComboColour();
        project.AddComboColour();
        project.ComboColours[1].Name = project.ComboColours[0].Name;

        // Act
        Action act = () => ComboColourStudioEngine.Validate(project);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("unique");
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
            true,
            false,
            false,
            false,
            SampleSet.None,
            SampleSet.None,
            0,
            0,
            string.Empty);
        hitObject.ActualNewCombo = newCombo;
        hitObject.ColourIndex = colourIndex;
        return hitObject;
    }
}
