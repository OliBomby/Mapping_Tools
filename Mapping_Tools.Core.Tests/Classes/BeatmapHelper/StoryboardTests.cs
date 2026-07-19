using Mapping_Tools.Classes.BeatmapHelper;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.BeatmapHelper;

[TestClass]
public class StoryboardTests {
    [TestMethod]
    public void ParseAndSerialize_PreservesStoryboardFileExactly() {
        const string path = "Resources\\TestStoryboard.osb";
        string expectedContent = File.ReadAllText(path);
        var storyboard = new StoryBoard(File.ReadAllLines(path).ToList());

        string actualContent = string.Join(Environment.NewLine, storyboard.GetLines());

        Assert.AreEqual(expectedContent, actualContent);
    }

    [TestMethod]
    public void Parse_InvalidBreakTime_ThrowsBeatmapParsingException() {
        var lines = new List<string> {
            "[Events]",
            "//Break Periods",
            "2,not-a-time,2000"
        };

        Assert.ThrowsException<BeatmapParsingException>(() => new StoryBoard(lines));
    }
}
