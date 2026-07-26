using Mapping_Tools.Core.Classes.SystemTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests.Classes.SystemTools {
    [TestClass]
    public class TypeConvertersTests {
        [TestMethod]
        public void ParseOsuTimestamp_ValidTimestamps_ReturnsExpectedTimes() {
            // Arrange
            // Act
            var test1 = TypeConverters.ParseOsuTimestamp("00:00:891 (1) - ");
            // Assert
            test1.TotalMilliseconds.Should().Be(891);

            var test2 = TypeConverters.ParseOsuTimestamp("60:00:074 (2,4) - ");
            test2.TotalMilliseconds.Should().Be(3600074);

            var test3 = TypeConverters.ParseOsuTimestamp("60:00:074 - ");
            test3.TotalMilliseconds.Should().Be(3600074);

            var test4 = TypeConverters.ParseOsuTimestamp("00:-01:-230 (1) - ");
            test4.TotalMilliseconds.Should().Be(-1230);
        }
    }
}
