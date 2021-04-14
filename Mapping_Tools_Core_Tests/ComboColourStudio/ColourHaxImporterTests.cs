using Mapping_Tools_Core.Tools.ComboColourStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Core_Tests.ComboColourStudio {
    [TestClass]
    public class ColourHaxImporterTests {
        [TestMethod]
        public void IsSubSequenceTest() {
            Assert.IsTrue(ColourHaxImporter.IsSubSequence(new []{1,2,3}, new []{1,2,3,4}));
            Assert.IsTrue(ColourHaxImporter.IsSubSequence(new []{1,2,3}, new []{1,2,3,4,6,5,2}));
            Assert.IsTrue(ColourHaxImporter.IsSubSequence(new int[]{}, new []{1,2,3,4}));
            Assert.IsFalse(ColourHaxImporter.IsSubSequence(new []{1,2,3}, new []{1,2,2,4}));
            Assert.IsFalse(ColourHaxImporter.IsSubSequence(new []{1,2,3}, new []{1,2}));
        }
    }
}