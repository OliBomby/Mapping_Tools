using System.Collections.Generic;
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

            CompareLines(lines, lines2);
        }

        [TestMethod]
        public void UnchangingComplicatedMapCodeTest() {
            var path = "Resources\\ComplicatedTestMap.osu";
            var lines = File.ReadAllLines(path).ToList();
            var editor = new BeatmapEditor(path);
            var lines2 = editor.Beatmap.GetLines();

            CompareLines(lines, lines2);
        }

        [TestMethod]
        public void UnchangingLazerMapCodeTest() {
            var path = "Resources\\THE ORAL CIGARETTES - GET BACK (Nikakis) [Sotarks_ Cataclysm].osu";
            var lines = File.ReadAllLines(path).ToList();
            var editor = new BeatmapEditor(path);
            var lines2 = editor.Beatmap.GetLines();

            CompareLines(lines, lines2);
        }

        [TestMethod]
        public void UnchangingStoryboardedMapCodeTest() {
            var path = "Resources\\Camellia - Body F10ating in the Zero Gravity Space (Orange_) [Nonsubmersible].osu";
            var lines = File.ReadAllLines(path).ToList();
            var editor = new BeatmapEditor(path);
            var lines2 = editor.Beatmap.GetLines();

            CompareLines(lines, lines2);
        }

        [TestMethod]
        public void UnchangingStoryboardCodeTest() {
            var path = "Resources\\TestStoryboard.osb";
            var lines = File.ReadAllLines(path).ToList();
            var editor = new StoryboardEditor(path);
            var lines2 = editor.StoryBoard.GetLines();

            CompareLines(lines, lines2);
        }

        private static void CompareLines(IReadOnlyList<string> expected, IReadOnlyList<string> actual) {
            var expectedGeneralLines = FileFormatHelper.GetCategoryLines(expected, "[General]").ToArray();
            var expectedEditorLines = FileFormatHelper.GetCategoryLines(expected, "[Editor]").ToArray();
            var expectedMetadataLines = FileFormatHelper.GetCategoryLines(expected, "[Metadata]").ToArray();
            var expectedDifficultyLines = FileFormatHelper.GetCategoryLines(expected, "[Difficulty]").ToArray();
            var expectedEventLines = FileFormatHelper.GetCategoryLines(expected, "[Events]").ToArray();
            var expectedTimingLines = FileFormatHelper.GetCategoryLines(expected, "[TimingPoints]").ToArray();
            var expectedColourLines = FileFormatHelper.GetCategoryLines(expected, "[Colours]").ToArray();
            var expectedHitobjectLines = FileFormatHelper.GetCategoryLines(expected, "[HitObjects]").ToArray();

            var actualGeneralLines = FileFormatHelper.GetCategoryLines(actual, "[General]").ToArray();
            var actualEditorLines = FileFormatHelper.GetCategoryLines(actual, "[Editor]").ToArray();
            var actualMetadataLines = FileFormatHelper.GetCategoryLines(actual, "[Metadata]").ToArray();
            var actualDifficultyLines = FileFormatHelper.GetCategoryLines(actual, "[Difficulty]").ToArray();
            var actualEventLines = FileFormatHelper.GetCategoryLines(actual, "[Events]").ToArray();
            var actualTimingLines = FileFormatHelper.GetCategoryLines(actual, "[TimingPoints]").ToArray();
            var actualColourLines = FileFormatHelper.GetCategoryLines(actual, "[Colours]").ToArray();
            var actualHitobjectLines = FileFormatHelper.GetCategoryLines(actual, "[HitObjects]").ToArray();

            CollectionAssert.AreEquivalent(expectedGeneralLines, actualGeneralLines);
            CollectionAssert.AreEquivalent(expectedEditorLines, actualEditorLines);
            CollectionAssert.AreEquivalent(expectedMetadataLines, actualMetadataLines);
            CollectionAssert.AreEquivalent(expectedDifficultyLines, actualDifficultyLines);
            AssertCollectionEqual(expectedEventLines, actualEventLines);
            AssertCollectionEqual(expectedTimingLines, actualTimingLines);
            AssertCollectionEqual(expectedColourLines, actualColourLines);
            AssertCollectionEqual(expectedHitobjectLines, actualHitobjectLines);
        }

        private static void AssertCollectionEqual(IReadOnlyList<string> expected, IReadOnlyList<string> actual) {
            Assert.AreEqual(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++) {
                Assert.AreEqual(expected[i], actual[i], $"At line {i} expected \"{expected[i]}\" but got \"{actual[i]}\".");
            }
        }
    }
}