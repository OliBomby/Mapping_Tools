using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Mapping_Tools.Application.Abstractions;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.BeatmapHelper {
    [TestClass]
    public class BeatmapHelperTests {
        [TestMethod]
        public void GetLines_EmptyMap_PreservesFixtureContent() {
            // Arrange
            var path = "Resources\\EmptyTestMap.osu";
            var expectedContent = File.ReadAllText(path);
            var editor = new BeatmapEditor(path);

            // Act
            var actualContent = SerializeLines(editor.Beatmap.GetLines());

            // Assert
            actualContent.Should().Be(expectedContent);
        }

        [TestMethod]
        public void GetLines_ComplicatedMap_PreservesFixtureContent() {
            // Arrange
            var path = "Resources\\ComplicatedTestMap.osu";
            var expectedContent = File.ReadAllText(path);
            var editor = new BeatmapEditor(path);

            // Act
            var actualContent = SerializeLines(editor.Beatmap.GetLines());

            // Assert
            actualContent.Should().Be(expectedContent);
        }

        [TestMethod]
        public void GetLines_LazerMap_PreservesFixtureContent() {
            // Arrange
            var path = "Resources\\THE ORAL CIGARETTES - GET BACK (Nikakis) [Sotarks_ Cataclysm].osu";
            var expectedContent = File.ReadAllText(path);
            var editor = new BeatmapEditor(path);

            // Act
            var actualContent = SerializeLines(editor.Beatmap.GetLines());

            // Assert
            actualContent.Should().Be(expectedContent);
        }

        [TestMethod]
        public void GetLines_StoryboardedMap_PreservesFixtureContent() {
            // Arrange
            var path = "Resources\\Camellia - Body F10ating in the Zero Gravity Space (Orange_) [Nonsubmersible].osu";
            var expectedContent = File.ReadAllText(path);
            var editor = new BeatmapEditor(path);

            // Act
            var actualContent = SerializeLines(editor.Beatmap.GetLines());

            // Assert
            actualContent.Should().Be(expectedContent);
        }

        [TestMethod]
        public void SaveFile_StoryboardEditor2_UsesInjectedTextFileStore() {
            // Arrange
            const string path = "virtual.osb";
            var sourceLines = File.ReadAllLines("Resources\\TestStoryboard.osb");
            var store = new FakeTextFileStore(path, sourceLines);
            var editor = new StoryboardEditor2(path, store);

            // Act
            editor.SaveFile();

            // Assert
            store.WrittenLines.Should().Equal(editor.StoryBoard.GetLines());
            store.WrittenPath.Should().Be(path);
        }

        [TestMethod]
        public void SaveFile_BeatmapEditor2_LoadsAndWritesThroughFileStore() {
            // Arrange
            const string path = "virtual.osu";
            var sourceLines = File.ReadAllLines("Resources\\EmptyTestMap.osu");
            var store = new FakeTextFileStore(path, sourceLines);
            var editor = new BeatmapEditor2(path, store);

            // Act
            editor.SaveFile();

            // Assert
            editor.Beatmap.Metadata["Title"].Value.Should().Be("Why you have to be mad?");
            store.WrittenLines.Should().Equal(editor.Beatmap.GetLines());
            store.WrittenPath.Should().Be(path);
        }

        private sealed class FakeTextFileStore : ITextFileStore {
            private readonly string sourcePath;
            private readonly IReadOnlyList<string> sourceLines;

            public FakeTextFileStore(string sourcePath, IReadOnlyList<string> sourceLines) {
                this.sourcePath = sourcePath;
                this.sourceLines = sourceLines;
            }

            public string WrittenPath { get; private set; }
            public List<string> WrittenLines { get; private set; }

            public IReadOnlyList<string> ReadAllLines(string path) {
                path.Should().Be(sourcePath);
                return sourceLines;
            }

            public void WriteAllLines(string path, IEnumerable<string> lines) {
                WrittenPath = path;
                WrittenLines = lines.ToList();
            }

            public void Delete(string path) { }

            public string GetParentFolder(string path) => "virtual";

            public string CombinePath(string parent, string child) => $"{parent}/{child}";
        }

        private static string SerializeLines(IEnumerable<string> lines) {
            // Repository fixtures use LF line endings regardless of the host platform.
            return string.Join("\n", lines);
        }
    }
}
