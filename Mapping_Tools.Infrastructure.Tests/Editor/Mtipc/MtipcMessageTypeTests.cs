using Mapping_Tools.Infrastructure.Editor.Mtipc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Infrastructure.Tests.Editor.Mtipc;

[TestClass]
public sealed class MtipcMessageTypeTests
{
    [TestMethod]
    public void MessageTypes_MatchMtipcProtocolValues()
    {
        // Arrange
        var expected = new Dictionary<MtipcMessageType, int>
        {
            [MtipcMessageType.Hello] = 0,
            [MtipcMessageType.ReadBeatmap] = 3,
            [MtipcMessageType.ReadBookmarks] = 4,
            [MtipcMessageType.ReadControlPoints] = 5,
            [MtipcMessageType.ReadObjects] = 6,
            [MtipcMessageType.EditorTime] = 13,
            [MtipcMessageType.ReloadEditor] = 18,
        };

        // Act
        var actual = expected.ToDictionary(pair => pair.Key, pair => (int)pair.Key);

        // Assert
        actual.Should().Equal(expected);
    }
}
