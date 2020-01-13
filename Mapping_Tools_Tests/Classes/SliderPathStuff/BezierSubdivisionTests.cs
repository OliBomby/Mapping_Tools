using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.SliderPathStuff {
    [TestClass]
    public class BezierSubdivisionTests {
        [TestMethod]
        public void Test1() {
            List<Vector2> points = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 1), new Vector2(2, 0) };
            BezierSubdivision subdivision = new BezierSubdivision(points);
            BezierSubdivision subdivision2;

            Assert.IsTrue(Math.Abs(subdivision.Flatness() - 1) < 0.001);
            subdivision.ScaleLeft(0.5);
            Assert.IsTrue((subdivision.points[0] - new Vector2(1, 0.5)).Length < 0.001);
            Assert.IsTrue((subdivision.points[1] - new Vector2(1.5, 0.5)).Length < 0.001);
            Assert.IsTrue((subdivision.points[2] - new Vector2(2, 0)).Length < 0.001);
            subdivision.ScaleRight(-1);
            Assert.IsTrue((subdivision.points[0] - new Vector2(1, 0.5)).Length < 0.001);
            Assert.IsTrue((subdivision.points[1] - new Vector2(0.5, 0.5)).Length < 0.001);
            Assert.IsTrue((subdivision.points[2] - new Vector2(0, 0)).Length < 0.001);
            Assert.IsTrue(Math.Abs(subdivision.Flatness() - 0.25) < 0.001);
            Assert.IsTrue(Math.Abs(subdivision.Length() - 1.207106) < 0.001);
            subdivision2 = subdivision.Prev();
            Assert.IsTrue((subdivision2.points[0] - new Vector2(2, 0)).Length < 0.001);
            Assert.IsTrue((subdivision2.points[1] - new Vector2(1.5, 0.5)).Length < 0.001);
            Assert.IsTrue((subdivision2.points[2] - new Vector2(1, 0.5)).Length < 0.001);
            subdivision.Reverse();
            subdivision2 = subdivision.Next();
            Assert.IsTrue((subdivision2.points[0] - new Vector2(1, 0.5)).Length < 0.001);
            Assert.IsTrue((subdivision2.points[1] - new Vector2(1.5, 0.5)).Length < 0.001);
            Assert.IsTrue((subdivision2.points[2] - new Vector2(2, 0)).Length < 0.001);
            subdivision2 = subdivision.Parent();
            Assert.IsTrue((subdivision2.points[0] - new Vector2(0, 0)).Length < 0.001);
            Assert.IsTrue((subdivision2.points[1] - new Vector2(1, 1)).Length < 0.001);
            Assert.IsTrue((subdivision2.points[2] - new Vector2(2, 0)).Length < 0.001);
            subdivision2.Children(out subdivision, out subdivision2);
            Assert.IsTrue((subdivision.points[0] - new Vector2(0, 0)).Length < 0.001);
            Assert.IsTrue((subdivision.points[1] - new Vector2(0.5, 0.5)).Length < 0.001);
            Assert.IsTrue((subdivision.points[2] - new Vector2(1, 0.5)).Length < 0.001);
            Assert.IsTrue((subdivision2.points[0] - new Vector2(1, 0.5)).Length < 0.001);
            Assert.IsTrue((subdivision2.points[1] - new Vector2(1.5, 0.5)).Length < 0.001);
            Assert.IsTrue((subdivision2.points[2] - new Vector2(2, 0)).Length < 0.001);
            subdivision.Increase();
            Assert.IsTrue((subdivision.points[0] - new Vector2(0, 0)).Length < 0.001);
            Assert.IsTrue((subdivision.points[1] - new Vector2(0.33333, 0.33333)).Length < 0.001);
            Assert.IsTrue((subdivision.points[2] - new Vector2(0.66667, 0.5)).Length < 0.001);
            Assert.IsTrue((subdivision.points[3] - new Vector2(1, 0.5)).Length < 0.001);
        }
        
        [TestMethod]
        public void Test2() {
            List<Vector2> points = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 2), new Vector2(2, 0) };
            BezierSubdivision subdivision = new BezierSubdivision(points, 0, 0);
            List<Vector2> points2 = new List<Vector2> { new Vector2(2, 0), new Vector2(4, 1), new Vector2(2, 6), new Vector2(1, 2) };
            BezierSubdivision subdivision2 = new BezierSubdivision(points2, 0, 1);
            LinkedList<BezierSubdivision> slider = new LinkedList<BezierSubdivision>();
            slider.AddLast(subdivision);
            slider.AddLast(subdivision2);

            BezierSubdivision.Subdivide(ref slider);
            var current = slider.First;
            Assert.IsTrue((current.Value.points[0] - new Vector2(0, 0)).Length < 0.001);
            Assert.IsTrue((current.Value.points[1] - new Vector2(0.25, 0.5)).Length < 0.001);
            Assert.IsTrue((current.Value.points[2] - new Vector2(0.5, 0.75)).Length < 0.001);
            current = current.Next;
            Assert.IsTrue((current.Value.points[0] - new Vector2(0.5, 0.75)).Length < 0.001);
            Assert.IsTrue((current.Value.points[1] - new Vector2(0.75, 1)).Length < 0.001);
            Assert.IsTrue((current.Value.points[2] - new Vector2(1, 1)).Length < 0.001);
            current = current.Next;
            Assert.IsTrue((current.Value.points[0] - new Vector2(1, 1)).Length < 0.001);
            Assert.IsTrue((current.Value.points[1] - new Vector2(1.25, 1)).Length < 0.001);
            Assert.IsTrue((current.Value.points[2] - new Vector2(1.5, 0.75)).Length < 0.001);
            current = current.Next;
            Assert.IsTrue((current.Value.points[0] - new Vector2(1.5, 0.75)).Length < 0.001);
            Assert.IsTrue((current.Value.points[1] - new Vector2(1.75, 0.5)).Length < 0.001);
            Assert.IsTrue((current.Value.points[2] - new Vector2(2, 0)).Length < 0.001);
            current = current.Next;
            subdivision2 = current.Value;
            for (int i = subdivision2.level; i > 0; i--) {
                subdivision2 = subdivision2.Parent();
            }
            Assert.IsTrue((subdivision2.points[0] - new Vector2(2, 0)).Length < 0.001);
            Assert.IsTrue((subdivision2.points[1] - new Vector2(4, 1)).Length < 0.001);
            Assert.IsTrue((subdivision2.points[2] - new Vector2(2, 6)).Length < 0.001);
            Assert.IsTrue((subdivision2.points[3] - new Vector2(1, 2)).Length < 0.001);
        }
        
        [TestMethod]
        public void Test3() {
            List<Vector2> points = new List<Vector2> { new Vector2(0, 0), new Vector2(4, 6), new Vector2(2, 1) };
            BezierSubdivision subdivision = new BezierSubdivision(points);
            Assert.IsTrue(Math.Abs(subdivision.LengthToT(2) - 0.1608) < 0.01);
            Assert.IsTrue(Math.Abs(subdivision.LengthToT(4) - 0.4568) < 0.01);
            Assert.IsTrue(Math.Abs(subdivision.LengthToT(8) - 1.1077) < 0.01);
            Assert.IsTrue(Math.Abs(subdivision.LengthToT(32) - 2.0559) < 0.01);
        }
    }
}