using Mapping_Tools.Classes.MathUtil;
using NUnit.Framework;

namespace Mapping_Tools_Tests.Classes.MathUtil;

[TestFixture]
public class MathUtilTests {
    [Test]
    public void Vector2AddTest() {
        var v1 = new Vector2(1, -4);
        var v2 = new Vector2(-8, 16);
        var expected = new Vector2(-7, 12);

        var actual = v1 + v2;

        Assert.That(actual, Is.EqualTo(expected));
    }
}