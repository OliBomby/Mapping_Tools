using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using NUnit.Framework;

namespace Mapping_Tools_Tests.Classes.BeatmapHelper {
    [TestFixture]
    public class BeatmapHelperTests {
        [Test]
        public void UnchangingEmptyMapCodeTest() {
            var path = "Resources\\EmptyTestMap.osu";
            var lines = File.ReadAllLines(path).ToList();
            var editor = new BeatmapEditor(path);
            var lines2 = editor.Beatmap.GetLines();

            CompareLines(lines, lines2);
        }

        [Test]
        public void UnchangingComplicatedMapCodeTest() {
            var path = "Resources\\ComplicatedTestMap.osu";
            var lines = File.ReadAllLines(path).ToList();
            var editor = new BeatmapEditor(path);
            var lines2 = editor.Beatmap.GetLines();

            CompareLines(lines, lines2);
        }

        [Test]
        public void UnchangingLazerMapCodeTest() {
            var path = "Resources\\THE ORAL CIGARETTES - GET BACK (Nikakis) [Sotarks_ Cataclysm].osu";
            var lines = File.ReadAllLines(path).ToList();
            var editor = new BeatmapEditor(path);
            var lines2 = editor.Beatmap.GetLines();

            CompareLines(lines, lines2);
        }

        [Test]
        public void UnchangingStoryboardedMapCodeTest() {
            var path = "Resources\\Camellia - Body F10ating in the Zero Gravity Space (Orange_) [Nonsubmersible].osu";
            var lines = File.ReadAllLines(path).ToList();
            var editor = new BeatmapEditor(path);
            var lines2 = editor.Beatmap.GetLines();

            CompareLines(lines, lines2);
        }

        [Test]
        public void UnchangingStoryboardCodeTest() {
            var path = "Resources\\TestStoryboard.osb";
            var lines = File.ReadAllLines(path).ToList();
            var editor = new StoryboardEditor(path);
            var lines2 = editor.StoryBoard.GetLines();

            CompareLines(lines, lines2);
        }

        private static void CompareLines(IReadOnlyList<string> expected, IReadOnlyList<string> actual) {
            var expectedGeneralLines = FileFormatHelper.GetCategoryLines(expected, "[General]").Select(NormalizeLine).ToArray();
            var expectedEditorLines = FileFormatHelper.GetCategoryLines(expected, "[Editor]").Select(NormalizeLine).ToArray();
            var expectedMetadataLines = FileFormatHelper.GetCategoryLines(expected, "[Metadata]").Select(NormalizeLine).ToArray();
            var expectedDifficultyLines = FileFormatHelper.GetCategoryLines(expected, "[Difficulty]").Select(NormalizeLine).ToArray();
            var expectedEventLines = FileFormatHelper.GetCategoryLines(expected, "[Events]").Select(NormalizeLine).ToArray();
            var expectedTimingLines = FileFormatHelper.GetCategoryLines(expected, "[TimingPoints]").Select(NormalizeLine).ToArray();
            var expectedColourLines = FileFormatHelper.GetCategoryLines(expected, "[Colours]").Select(NormalizeLine).ToArray();
            var expectedHitobjectLines = FileFormatHelper.GetCategoryLines(expected, "[HitObjects]").Select(NormalizeLine).ToArray();

            var actualGeneralLines = FileFormatHelper.GetCategoryLines(actual, "[General]").Select(NormalizeLine).ToArray();
            var actualEditorLines = FileFormatHelper.GetCategoryLines(actual, "[Editor]").Select(NormalizeLine).ToArray();
            var actualMetadataLines = FileFormatHelper.GetCategoryLines(actual, "[Metadata]").Select(NormalizeLine).ToArray();
            var actualDifficultyLines = FileFormatHelper.GetCategoryLines(actual, "[Difficulty]").Select(NormalizeLine).ToArray();
            var actualEventLines = FileFormatHelper.GetCategoryLines(actual, "[Events]").Select(NormalizeLine).ToArray();
            var actualTimingLines = FileFormatHelper.GetCategoryLines(actual, "[TimingPoints]").Select(NormalizeLine).ToArray();
            var actualColourLines = FileFormatHelper.GetCategoryLines(actual, "[Colours]").Select(NormalizeLine).ToArray();
            var actualHitobjectLines = FileFormatHelper.GetCategoryLines(actual, "[HitObjects]").Select(NormalizeLine).ToArray();

            CollectionAssert.AreEquivalent(expectedGeneralLines, actualGeneralLines);
            CollectionAssert.AreEquivalent(expectedEditorLines, actualEditorLines);
            CollectionAssert.AreEquivalent(expectedMetadataLines, actualMetadataLines);
            CollectionAssert.AreEquivalent(expectedDifficultyLines, actualDifficultyLines);
            AssertCollectionEqual(expectedEventLines, actualEventLines);
            AssertCollectionEqual(expectedTimingLines, actualTimingLines);
            AssertCollectionEqual(expectedColourLines, actualColourLines);
            AssertCollectionEqual(expectedHitobjectLines, actualHitobjectLines);
        }

        private static string NormalizeLine(string line) {
            // Remove all whitespace around colons and trim the line
            return System.Text.RegularExpressions.Regex.Replace(line, @"\s*:\s*", ":").Trim();
        }

        private static void AssertCollectionEqual(IReadOnlyList<string> expected, IReadOnlyList<string> actual) {
            Assert.AreEqual(expected.Count, actual.Count);

            for (int i = 0; i < expected.Count; i++) {
                Assert.AreEqual(NormalizeLine(expected[i]), NormalizeLine(actual[i]), $"At line {i} expected \"{expected[i]}\" but got \"{actual[i]}\".");
            }
        }
    }
}