using System;
using Mapping_Tools.Classes.MathUtil;
using NUnit.Framework;

namespace Mapping_Tools_Tests.Classes.MathUtil;

[TestFixture]
public class GradientDescentUtilTests {
    [Test]
    public void GradientDescentTest() {
        Func<double, double> func = Math.Sin;
        const double lower = 0;
        const double upper = 2 * Math.PI;
        const double rate = 0.5;

        double result1 = GradientDescentUtil.GradientDescent(func, lower, upper, rate);
        Assert.That(result1, Is.EqualTo(1.5 * Math.PI).Within(0.001));

        double result2 = GradientDescentUtil.GradientAscent(func, lower, upper, rate);
        Assert.That(result2, Is.EqualTo(0.5 * Math.PI).Within(0.001));

        double result3 = GradientDescentUtil.GradientDescent(func, lower + 4 * Math.PI, upper + 4 * Math.PI, rate);
        Assert.That(result3, Is.EqualTo(1.5 * Math.PI + 4 * Math.PI).Within(0.001));
    }
}