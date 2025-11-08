using Mapping_Tools.Domain.MathUtil;

namespace Mapping_Tools.Domain.Tests.MathUtil;

[TestFixture]
public class Vector2Tests {
    [Test]
    public void Vector2AddTest() {
        var v1 = new Vector2(1, -4);
        var v2 = new Vector2(-8, 16);
        var expected = new Vector2(-7, 12);

        var actual = v1 + v2;

        Assert.That(actual, Is.EqualTo(expected));
    }
}