using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.Tools.MapsetMerger;
using Mapping_Tools.Core.Tools.MapsetMerger.Models;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools.MapsetMerger;

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
    public void ResolveDuplicateDifficultyName_WithExistingVersion_UsesLegacyPrefixedName()
    {
        // Arrange
        HashSet<string> used = ["Normal"];

        // Act
        string result = MapsetMergerEngine.ResolveDuplicateDifficultyName("Normal", "Pack - ", used);

        // Assert
        result.Should().Be("Pack - Normal");
    }

    [DataRow(".")]
    [DataRow("..")]
    [DataRow("nested/name")]
    [DataRow(" ")]
    [TestMethod]
    public void Validate_WithUnsafeMapsetName_ThrowsArgumentException(string name)
    {
        // Arrange
        MapsetMergerInput[] inputs = [new(name, "source")];

        // Act
        Action act = () => MapsetMergerEngine.Validate(inputs);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [DataRow("../audio.mp3")]
    [DataRow("sub/../audio.mp3")]
    [DataRow("/absolute/audio.mp3")]
    [TestMethod]
    public void CombineReference_WithAbsoluteOrParentPath_RejectsReference(string reference)
    {
        // Arrange
        const string mapset_name = "Pack";

        // Act
        Action act = () => MapsetMergerEngine.CombineReference(mapset_name, reference);

        // Assert
        act.Should().Throw<InvalidDataException>();
    }

    [TestMethod]
    public void RewriteBeatmapReferences_WithRepeatedCustomIndices_ReusesMapping()
    {
        // Arrange
        Beatmap beatmap = new(File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Resources", "standard-feature-rich.osu")).ToList());
        beatmap.HitObjects.RemoveRange(3, beatmap.HitObjects.Count - 3);
        beatmap.BeatmapTiming.SetTimingPoints([beatmap.BeatmapTiming.TimingPoints[0]]);
        beatmap.HitObjects[0].CustomIndex = 7;
        beatmap.HitObjects[1].CustomIndex = 7;
        beatmap.HitObjects[2].CustomIndex = 9;
        beatmap.BeatmapTiming.TimingPoints[0].SampleIndex = 9;
        int nextSampleIndex = 3;
        Dictionary<int, int> mappings = [];
        string originalAudio = beatmap.General["AudioFilename"].Value.Trim();

        // Act
        MapsetMergerReferences references = MapsetMergerEngine.RewriteBeatmapReferences(
            beatmap, "Pack", ref nextSampleIndex, mappings);

        // Assert
        mappings.Should().ContainKey(7).WhoseValue.Should().Be(3);
        mappings.Should().ContainKey(9).WhoseValue.Should().Be(4);
        nextSampleIndex.Should().Be(5);
        beatmap.HitObjects.Take(3).Select(hitObject => hitObject.CustomIndex).Should().Equal(3, 3, 4);
        beatmap.BeatmapTiming.TimingPoints[0].SampleIndex.Should().Be(4);
        beatmap.General["AudioFilename"].Value.Trim().Should().Be(Path.Combine("Pack", originalAudio));
        references.OtherAudioFiles.Should().Contain(originalAudio);
    }

    [DataRow("normal-hitnormal2.wav", 1, "normal-hitnormal.wav")]
    [DataRow("drum-hitclap2.wav", 5, "drum-hitclap5.wav")]
    [DataRow("custom-sample.wav", 5, "custom-sample.wav")]
    [TestMethod]
    public void GetRemappedHitsoundFilename_WithSampleName_PreservesLegacyNaming(
        string filename, int mappedIndex, string expected)
    {
        // Arrange
        // Act
        string result = MapsetMergerEngine.GetRemappedHitsoundFilename(filename, mappedIndex);

        // Assert
        result.Should().Be(expected);
    }

    [TestMethod]
    public void RewriteStoryboardReferences_WithMixedAssets_CollectsAndPrefixesEachAssetType()
    {
        // Arrange
        StoryBoard storyboard = new();
        Background background = new() { Filename = "background.jpg" };
        Video video = new() { Filename = "video.mp4" };
        Sprite sprite = new() { FilePath = "sprite.png" };
        Animation animation = new() { FilePath = Path.Combine("frames", "anim.png"), FrameCount = 2 };
        StoryboardSoundSample sample = new(1000, default, "effect.wav", 80);
        storyboard.BackgroundAndVideoEvents.Add(background);
        storyboard.BackgroundAndVideoEvents.Add(video);
        storyboard.StoryboardLayerForeground.Add(sprite);
        storyboard.StoryboardLayerForeground.Add(animation);
        storyboard.StoryboardSoundSamples.Add(sample);

        // Act
        MapsetMergerReferences references = MapsetMergerEngine.RewriteStoryboardReferences(storyboard, "Pack");

        // Assert
        references.ImageFiles.Should().Contain("background.jpg");
        references.ImageFiles.Should().Contain("sprite.png");
        references.ImageFiles.Should().Contain(Path.Combine("frames", "anim0"));
        references.ImageFiles.Should().Contain(Path.Combine("frames", "anim1"));
        references.VideoFiles.Should().Contain("video.mp4");
        references.OtherAudioFiles.Should().Contain("effect.wav");
        background.Filename.Should().Be(Path.Combine("Pack", "background.jpg"));
        video.Filename.Should().Be(Path.Combine("Pack", "video.mp4"));
        sprite.FilePath.Should().Be(Path.Combine("Pack", "sprite.png"));
        animation.FilePath.Should().Be(Path.Combine("Pack", "frames", "anim.png"));
        sample.FilePath.Should().Be(Path.Combine("Pack", "effect.wav"));
    }
}
