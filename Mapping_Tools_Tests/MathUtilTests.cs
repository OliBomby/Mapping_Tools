using Mapping_Tools.Classes.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests {
    [TestClass]
    public class MathUtilTests {
        [TestMethod]
        public void Vector2AddTest() {
            var v1 = new Vector2(1, -4);
            var v2 = new Vector2(-8, 16);
            var expected = new Vector2(-7, 12);

            var actual = v1 + v2;

            Assert.AreEqual(expected, actual, "Epic Fail");
        }
    }
}
