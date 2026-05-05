using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mapping_Tools.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.BeatmapHelper {
    [TestClass]
    public class BeatmapHelperTests {
        [TestMethod]
        public void UnchangingEmptyMapCodeTest() {
            var path = "Resources\\EmptyTestMap.osu";
            var expectedContent = File.ReadAllText(path);
            var editor = new BeatmapEditor(path);
            var actualContent = SerializeLines(editor.Beatmap.GetLines());

            AssertEquals(expectedContent, actualContent);
        }

        [TestMethod]
        public void UnchangingComplicatedMapCodeTest() {
            var path = "Resources\\ComplicatedTestMap.osu";
            var expectedContent = File.ReadAllText(path);
            var editor = new BeatmapEditor(path);
            var actualContent = SerializeLines(editor.Beatmap.GetLines());

            AssertEquals(expectedContent, actualContent);
        }

        [TestMethod]
        public void UnchangingLazerMapCodeTest() {
            var path = "Resources\\THE ORAL CIGARETTES - GET BACK (Nikakis) [Sotarks_ Cataclysm].osu";
            var expectedContent = File.ReadAllText(path);
            var editor = new BeatmapEditor(path);
            var actualContent = SerializeLines(editor.Beatmap.GetLines());

            AssertEquals(expectedContent, actualContent);
        }

        [TestMethod]
        public void UnchangingStoryboardedMapCodeTest() {
            var path = "Resources\\Camellia - Body F10ating in the Zero Gravity Space (Orange_) [Nonsubmersible].osu";
            var expectedContent = File.ReadAllText(path);
            var editor = new BeatmapEditor(path);
            var actualContent = SerializeLines(editor.Beatmap.GetLines());

            AssertEquals(expectedContent, actualContent);
        }

        [TestMethod]
        public void UnchangingStoryboardCodeTest() {
            var path = "Resources\\TestStoryboard.osb";
            var expectedContent = File.ReadAllText(path);
            var editor = new StoryboardEditor(path);
            var actualContent = SerializeLines(editor.StoryBoard.GetLines());

            AssertEquals(expectedContent, actualContent);
        }

        private static void AssertEquals(string expectedContent, string actualContent) {
            if (string.Equals(expectedContent, actualContent, StringComparison.Ordinal)) {
                return;
            }

            var message = BuildDiffMessage(expectedContent, actualContent);
            Assert.Fail(message);
        }

        private static string BuildDiffMessage(string expectedContent, string actualContent) {
            var expectedLines = SplitLines(expectedContent);
            var actualLines = SplitLines(actualContent);
            var firstDifferenceIndex = GetFirstDifferenceIndex(expectedContent, actualContent);
            var expectedPosition = GetLineAndColumn(expectedContent, firstDifferenceIndex);
            var actualPosition = GetLineAndColumn(actualContent, firstDifferenceIndex);

            var builder = new StringBuilder();
            builder.AppendLine($"File contents differ at expected line {expectedPosition.line}, column {expectedPosition.column} and actual line {actualPosition.line}, column {actualPosition.column}.");
            builder.AppendLine($"Expected length: {expectedContent.Length} characters across {expectedLines.Count} lines.");
            builder.AppendLine($"Actual length:   {actualContent.Length} characters across {actualLines.Count} lines.");
            builder.AppendLine();
            builder.Append(BuildUnifiedDiff(expectedLines, actualLines));

            return builder.ToString();
        }

        private static int GetFirstDifferenceIndex(string expectedContent, string actualContent) {
            var maxSharedLength = Math.Min(expectedContent.Length, actualContent.Length);

            for (int i = 0; i < maxSharedLength; i++) {
                if (expectedContent[i] != actualContent[i]) {
                    return i;
                }
            }

            return maxSharedLength;
        }

        private static string SerializeLines(IEnumerable<string> lines) {
            return string.Join(Environment.NewLine, lines);
        }

        private static List<string> SplitLines(string content) {
            return content.Replace("\r\n", "\n").Replace("\r", "\n")
                .Split(new[] { '\n' }, StringSplitOptions.None)
                .ToList();
        }


        private static (int line, int column) GetLineAndColumn(string content, int index) {
            var boundedIndex = Math.Max(0, Math.Min(index, content.Length));
            var line = 1;
            var column = 1;

            for (int i = 0; i < boundedIndex; i++) {
                if (content[i] == '\n') {
                    line++;
                    column = 1;
                } else {
                    column++;
                }
            }

            return (line, column);
        }

        private static string BuildUnifiedDiff(IReadOnlyList<string> expected, IReadOnlyList<string> actual,
                int contextLines = 3, int lookAhead = 50, int maxHunks = 5, int maxChangedLinesPerSide = 20) {
            var builder = new StringBuilder();
            builder.AppendLine("--- expected");
            builder.AppendLine("+++ actual");

            var expectedIndex = 0;
            var actualIndex = 0;
            var emittedHunks = 0;

            while ((expectedIndex < expected.Count || actualIndex < actual.Count) && emittedHunks < maxHunks) {
                if (expectedIndex < expected.Count && actualIndex < actual.Count && expected[expectedIndex] == actual[actualIndex]) {
                    expectedIndex++;
                    actualIndex++;
                    continue;
                }

                var mismatchExpectedIndex = expectedIndex;
                var mismatchActualIndex = actualIndex;
                var (resyncedExpectedIndex, resyncedActualIndex) = FindResync(expected, actual, mismatchExpectedIndex,
                    mismatchActualIndex, lookAhead);

                var leadingContextCount = CountLeadingContext(expected, actual, mismatchExpectedIndex, mismatchActualIndex,
                    contextLines);
                var trailingContextCount = CountTrailingContext(expected, actual, resyncedExpectedIndex, resyncedActualIndex,
                    contextLines);

                var expectedHunkStart = mismatchExpectedIndex - leadingContextCount;
                var actualHunkStart = mismatchActualIndex - leadingContextCount;
                var expectedHunkLength = leadingContextCount + (resyncedExpectedIndex - mismatchExpectedIndex) + trailingContextCount;
                var actualHunkLength = leadingContextCount + (resyncedActualIndex - mismatchActualIndex) + trailingContextCount;

                builder.AppendLine($"@@ -{expectedHunkStart + 1},{expectedHunkLength} +{actualHunkStart + 1},{actualHunkLength} @@");

                for (int i = mismatchExpectedIndex - leadingContextCount; i < mismatchExpectedIndex; i++) {
                    builder.Append(' ').AppendLine(expected[i]);
                }

                AppendChangedLines(builder, expected, mismatchExpectedIndex, resyncedExpectedIndex, '-', maxChangedLinesPerSide);
                AppendChangedLines(builder, actual, mismatchActualIndex, resyncedActualIndex, '+', maxChangedLinesPerSide);

                for (int i = 0; i < trailingContextCount; i++) {
                    builder.Append(' ').AppendLine(expected[resyncedExpectedIndex + i]);
                }

                expectedIndex = resyncedExpectedIndex;
                actualIndex = resyncedActualIndex;
                emittedHunks++;
            }

            if (expectedIndex < expected.Count || actualIndex < actual.Count) {
                builder.AppendLine($"... diff truncated after {maxHunks} hunks ...");
            }

            return builder.ToString();
        }

        private static int CountLeadingContext(IReadOnlyList<string> expected, IReadOnlyList<string> actual,
                int expectedIndex, int actualIndex, int contextLines) {
            var count = 0;

            while (count < contextLines && expectedIndex - count - 1 >= 0 && actualIndex - count - 1 >= 0 &&
                   expected[expectedIndex - count - 1] == actual[actualIndex - count - 1]) {
                count++;
            }

            return count;
        }

        private static int CountTrailingContext(IReadOnlyList<string> expected, IReadOnlyList<string> actual,
                int expectedIndex, int actualIndex, int contextLines) {
            var count = 0;

            while (count < contextLines && expectedIndex + count < expected.Count && actualIndex + count < actual.Count &&
                   expected[expectedIndex + count] == actual[actualIndex + count]) {
                count++;
            }

            return count;
        }

        private static (int expectedIndex, int actualIndex) FindResync(IReadOnlyList<string> expected,
                IReadOnlyList<string> actual, int expectedStart, int actualStart, int lookAhead, int stableMatchLength = 2) {
            for (int distance = 1; distance <= lookAhead * 2; distance++) {
                var minExpectedSkip = Math.Max(0, distance - lookAhead);
                var maxExpectedSkip = Math.Min(lookAhead, distance);

                for (int expectedSkip = minExpectedSkip; expectedSkip <= maxExpectedSkip; expectedSkip++) {
                    var actualSkip = distance - expectedSkip;
                    var candidateExpectedIndex = expectedStart + expectedSkip;
                    var candidateActualIndex = actualStart + actualSkip;

                    if (candidateExpectedIndex >= expected.Count || candidateActualIndex >= actual.Count) {
                        continue;
                    }

                    if (IsStableMatch(expected, actual, candidateExpectedIndex, candidateActualIndex, stableMatchLength)) {
                        return (candidateExpectedIndex, candidateActualIndex);
                    }
                }
            }

            return (expected.Count, actual.Count);
        }

        private static bool IsStableMatch(IReadOnlyList<string> expected, IReadOnlyList<string> actual,
                int expectedIndex, int actualIndex, int stableMatchLength) {
            for (int i = 0; i < stableMatchLength; i++) {
                var currentExpectedIndex = expectedIndex + i;
                var currentActualIndex = actualIndex + i;

                if (currentExpectedIndex >= expected.Count || currentActualIndex >= actual.Count) {
                    return i > 0;
                }

                if (expected[currentExpectedIndex] != actual[currentActualIndex]) {
                    return false;
                }
            }

            return true;
        }

        private static void AppendChangedLines(StringBuilder builder, IReadOnlyList<string> lines, int start, int end,
                char prefix, int maxLines) {
            var lineCount = end - start;
            var displayedLineCount = Math.Min(lineCount, maxLines);

            for (int i = 0; i < displayedLineCount; i++) {
                builder.Append(prefix).AppendLine(lines[start + i]);
            }

            if (lineCount > maxLines) {
                builder.Append(prefix).AppendLine($"... {lineCount - maxLines} more line(s) ...");
            }
        }
    }
}