using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.BeatmapHelper.IO.Decoding;
using Mapping_Tools.Core.BeatmapHelper.IO.Encoding;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.IO;

namespace Mapping_Tools.Core.Tests.BeatmapHelper;

[TestClass]
public class BeatmapParsingTests {
    [TestMethod]
    public void UnchangingEmptyMapCodeTest() {
        var path = Path.Join("Resources", "EmptyTestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new OsuBeatmapDecoder();
        var encoder = new OsuBeatmapEncoder();

        TestUnchanging(lines, decoder, encoder);
    }

    [TestMethod]
    public void UnchangingComplicatedMapCodeTest() {
        var path = Path.Join("Resources", "ComplicatedTestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new OsuBeatmapDecoder();
        var encoder = new OsuBeatmapEncoder();

        TestUnchanging(lines, decoder, encoder);
    }

    private static void TestUnchanging(string lines, IDecoder<Beatmap> decoder, IEncoder<Beatmap> encoder) {
        var map = decoder.Decode(lines);
        var lines2 = encoder.Encode(map);

        Debug.Print(lines);
        Debug.Print(lines2);

        // Split equal asserting to lines so we know where the difference is
        var linesSplit = lines.Split(Environment.NewLine);
        var lines2Split = lines2.Split(Environment.NewLine);

        for (int i = 0; i < linesSplit.Length; i++) {
            Assert.IsTrue(i < lines2Split.Length);
            Assert.AreEqual(linesSplit[i], lines2Split[i], $"Line equality fail at line {i+1}!");
        }
    }

    [TestMethod]
    public void ManiaParseTest() {
        var path = Path.Join("Resources", "ManiaTestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new OsuBeatmapDecoder();
        var encoder = new OsuBeatmapEncoder();

        TestUnchanging(lines, decoder, encoder);
    }

    [TestMethod]
    public void V9ParseTest() {
        var path = Path.Join("Resources", "V9TestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new OsuBeatmapDecoder();

        decoder.Decode(lines);
    }

    [TestMethod]
    public void V10TaikoParseTest() {
        var path = Path.Join("Resources", "V10TaikoTestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new OsuBeatmapDecoder();

        decoder.Decode(lines);
    }

    [TestMethod]
    public void V14ExtraWhiteSpaceParseTest() {
        var path = Path.Join("Resources", "MYTH & ROID - L.L.L. (jonathanlfj) [Raose's Hard].osu");
        var lines = File.ReadAllText(path);
        var decoder = new OsuBeatmapDecoder();

        decoder.Decode(lines);
    }

    [TestMethod]
    public void V9SbInSoundSamplesParseTest() {
        var path = Path.Join("Resources", "Jun.A - The Refrain of the Lovely Great War (KanbeKotori) [Alace's Taiko].osu");
        var lines = File.ReadAllText(path);
        var decoder = new OsuBeatmapDecoder();

        var map = decoder.Decode(lines);

        Assert.AreEqual(typeof(Sprite), map.Storyboard.StoryboardLayerForeground[0].GetType());
    }

    [TestMethod]
    public void V5ParseTest() {
        var path = Path.Join("Resources", "V5TestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new OsuBeatmapDecoder();

        decoder.Decode(lines);
    }

    [TestMethod]
    public void V10ParseTest() {
        var path = Path.Join("Resources", "V10TestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new OsuBeatmapDecoder();

        decoder.Decode(lines);
    }

    [TestMethod]
    public void MessedUpParseTest() {
        var path = Path.Join("Resources", "MessedUpTestMap.osu");
        var lines = File.ReadAllText(path);
        var decoder = new OsuBeatmapDecoder();

        decoder.Decode(lines);
    }
}