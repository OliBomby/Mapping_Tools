using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.BeatmapHelper;

[TestClass]
public class StoryboardTests
{
    [TestMethod]
    public void DecodeAndEncode_StoryboardFixture_PreservesNormalizedContent()
    {
        // Arrange
        const string path = "Resources\\TestStoryboard.osb";
        string expectedContent = File.ReadAllText(path);
        var storyboard = new StoryboardDecoder().Decode(expectedContent);

        // Act
        string actualContent = new StoryboardEncoder().Encode(storyboard);
        string expectedNormalized = expectedContent.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');
        string actualNormalized = actualContent.Replace("\r\n", "\n").Replace('\r', '\n').TrimEnd('\n');

        // Assert
        actualNormalized.Should().Be(expectedNormalized);
        actualContent.Replace("\r\n", "").Should().NotContain("\n");
    }

    [TestMethod]
    public void Decode_InvalidBreakTime_ThrowsBeatmapParsingException()
    {
        // Arrange
        const string text = "[Events]\r\n//Break Periods\r\n2,not-a-time,2000";

        // Act
        Action act = () => _ = new StoryboardDecoder().Decode(text);

        // Assert
        act.Should().Throw<BeatmapParsingException>();
    }
}
