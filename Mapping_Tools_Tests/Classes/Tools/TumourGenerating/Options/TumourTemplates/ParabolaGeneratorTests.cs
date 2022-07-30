using Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.Tools.TumourGenerating.Options.TumourTemplates {
    [TestClass]
    public class ParabolaGeneratorTests {
        [TestMethod]
        public void TestDistanceFunction() {
            var template = new ParabolaTemplate { Length = 1, Width = 1 };
            var distanceFunc = template.GetDistanceRelation();

            Assert.AreEqual(0, distanceFunc(0));
            Assert.AreEqual(0.5, distanceFunc(0.5));
            Assert.AreEqual(1, distanceFunc(1));
        }
    }
}