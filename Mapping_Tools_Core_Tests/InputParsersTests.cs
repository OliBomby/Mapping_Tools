using Mapping_Tools_Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace Mapping_Tools_Core_Tests {
    [TestClass]
    public class SerializationTests {
        [TestMethod]
        public void ParseDoubleTest() {
            double actual = InputParsers.ParseDouble("1 + 1");
            const double expected = 2;

            Assert.AreEqual(expected, actual);
        }
    }
}