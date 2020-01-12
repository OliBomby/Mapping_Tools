using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Classes.SliderPathStuff.Tests {
    [TestClass]
    public class BezierSubdivisionTests {
        [TestMethod]
        public void LengthToTTest() {
            Debug.WriteLine(@"Testing LengthToT!");

            var points = new[] {new Vector2(0, 0), new Vector2(100, 100), new Vector2(200, 0)};
            var bezierSubdivision = new BezierSubdivision(points.ToList());

            var sliderPath = new SliderPath(PathType.Bezier, points);
            var length = sliderPath.Distance;

            var result = bezierSubdivision.LengthToT(length / 2, 0.1, 0.25);
            Debug.WriteLine(result);
            Assert.AreEqual(0.5, result, 0.1);

            //result = bezierSubdivision.LengthToT(length);
            //Trace.WriteLine(result);
            //Assert.AreEqual(1, result, 1);

            //result = bezierSubdivision.LengthToT(0);
            //Trace.WriteLine(result);
            //Assert.AreEqual(0, result, 1);
        }
    }
}