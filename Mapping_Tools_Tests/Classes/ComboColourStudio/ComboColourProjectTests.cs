using Mapping_Tools.Classes.Tools.ComboColourStudio;
using NUnit.Framework;

namespace Mapping_Tools_Tests.Classes.ComboColourStudio;

[TestFixture]
public class ComboColourProjectTests {
    [Test]
    public void IsSubSequenceTest() {
        Assert.That(ComboColourProject.IsSubSequence(new []{1,2,3}, new []{1,2,3,4}), Is.True);
        Assert.That(ComboColourProject.IsSubSequence(new []{1,2,3}, new []{1,2,3,4,6,5,2}), Is.True);
        Assert.That(ComboColourProject.IsSubSequence(new int[]{}, new []{1,2,3,4}), Is.True);
        Assert.That(ComboColourProject.IsSubSequence(new []{1,2,3}, new []{1,2,2,4}), Is.False);
        Assert.That(ComboColourProject.IsSubSequence(new []{1,2,3}, new []{1,2}), Is.False);
    }
}