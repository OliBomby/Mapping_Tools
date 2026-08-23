using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Core.Tools.HitsoundPreviewHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

[TestClass]
public sealed class HitsoundPreviewHelperEngineTests
{
    [TestMethod]
    public void Apply_WithNearestZones_UpdatesOriginHitsoundsAndSampleFields()
    {
        // Arrange
        HitObject first = new("64,96,1000,1,0,0:0:0:0:");
        HitObject second = new("400,96,2000,1,0,0:0:0:0:");
        Beatmap beatmap = new(
            new List<HitObject> { first, second },
            [],
            globalSv: 1.4);
        List<HitsoundZone> zones =
        [
            new("first", "kick.wav", 64, 96, Hitsound.Clap, SampleSet.Drum, SampleSet.Normal, 3),
            new("second", "snare.wav", 400, 96, Hitsound.Finish, SampleSet.Normal, SampleSet.Drum, 4),
        ];

        // Act
        int updated = HitsoundPreviewHelperEngine.Apply(
            beatmap,
            beatmap.HitObjects,
            zones);

        // Assert
        updated.Should().Be(2);
        first.Hitsounds.Should().Be(8);
        first.Filename.Should().Be("kick.wav");
        first.SampleSet.Should().Be(SampleSet.Drum);
        first.AdditionSet.Should().Be(SampleSet.Normal);
        first.CustomIndex.Should().Be(3);
        second.Hitsounds.Should().Be(4);
        second.Filename.Should().Be("snare.wav");
    }

    [TestMethod]
    public void Apply_WithSubsetOfObjects_LeavesOtherObjectsUnchanged()
    {
        // Arrange
        HitObject selected = new("64,96,1000,1,0,0:0:0:0:");
        HitObject unselected = new("400,96,2000,1,0,0:0:0:0:");
        Beatmap beatmap = new(
            new List<HitObject> { selected, unselected },
            [],
            globalSv: 1.4);
        HitsoundZone zone = new(
            "selected", "selected.wav", 64, 96,
            Hitsound.Whistle, SampleSet.Normal, SampleSet.Drum, 2);

        // Act
        int updated = HitsoundPreviewHelperEngine.Apply(
            beatmap,
            [selected],
            [zone]);

        // Assert
        updated.Should().Be(1);
        selected.Hitsounds.Should().Be(2);
        unselected.Hitsounds.Should().Be(0);
        unselected.Filename.Should().BeEmpty();
    }

    [TestMethod]
    public void Apply_WithoutZones_ThrowsValidationException()
    {
        // Arrange
        Beatmap beatmap = new(
            new List<HitObject> { new("64,96,1000,1,0,0:0:0:0:") },
            [],
            globalSv: 1.4);

        // Act
        Action act = () => HitsoundPreviewHelperEngine.Apply(
            beatmap,
            beatmap.HitObjects,
            []);

        // Assert
        act.Should().Throw<ArgumentException>()
            .Which.Message.Should().Contain("There are no zones!");
    }

    [TestMethod]
    public void Apply_WithDuplicateSelectionAndEqualDistance_PreservesInputOrderAndUpdatesOnce()
    {
        // Arrange
        HitObject selected = new("256,96,1000,1,0,0:0:0:0:");
        Beatmap beatmap = new(
            new List<HitObject> { selected },
            [],
            globalSv: 1.4);
        HitsoundZone first = new(
            "first", "first.wav", 128, 96,
            Hitsound.Clap, SampleSet.Normal, SampleSet.Drum, 1);
        HitsoundZone second = new(
            "second", "second.wav", 384, 96,
            Hitsound.Finish, SampleSet.Drum, SampleSet.Normal, 2);

        // Act
        int updated = HitsoundPreviewHelperEngine.Apply(
            beatmap,
            [selected, selected],
            [first, second]);

        // Assert
        updated.Should().Be(1);
        selected.Hitsounds.Should().Be(8);
        selected.Filename.Should().Be("first.wav");
        selected.CustomIndex.Should().Be(1);
    }

    [TestMethod]
    public void Apply_WithSliderSpinnerAndHold_PreservesObjectSpecificSampleSemantics()
    {
        // Arrange
        HitObject slider = new(
            "64,96,1000,2,0,B|164:96,1,100,0|0,0:0:0:0:");
        HitObject spinner = new(
            "256,192,2000,8,0,3000,0:0:0:0:");
        HitObject hold = new(
            "256,192,4000,128,0,5000:0:0:0:0:");
        Beatmap beatmap = new(
            new List<HitObject> { slider, spinner, hold },
            [],
            globalSv: 1.4);
        HitsoundZone zone = new(
            "custom", "custom.wav", 64, 96,
            Hitsound.Finish, SampleSet.Drum, SampleSet.Normal, 5);

        // Act
        int updated = HitsoundPreviewHelperEngine.Apply(
            beatmap,
            beatmap.HitObjects,
            [zone]);

        // Assert
        updated.Should().BeGreaterThan(0);
        slider.EdgeHitsounds.Should().OnlyContain(hitsound => hitsound == 4);
        slider.Filename.Should().BeEmpty();
        slider.CustomIndex.Should().Be(0);
        spinner.Hitsounds.Should().Be(4);
        spinner.Filename.Should().Be("custom.wav");
        spinner.CustomIndex.Should().Be(5);
        hold.Hitsounds.Should().Be(4);
        hold.Filename.Should().Be("custom.wav");
        hold.CustomIndex.Should().Be(5);
    }

    [TestMethod]
    public void Apply_WhenCancelledBeforeMutation_LeavesBeatmapUnchanged()
    {
        // Arrange
        HitObject selected = new("64,96,1000,1,0,0:0:0:0:");
        Beatmap beatmap = new(
            new List<HitObject> { selected },
            [],
            globalSv: 1.4);
        HitsoundZone zone = new("zone", "zone.wav", 64, 96, Hitsound.Clap,
            SampleSet.Normal, SampleSet.None, 1);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Action act = () => HitsoundPreviewHelperEngine.Apply(
            beatmap,
            [selected],
            [zone],
            cancellationToken: cancellation.Token);

        // Assert
        act.Should().Throw<OperationCanceledException>();
        selected.Hitsounds.Should().Be(0);
        selected.Filename.Should().BeEmpty();
    }
}
