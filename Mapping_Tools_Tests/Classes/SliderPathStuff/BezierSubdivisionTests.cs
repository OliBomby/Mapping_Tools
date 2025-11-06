using Mapping_Tools.Classes.MathUtil;
using NUnit.Framework;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.ToolHelpers.Sliders;

namespace Mapping_Tools_Tests.Classes.SliderPathStuff {
    [TestFixture]
    public class BezierSubdivisionTests {
        [Test]
        public void Test1() {
            List<Vector2> points = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 1), new Vector2(2, 0) };
            BezierSubdivision subdivision = new BezierSubdivision(points);

            Assert.That(subdivision.Flatness(), Is.EqualTo(1).Within(0.001));
            subdivision.ScaleLeft(0.5);
            Assert.That((subdivision.Points[0] - new Vector2(1, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision.Points[1] - new Vector2(1.5, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision.Points[2] - new Vector2(2, 0)).Length, Is.EqualTo(0).Within(0.001));
            subdivision.ScaleRight(-1);
            Assert.That((subdivision.Points[0] - new Vector2(1, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision.Points[1] - new Vector2(0.5, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision.Points[2] - new Vector2(0, 0)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That(subdivision.Flatness(), Is.EqualTo(0.25).Within(0.001));
            Assert.That(subdivision.Length(), Is.EqualTo(1.207106).Within(0.001));
            var subdivision2 = subdivision.Prev();
            Assert.That((subdivision2.Points[0] - new Vector2(2, 0)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision2.Points[1] - new Vector2(1.5, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision2.Points[2] - new Vector2(1, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            subdivision.Reverse();
            subdivision2 = subdivision.Next();
            Assert.That((subdivision2.Points[0] - new Vector2(1, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision2.Points[1] - new Vector2(1.5, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision2.Points[2] - new Vector2(2, 0)).Length, Is.EqualTo(0).Within(0.001));
            subdivision2 = subdivision.Parent();
            Assert.That((subdivision2.Points[0] - new Vector2(0, 0)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision2.Points[1] - new Vector2(1, 1)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision2.Points[2] - new Vector2(2, 0)).Length, Is.EqualTo(0).Within(0.001));
            subdivision2.Children(out subdivision, out subdivision2);
            Assert.That((subdivision.Points[0] - new Vector2(0, 0)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision.Points[1] - new Vector2(0.5, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision.Points[2] - new Vector2(1, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision2.Points[0] - new Vector2(1, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision2.Points[1] - new Vector2(1.5, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision2.Points[2] - new Vector2(2, 0)).Length, Is.EqualTo(0).Within(0.001));
            subdivision.Increase();
            Assert.That((subdivision.Points[0] - new Vector2(0, 0)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision.Points[1] - new Vector2(0.33333, 0.33333)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision.Points[2] - new Vector2(0.66667, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision.Points[3] - new Vector2(1, 0.5)).Length, Is.EqualTo(0).Within(0.001));
        }
        
        [Test]
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
            Assert.That((current.Value.Points[0] - new Vector2(0, 0)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((current.Value.Points[1] - new Vector2(0.25, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((current.Value.Points[2] - new Vector2(0.5, 0.75)).Length, Is.EqualTo(0).Within(0.001));
            current = current.Next;
            Assert.That(current, Is.Not.Null);
            Assert.That((current.Value.Points[0] - new Vector2(0.5, 0.75)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((current.Value.Points[1] - new Vector2(0.75, 1)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((current.Value.Points[2] - new Vector2(1, 1)).Length, Is.EqualTo(0).Within(0.001));
            current = current.Next;
            Assert.That(current, Is.Not.Null);
            Assert.That((current.Value.Points[0] - new Vector2(1, 1)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((current.Value.Points[1] - new Vector2(1.25, 1)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((current.Value.Points[2] - new Vector2(1.5, 0.75)).Length, Is.EqualTo(0).Within(0.001));
            current = current.Next;
            Assert.That(current, Is.Not.Null);
            Assert.That((current.Value.Points[0] - new Vector2(1.5, 0.75)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((current.Value.Points[1] - new Vector2(1.75, 0.5)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((current.Value.Points[2] - new Vector2(2, 0)).Length, Is.EqualTo(0).Within(0.001));
            current = current.Next;
            Assert.That(current, Is.Not.Null);
            subdivision2 = current.Value;
            for (int i = subdivision2.Level; i > 0; i--) {
                subdivision2 = subdivision2.Parent();
            }
            Assert.That((subdivision2.Points[0] - new Vector2(2, 0)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision2.Points[1] - new Vector2(4, 1)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision2.Points[2] - new Vector2(2, 6)).Length, Is.EqualTo(0).Within(0.001));
            Assert.That((subdivision2.Points[3] - new Vector2(1, 2)).Length, Is.EqualTo(0).Within(0.001));
        }
        
        [Test]
        public void Test3() {
            List<Vector2> points = new List<Vector2> { new Vector2(0, 0), new Vector2(4, 6), new Vector2(2, 1) };
            BezierSubdivision subdivision = new BezierSubdivision(points);
            Assert.That(subdivision.LengthToT(2), Is.EqualTo(0.1608).Within(0.01));
            Assert.That(subdivision.LengthToT(4), Is.EqualTo(0.4568).Within(0.01));
            Assert.That(subdivision.LengthToT(8), Is.EqualTo(1.1077).Within(0.01));
            Assert.That(subdivision.LengthToT(32), Is.EqualTo(2.0559).Within(0.01));
        }
        
        [Test]
        public void Test4() {
            List<Vector2> points = new List<Vector2> { new Vector2(0, 0), new Vector2(100, 100), new Vector2(200, 0) };
            BezierSubdivision subdivision = new BezierSubdivision(points);
            var length = new SliderPath(PathType.Bezier, points.ToArray()).Distance;
            var length2 = subdivision.SubdividedApproximationLength(0.25);
            Assert.That(length2, Is.EqualTo(length).Within(0.001));
            Assert.That(subdivision.LengthToT(length / 2), Is.EqualTo(0.5).Within(0.01));
            Assert.That(subdivision.LengthToT(length), Is.EqualTo(1).Within(0.01));
            Assert.That(subdivision.LengthToT(0), Is.EqualTo(0).Within(0.01));
        }
    }
}