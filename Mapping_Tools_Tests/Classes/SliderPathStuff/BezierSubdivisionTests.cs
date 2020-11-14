using Mapping_Tools.Classes.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.ToolHelpers;

namespace Mapping_Tools_Tests.Classes.SliderPathStuff {
    [TestClass]
    public class BezierSubdivisionTests {
        [TestMethod]
        public void Test1() {
            List<Vector2> points = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 1), new Vector2(2, 0) };
            BezierSubdivision subdivision = new BezierSubdivision(points);

            Assert.AreEqual(1, subdivision.Flatness(), 0.001);
            subdivision.ScaleLeft(0.5);
            Assert.AreEqual(0, (subdivision.Points[0] - new Vector2(1, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (subdivision.Points[1] - new Vector2(1.5, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (subdivision.Points[2] - new Vector2(2, 0)).Length, 0.001);
            subdivision.ScaleRight(-1);
            Assert.AreEqual(0, (subdivision.Points[0] - new Vector2(1, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (subdivision.Points[1] - new Vector2(0.5, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (subdivision.Points[2] - new Vector2(0, 0)).Length, 0.001);
            Assert.AreEqual(0.25, subdivision.Flatness(), 0.001);
            Assert.AreEqual(1.207106, subdivision.Length(), 0.001);
            var subdivision2 = subdivision.Prev();
            Assert.AreEqual(0, (subdivision2.Points[0] - new Vector2(2, 0)).Length, 0.001);
            Assert.AreEqual(0, (subdivision2.Points[1] - new Vector2(1.5, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (subdivision2.Points[2] - new Vector2(1, 0.5)).Length, 0.001);
            subdivision.Reverse();
            subdivision2 = subdivision.Next();
            Assert.AreEqual(0, (subdivision2.Points[0] - new Vector2(1, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (subdivision2.Points[1] - new Vector2(1.5, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (subdivision2.Points[2] - new Vector2(2, 0)).Length, 0.001);
            subdivision2 = subdivision.Parent();
            Assert.AreEqual(0, (subdivision2.Points[0] - new Vector2(0, 0)).Length, 0.001);
            Assert.AreEqual(0, (subdivision2.Points[1] - new Vector2(1, 1)).Length, 0.001);
            Assert.AreEqual(0, (subdivision2.Points[2] - new Vector2(2, 0)).Length, 0.001);
            subdivision2.Children(out subdivision, out subdivision2);
            Assert.AreEqual(0, (subdivision.Points[0] - new Vector2(0, 0)).Length, 0.001);
            Assert.AreEqual(0, (subdivision.Points[1] - new Vector2(0.5, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (subdivision.Points[2] - new Vector2(1, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (subdivision2.Points[0] - new Vector2(1, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (subdivision2.Points[1] - new Vector2(1.5, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (subdivision2.Points[2] - new Vector2(2, 0)).Length, 0.001);
            subdivision.Increase();
            Assert.AreEqual(0, (subdivision.Points[0] - new Vector2(0, 0)).Length, 0.001);
            Assert.AreEqual(0, (subdivision.Points[1] - new Vector2(0.33333, 0.33333)).Length, 0.001);
            Assert.AreEqual(0, (subdivision.Points[2] - new Vector2(0.66667, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (subdivision.Points[3] - new Vector2(1, 0.5)).Length, 0.001);
        }
        
        [TestMethod]
        public void Test2() {
            List<Vector2> points = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 2), new Vector2(2, 0) };
            BezierSubdivision subdivision = new BezierSubdivision(points);
            List<Vector2> points2 = new List<Vector2> { new Vector2(2, 0), new Vector2(4, 1), new Vector2(2, 6), new Vector2(1, 2) };
            BezierSubdivision subdivision2 = new BezierSubdivision(points2, 0, 1);
            LinkedList<BezierSubdivision> slider = new LinkedList<BezierSubdivision>();
            slider.AddLast(subdivision);
            slider.AddLast(subdivision2);

            BezierSubdivision.Subdivide(ref slider);
            var current = slider.First;
            Assert.AreEqual(0, (current.Value.Points[0] - new Vector2(0, 0)).Length, 0.001);
            Assert.AreEqual(0, (current.Value.Points[1] - new Vector2(0.25, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (current.Value.Points[2] - new Vector2(0.5, 0.75)).Length, 0.001);
            current = current.Next;
            Assert.IsNotNull(current);
            Assert.AreEqual(0, (current.Value.Points[0] - new Vector2(0.5, 0.75)).Length, 0.001);
            Assert.AreEqual(0, (current.Value.Points[1] - new Vector2(0.75, 1)).Length, 0.001);
            Assert.AreEqual(0, (current.Value.Points[2] - new Vector2(1, 1)).Length, 0.001);
            current = current.Next;
            Assert.IsNotNull(current);
            Assert.AreEqual(0, (current.Value.Points[0] - new Vector2(1, 1)).Length, 0.001);
            Assert.AreEqual(0, (current.Value.Points[1] - new Vector2(1.25, 1)).Length, 0.001);
            Assert.AreEqual(0, (current.Value.Points[2] - new Vector2(1.5, 0.75)).Length, 0.001);
            current = current.Next;
            Assert.IsNotNull(current);
            Assert.AreEqual(0, (current.Value.Points[0] - new Vector2(1.5, 0.75)).Length, 0.001);
            Assert.AreEqual(0, (current.Value.Points[1] - new Vector2(1.75, 0.5)).Length, 0.001);
            Assert.AreEqual(0, (current.Value.Points[2] - new Vector2(2, 0)).Length, 0.001);
            current = current.Next;
            Assert.IsNotNull(current);
            subdivision2 = current.Value;
            for (int i = subdivision2.Level; i > 0; i--) {
                subdivision2 = subdivision2.Parent();
            }
            Assert.AreEqual(0, (subdivision2.Points[0] - new Vector2(2, 0)).Length, 0.001);
            Assert.AreEqual(0, (subdivision2.Points[1] - new Vector2(4, 1)).Length, 0.001);
            Assert.AreEqual(0, (subdivision2.Points[2] - new Vector2(2, 6)).Length, 0.001);
            Assert.AreEqual(0, (subdivision2.Points[3] - new Vector2(1, 2)).Length, 0.001);
        }
        
        [TestMethod]
        public void Test3() {
            List<Vector2> points = new List<Vector2> { new Vector2(0, 0), new Vector2(4, 6), new Vector2(2, 1) };
            BezierSubdivision subdivision = new BezierSubdivision(points);
            Assert.AreEqual(0.1608, subdivision.LengthToT(2), 0.01);
            Assert.AreEqual(0.4568, subdivision.LengthToT(4), 0.01);
            Assert.AreEqual(1.1077, subdivision.LengthToT(8), 0.01);
            Assert.AreEqual(2.0559, subdivision.LengthToT(32), 0.01);
        }
        
        [TestMethod]
        public void Test4() {
            List<Vector2> points = new List<Vector2> { new Vector2(0, 0), new Vector2(100, 100), new Vector2(200, 0) };
            BezierSubdivision subdivision = new BezierSubdivision(points);
            var length = new SliderPath(PathType.Bezier, points.ToArray()).Distance;
            var length2 = subdivision.SubdividedApproximationLength(0.25);
            Assert.AreEqual(length, length2, 0.001);
            Assert.AreEqual(0.5, subdivision.LengthToT(length / 2), 0.01);
            Assert.AreEqual(1, subdivision.LengthToT(length), 0.01);
            Assert.AreEqual(0, subdivision.LengthToT(0), 0.01);
        }
    }
}