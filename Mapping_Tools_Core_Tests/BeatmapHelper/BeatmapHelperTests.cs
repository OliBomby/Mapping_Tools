using Mapping_Tools_Core.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools_Core.BeatmapHelper.Decoding;

namespace Mapping_Tools_Core_Tests.BeatmapHelper {
    [TestClass]
    public class BeatmapHelperTests {
        [TestMethod]
        public void UnchangingEmptyMapCodeTest() {
            var path = "Resources\\EmptyTestMap.osu";
            var lines = File.ReadAllLines(path).ToList();
            var parser = new OsuBeatmapDecoder();

            TestUnchanging(lines, parser);
        }

        [TestMethod]
        public void UnchangingComplicatedMapCodeTest() {
            var path = "Resources\\ComplicatedTestMap.osu";
            var lines = File.ReadAllLines(path).ToList();
            var parser = new OsuBeatmapDecoder();

            TestUnchanging(lines, parser);
        }

        private static void TestUnchanging(IReadOnlyList<string> lines, IParser<Beatmap> parser) {
            var lines2 = parser.Serialize(parser.ParseNew(lines)).ToList();

            for (int i = 0; i < lines.Count; i++) {
                Assert.AreEqual(lines[i], lines2[i]);
            }
        }
    }
}