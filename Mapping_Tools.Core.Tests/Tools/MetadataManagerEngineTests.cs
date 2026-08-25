using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Tools.MetadataManager;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Tools;

[TestClass]
public sealed class MetadataManagerEngineTests
{
    [TestMethod]
    public void Apply_WithConfiguredMetadata_PreservesMapContentAndWritesMetadata()
    {
        // Arrange
        var beatmap = Load("standard-feature-rich.osu");
        int timingPointCount = beatmap.BeatmapTiming.TimingPoints.Count;
        int hitObjectCount = beatmap.HitObjects.Count;
        MetadataManagerEngineOptions options = new()
        {
            Artist = "Wave Zero Artist",
            RomanisedArtist = "Wave Zero Artist",
            Title = "Wave Zero Metadata Baseline",
            RomanisedTitle = "Wave Zero Metadata Baseline",
            BeatmapCreator = "Fixture Mapper",
            Source = "Wave 0",
            Tags = "wave zero metadata fixture wave",
            PreviewTime = 12345,
            ResetIds = true,
            ComboColours = [new ComboColour(RgbaColour.FromRgb(255, 51, 102))],
        };

        // Act
        MetadataManagerEngine.Apply(beatmap, options);

        // Assert
        beatmap.Metadata["ArtistUnicode"].Value.Should().Be("Wave Zero Artist");
        beatmap.Metadata["Title"].Value.Should().Be("Wave Zero Metadata Baseline");
        beatmap.Metadata["Creator"].Value.Should().Be("Fixture Mapper");
        beatmap.Metadata["Source"].Value.Should().Be("Wave 0");
        beatmap.Metadata["Tags"].Value.Should().Be("wave zero metadata fixture");
        beatmap.General["PreviewTime"].DoubleValue.Should().Be(12345);
        beatmap.Metadata["BeatmapID"].Value.Should().Be("0");
        beatmap.Metadata["BeatmapSetID"].Value.Should().Be("-1");
        beatmap.ComboColours.Should().ContainSingle();
        beatmap.ComboColours[0].Color.Should().Be(RgbaColour.FromRgb(255, 51, 102));
        beatmap.BeatmapTiming.TimingPoints.Should().HaveCount(timingPointCount);
        beatmap.HitObjects.Should().HaveCount(hitObjectCount);
    }

    [TestMethod]
    public void Read_WithBeatmap_ReturnsIndependentMetadataAndColours()
    {
        // Arrange
        var beatmap = Load("standard-feature-rich.osu");

        // Act
        var options = MetadataManagerEngine.Read(beatmap);
        options.ComboColours[0].Color = RgbaColour.FromRgb(1, 2, 3);

        // Assert
        options.Artist.Should().Be(beatmap.Metadata["ArtistUnicode"].Value);
        options.RomanisedTitle.Should().Be(beatmap.Metadata["Title"].Value);
        options.PreviewTime.Should().Be(beatmap.General["PreviewTime"].DoubleValue);
        beatmap.ComboColours[0].Color.Should().NotBe(RgbaColour.FromRgb(1, 2, 3));
    }

    private static Beatmap Load(string fileName)
    {
        return new Beatmap(
            File.ReadAllLines(Path.Combine(AppContext.BaseDirectory, "Resources", fileName)).ToList());
    }
}
