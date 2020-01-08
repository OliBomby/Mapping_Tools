using System;
using Mapping_Tools.Classes.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.MathUtil {
    [TestClass]
    public class GradientDescentUtilTests {
        [TestMethod]
        public void GradientDescentTest() {
            Func<double, double> func = Math.Sin;
            const double lower = 0;
            const double upper = 2 * Math.PI;
            const double rate = 0.5;

            double result1 = GradientDescentUtil.GradientDescent(func, lower, upper, rate);
            Assert.IsTrue(Math.Abs(result1 - 1.5 * Math.PI) < 0.001);

            double result2 = GradientDescentUtil.GradientAscent(func, lower, upper, rate);
            Assert.IsTrue(Math.Abs(result2 - 0.5 * Math.PI) < 0.001);

            double result3 = GradientDescentUtil.GradientDescent(func, lower + 4 * Math.PI, upper + 4 * Math.PI, rate);
            Assert.IsTrue(Math.Abs(result3 - (1.5 * Math.PI + 4 * Math.PI)) < 0.001);
        }
    }
}