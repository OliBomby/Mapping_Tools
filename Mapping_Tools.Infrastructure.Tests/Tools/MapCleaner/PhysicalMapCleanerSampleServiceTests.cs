using Mapping_Tools.Core.BeatmapHelper.Serialization;
using Mapping_Tools.Infrastructure.Tools.MapCleaner;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Tools.MapCleaner;

[TestClass]
public sealed class PhysicalMapCleanerSampleServiceTests
{
    [DataTestMethod]
    [DataRow(-6, true)]
    [DataRow(-5, true)]
    [DataRow(0, true)]
    [DataRow(5, true)]
    [DataRow(6, true)]
    [DataRow(0, false)]
    public async Task MoveUnusedToRecoveryAsync_WithSampleChangeNearSliderRepeat_PreservesBodySamples(
        int offsetFromRepeat,
        bool resetBeforeTail)
    {
        // Arrange
        using TestDirectory directory = new();
        string reset = resetBeforeTail ? "1800,-100,4,1,1,100,0,0" : string.Empty;
        string text = $"""
            osu file format v14

            [General]
            AudioFilename: audio.mp3
            Mode: 0

            [Difficulty]
            SliderMultiplier: 1
            SliderTickRate: 2

            [TimingPoints]
            0,500,4,1,1,100,1,0
            {1500 + offsetFromRepeat},-100,4,2,2,100,0,0
            {reset}

            [HitObjects]
            64,96,1000,2,2,L|164:96,2,100,0|0|0,0:0|0:0|0:0,0:0:0:0:
            """;
        string mapPath = Path.Combine(directory.Root, "repeat.osu");
        File.WriteAllText(mapPath, text);
        string[] usedSamples = ["soft-sliderslide2.wav", "soft-sliderwhistle2.wav", "soft-slidertick2.wav"];
        foreach (string sample in usedSamples) File.WriteAllBytes(Path.Combine(directory.Root, sample), [1]);
        string unusedPath = Path.Combine(directory.Root, "normal-sliderslide9.wav");
        File.WriteAllBytes(unusedPath, [2]);
        BeatmapDecoder decoder = new();
        var beatmap = decoder.Decode(text);
        PhysicalMapCleanerSampleService service = new(decoder, new StoryboardDecoder());

        // Act
        int moved = await service.MoveUnusedToRecoveryAsync(directory.Root, mapPath, beatmap);

        // Assert
        moved.Should().Be(1);
        foreach (string sample in usedSamples)
            File.Exists(Path.Combine(directory.Root, sample)).Should().BeTrue($"{sample} plays after the repeat");
        File.Exists(unusedPath).Should().BeFalse();
        Directory.GetFiles(Path.Combine(directory.Root, ".mapping-tools-unused-samples"), "*.wav", SearchOption.AllDirectories)
            .Should().ContainSingle().Which.Should().EndWith("normal-sliderslide9.wav");
    }

    private sealed class TestDirectory : IDisposable
    {
        public TestDirectory()
        {
            Root = Path.Combine(Path.GetTempPath(), "MappingToolsMapCleaner", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
        }

        public string Root { get; }

        public void Dispose()
        {
            Directory.Delete(Root, true);
        }
    }
}
