using Mapping_Tools.Classes.SystemTools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.SystemTools {
    [TestClass]
    public class TypeConvertersTests {
        [TestMethod]
        public void TimestampParserTest() {
            var test1 = TypeConverters.ParseOsuTimestamp("00:00:891 (1) - ");
            Assert.AreEqual(891, test1.TotalMilliseconds);

            var test2 = TypeConverters.ParseOsuTimestamp("60:00:074 (2,4) - ");
            Assert.AreEqual(3600074, test2.TotalMilliseconds);

            var test3 = TypeConverters.ParseOsuTimestamp("60:00:074 - ");
            Assert.AreEqual(3600074, test3.TotalMilliseconds);

            var test4 = TypeConverters.ParseOsuTimestamp("00:-01:-230 (1) - ");
            Assert.AreEqual(-1230, test4.TotalMilliseconds);
        }
    }
}