using Mapping_Tools.Core.Classes.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Core.Tests {
    [TestClass]
    public class MathUtilTests {
        [TestMethod]
        public void Add_TwoVectors_ReturnsSum() {
            // Arrange
            var v1 = new Vector2(1, -4);
            var v2 = new Vector2(-8, 16);
            var expected = new Vector2(-7, 12);

            // Act
            var actual = v1 + v2;

            // Assert
            actual.Should().Be(expected, "Epic Fail");
        }
    }
}
