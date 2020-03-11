using System.IO;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.BeatmapHelper {
    [TestClass]
    public class BeatmapHelperTests {
        [TestMethod]
        public void UnchangingEmptyMapCodeTest() {
            var path = "Resources\\EmptyTestMap.osu";
            var lines = File.ReadAllLines(path).ToList();
            var editor = new BeatmapEditor(path);
            var lines2 = editor.Beatmap.GetLines();

            for (int i = 0; i < lines.Count; i++) {
                Assert.AreEqual(lines[i], lines2[i]);
            }
        }

        [TestMethod]
        public void UnchangingComplicatedMapCodeTest() {
            var path = "Resources\\ComplicatedTestMap.osu";
            var lines = File.ReadAllLines(path).ToList();
            var editor = new BeatmapEditor(path);
            var lines2 = editor.Beatmap.GetLines();

            for (int i = 0; i < lines.Count; i++) {
                Assert.AreEqual(lines[i], lines2[i]);
            }
        }
    }
}