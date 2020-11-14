using Mapping_Tools.Classes.Tools.ComboColourStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.ComboColourStudio {
    [TestClass]
    public class ComboColourProjectTests {
        [TestMethod]
        public void IsSubSequenceTest() {
            Assert.IsTrue(ComboColourProject.IsSubSequence(new []{1,2,3}, new []{1,2,3,4}));
            Assert.IsTrue(ComboColourProject.IsSubSequence(new []{1,2,3}, new []{1,2,3,4,6,5,2}));
            Assert.IsTrue(ComboColourProject.IsSubSequence(new int[]{}, new []{1,2,3,4}));
            Assert.IsFalse(ComboColourProject.IsSubSequence(new []{1,2,3}, new []{1,2,2,4}));
            Assert.IsFalse(ComboColourProject.IsSubSequence(new []{1,2,3}, new []{1,2}));
        }
    }
}