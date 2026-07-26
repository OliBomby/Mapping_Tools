using Mapping_Tools.Core.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.BeatmapHelper;

[TestClass]
public class StoryboardTests {
    [TestMethod]
    public void ParseAndSerialize_PreservesStoryboardFileExactly() {
        // Arrange
        const string path = "Resources\\TestStoryboard.osb";
        string expectedContent = File.ReadAllText(path);
        var storyboard = new StoryBoard(File.ReadAllLines(path).ToList());

        // Act
        string actualContent = string.Join(Environment.NewLine, storyboard.GetLines());

        // Assert
        actualContent.Should().Be(expectedContent);
    }

    [TestMethod]
    public void Parse_InvalidBreakTime_ThrowsBeatmapParsingException() {
        // Arrange
        var lines = new List<string> {
            "[Events]",
            "//Break Periods",
            "2,not-a-time,2000"
        };

        // Act
        Action act1 = () => new StoryBoard(lines);

        // Assert
        act1.Should().Throw<BeatmapParsingException>();
    }
}
