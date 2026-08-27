using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.Tools.HitsoundCopier;
using Mapping_Tools.Core.Tools.HitsoundCopier.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

[TestClass]
public sealed class HitsoundCopierEngineTests
{
    [TestMethod]
    public void Apply_OverwriteMode_CopiesAllSourceObjectHitsounds()
    {
        // Arrange
        var source = LoadFixture();
        Beatmap target = new(source.GetLines());
        HitsoundCopierEngineOptions options = new()
        {
            CopyMode = HitsoundCopierCopyMode.OverwriteEverything,
        };

        // Act
        var result = HitsoundCopierEngine.Apply(
            target,
            source,
            options,
            @"C:\maps");

        // Assert
        result.MatchedHitsoundCount.Should().BeGreaterThan(0);
        target.HitObjects.Select(item => item.Hitsounds)
            .Should().BeEquivalentTo(source.HitObjects.Select(item => item.Hitsounds));
    }

    [TestMethod]
    public void Validate_WithUndefinedCopyMode_ThrowsArgumentException()
    {
        // Arrange
        HitsoundCopierEngineOptions options = new()
        {
            CopyMode = (HitsoundCopierCopyMode)2,
        };

        // Act
        Action act = () => HitsoundCopierEngine.Validate(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Apply_StoryboardSamples_PreservesSourceTiming()
    {
        // Arrange
        var source = LoadStoryboardFixture();
        source.StoryboardSoundSamples.Clear();
        source.StoryboardSoundSamples.Add(new StoryboardSoundSample(100, StoryboardLayer.Foreground, "sample.wav", 80));
        Beatmap target = new(source.GetLines());
        HitsoundCopierEngineOptions options = new()
        {
            CopyHitsounds = false,
            CopyStoryboardedSamples = true,
            IgnoreHitsoundSatisfiedSamples = false,
        };

        // Act
        HitsoundCopierEngine.Apply(
            target,
            source,
            options,
            @"C:\maps");

        // Assert
        target.StoryboardSoundSamples.Should().ContainSingle();
        target.StoryboardSoundSamples[0].StartTime.Should().Be(100);
    }

    private static Beatmap LoadFixture()
    {
        return new Beatmap(
            File.ReadAllLines(Path.Combine(
                AppContext.BaseDirectory,
                "Resources",
                "ComplicatedTestMap.osu")).ToList());
    }

    private static Beatmap LoadStoryboardFixture()
    {
        return new Beatmap(
            File.ReadAllLines(Path.Combine(
                AppContext.BaseDirectory,
                "Resources",
                "EmptyTestMap.osu")).ToList());
    }
}
