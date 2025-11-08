using System.Diagnostics;
using Mapping_Tools.Domain.Beatmaps;
using Mapping_Tools.Domain.Beatmaps.Events;
using Mapping_Tools.Domain.Beatmaps.Parsing;
using Mapping_Tools.Domain.Beatmaps.Parsing.V14;

namespace Mapping_Tools.Domain.Tests.Beatmaps;

[TestFixture]
public class BeatmapParsingTests {
    [TestCase("EmptyTestMap.osu")]
    [TestCase("ComplicatedTestMap.osu")]
    [TestCase("ManiaTestMap.osu")]
    public void Beatmap_Encode_Decode_Remains_Unchanged(string mapName) {
        var path = Path.Join("Resources", mapName);
        var lines = File.ReadAllText(path);
        var decoder = new BeatmapDecoder();
        var encoder = new BeatmapEncoder();

        TestUnchanging(lines, decoder, encoder);
    }

    private static void TestUnchanging(string lines, IDecoder<Beatmap> decoder, IEncoder<Beatmap> encoder) {
        var map = decoder.Decode(lines);
        var lines2 = encoder.Encode(map);

        Console.WriteLine(lines);
        Console.WriteLine(lines2);

        // Split equal asserting to lines so we know where the difference is
        var linesSplit = lines.Split(Environment.NewLine);
        var lines2Split = lines2.Split(Environment.NewLine);

        for (int i = 0; i < linesSplit.Length; i++) {
            Assert.That(i, Is.LessThan(lines2Split.Length));
            Assert.That(lines2Split[i], Is.EqualTo(linesSplit[i]), $"Line equality fail at line {i+1}!\nExpected:\n{GetLinesArea(linesSplit, i)}\n\nActual:\n{GetLinesArea(lines2Split, i)}\n");
        }
    }

    private static string GetLinesArea(string[] lines, int middleIndex, int areaSize = 2) {
        int start = Math.Max(0, middleIndex - areaSize);
        int end = Math.Min(lines.Length - 1, middleIndex + areaSize);
        return string.Join(Environment.NewLine, lines[start..(end + 1)]);
    }

    [Test]
    public void V9ParseTest() {
        var path = Path.Join("Resources", "V9TestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new BeatmapDecoder();

        decoder.Decode(lines);
    }

    [Test]
    public void V10TaikoParseTest() {
        var path = Path.Join("Resources", "V10TaikoTestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new BeatmapDecoder();

        decoder.Decode(lines);
    }

    [Test]
    public void V14ExtraWhiteSpaceParseTest() {
        var path = Path.Join("Resources", "MYTH & ROID - L.L.L. (jonathanlfj) [Raose's Hard].osu");
        var lines = File.ReadAllText(path);
        var decoder = new BeatmapDecoder();

        decoder.Decode(lines);
    }

    [Test]
    public void V9SbInSoundSamplesParseTest() {
        var path = Path.Join("Resources", "Jun.A - The Refrain of the Lovely Great War (KanbeKotori) [Alace's Taiko].osu");
        var lines = File.ReadAllText(path);
        var decoder = new BeatmapDecoder();

        var map = decoder.Decode(lines);

        Assert.That(map.Storyboard.StoryboardLayerForeground, Has.ItemAt(0).TypeOf<Sprite>());
    }

    [Test]
    public void V5ParseTest() {
        var path = Path.Join("Resources", "V5TestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new BeatmapDecoder();

        decoder.Decode(lines);
    }

    [Test]
    public void V10ParseTest() {
        var path = Path.Join("Resources", "V10TestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new BeatmapDecoder();

        decoder.Decode(lines);
    }

    [Test]
    public void MessedUpParseTest() {
        var path = Path.Join("Resources", "MessedUpTestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new BeatmapDecoder();

        decoder.Decode(lines);
    }
}