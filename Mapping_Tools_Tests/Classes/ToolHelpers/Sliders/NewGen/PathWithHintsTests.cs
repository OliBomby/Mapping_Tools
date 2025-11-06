using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen;
using NUnit.Framework;

namespace Mapping_Tools_Tests.Classes.ToolHelpers.Sliders.NewGen {
    [TestFixture]
    public class PathWithHintsTests {
        private PathWithHints path;
        private List<LinkedListNode<PathPoint>> points;
        private const int NumPoints = 11;

        [SetUp]
        public void Initialize() {
            points = new List<LinkedListNode<PathPoint>>();
            path = new PathWithHints();
            for (int i = 0; i < NumPoints; i++) {
                path.Path.AddLast(new PathPoint(new Vector2(i, 0), 0, 0, i));
                points.Add(path.Path.Last);
            }
            path.AddReconstructionHint(new ReconstructionHint(path.Path.First, path.Path.Last, -1, new List<Vector2> {
                new(0,0),
                new(NumPoints - 1,0)
            }, PathType.Linear));
        }

        [Test]
        public void TestZeroLengthHint() {
            Assert.Throws<ArgumentException>(() => path.AddReconstructionHint(new ReconstructionHint(points[2], points[2], 0, new List<Vector2> {
                new(2,0),
                new(2, 1),
                new(2,0)
            })));
        }

        [Test]
        public void TestBasicOverlaps() {
            path.AddReconstructionHint(new ReconstructionHint(points[2], points[8], 0, new List<Vector2> {
                new(2,0),
                new(5, 1),
                new(8,0)
            }));

            Assert.That(path.ReconstructionHints.Count, Is.EqualTo(3));
            Assert.That(path.ReconstructionHints[0].Start, Is.EqualTo(points[0]));
            Assert.That(path.ReconstructionHints[0].End, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[0].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[0].EndP, Is.EqualTo(0.2));
            Assert.That(path.ReconstructionHints[0].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[1].Start, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[1].End, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[1].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[1].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[1].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[2].Start, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[2].End, Is.EqualTo(points[10]));
            Assert.That(path.ReconstructionHints[2].StartP, Is.EqualTo(0.8));
            Assert.That(path.ReconstructionHints[2].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[2].Anchors, Is.Not.Null);

            path.AddReconstructionHint(new ReconstructionHint(points[0], points[1], 0, new List<Vector2> {
                new(0,0),
                new(0.5, 1),
                new(1,0)
            }));

            Assert.That(path.ReconstructionHints.Count, Is.EqualTo(4));
            Assert.That(path.ReconstructionHints[0].Start, Is.EqualTo(points[0]));
            Assert.That(path.ReconstructionHints[0].End, Is.EqualTo(points[1]));
            Assert.That(path.ReconstructionHints[0].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[0].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[0].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[1].Start, Is.EqualTo(points[1]));
            Assert.That(path.ReconstructionHints[1].End, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[1].StartP, Is.EqualTo(0.1));
            Assert.That(path.ReconstructionHints[1].EndP, Is.EqualTo(0.2));
            Assert.That(path.ReconstructionHints[1].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[2].Start, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[2].End, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[2].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[2].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[2].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[3].Start, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[3].End, Is.EqualTo(points[10]));
            Assert.That(path.ReconstructionHints[3].StartP, Is.EqualTo(0.8));
            Assert.That(path.ReconstructionHints[3].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[3].Anchors, Is.Not.Null);

            path.AddReconstructionHint(new ReconstructionHint(points[9], points[10], 0, new List<Vector2> {
                new(9,0),
                new(9.5, 1),
                new(10,0)
            }));

            Assert.That(path.ReconstructionHints.Count, Is.EqualTo(5));
            Assert.That(path.ReconstructionHints[0].Start, Is.EqualTo(points[0]));
            Assert.That(path.ReconstructionHints[0].End, Is.EqualTo(points[1]));
            Assert.That(path.ReconstructionHints[0].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[0].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[0].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[1].Start, Is.EqualTo(points[1]));
            Assert.That(path.ReconstructionHints[1].End, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[1].StartP, Is.EqualTo(0.1));
            Assert.That(path.ReconstructionHints[1].EndP, Is.EqualTo(0.2));
            Assert.That(path.ReconstructionHints[1].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[2].Start, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[2].End, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[2].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[2].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[2].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[3].Start, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[3].End, Is.EqualTo(points[9]));
            Assert.That(path.ReconstructionHints[3].StartP, Is.EqualTo(0.8));
            Assert.That(path.ReconstructionHints[3].EndP, Is.EqualTo(0.9));
            Assert.That(path.ReconstructionHints[3].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[4].Start, Is.EqualTo(points[9]));
            Assert.That(path.ReconstructionHints[4].End, Is.EqualTo(points[10]));
            Assert.That(path.ReconstructionHints[4].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[4].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[4].Anchors, Is.Not.Null);

            path.AddReconstructionHint(new ReconstructionHint(points[1], points[2], 0, null));

            Assert.That(path.ReconstructionHints.Count, Is.EqualTo(5));
            Assert.That(path.ReconstructionHints[0].Start, Is.EqualTo(points[0]));
            Assert.That(path.ReconstructionHints[0].End, Is.EqualTo(points[1]));
            Assert.That(path.ReconstructionHints[0].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[0].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[0].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[1].Start, Is.EqualTo(points[1]));
            Assert.That(path.ReconstructionHints[1].End, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[1].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[1].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[1].Anchors, Is.Null);
            Assert.That(path.ReconstructionHints[2].Start, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[2].End, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[2].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[2].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[2].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[3].Start, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[3].End, Is.EqualTo(points[9]));
            Assert.That(path.ReconstructionHints[3].StartP, Is.EqualTo(0.8));
            Assert.That(path.ReconstructionHints[3].EndP, Is.EqualTo(0.9));
            Assert.That(path.ReconstructionHints[3].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[4].Start, Is.EqualTo(points[9]));
            Assert.That(path.ReconstructionHints[4].End, Is.EqualTo(points[10]));
            Assert.That(path.ReconstructionHints[4].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[4].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[4].Anchors, Is.Not.Null);
        }

        [Test]
        public void TestSameLayerOverlapsLeft() {
            path.AddReconstructionHint(new ReconstructionHint(points[2], points[8], 0, new List<Vector2> {
                new(2,0),
                new(5, 1),
                new(8,0)
            }));

            path.AddReconstructionHint(new ReconstructionHint(points[1], points[3], 0, new List<Vector2> {
                new(1,0),
                new(2, 1),
                new(3, 0)
            }));

            Assert.That(path.ReconstructionHints.Count, Is.EqualTo(5));
            Assert.That(path.ReconstructionHints[0].Start, Is.EqualTo(points[0]));
            Assert.That(path.ReconstructionHints[0].End, Is.EqualTo(points[1]));
            Assert.That(path.ReconstructionHints[0].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[0].EndP, Is.EqualTo(0.1));
            Assert.That(path.ReconstructionHints[0].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[1].Start, Is.EqualTo(points[1]));
            Assert.That(path.ReconstructionHints[1].End, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[1].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[1].EndP, Is.EqualTo(0.5));
            Assert.That(path.ReconstructionHints[1].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[2].Start, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[2].End, Is.EqualTo(points[3]));
            Assert.That(path.ReconstructionHints[2].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[2].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[2].Anchors, Is.Null);
            Assert.That(path.ReconstructionHints[3].Start, Is.EqualTo(points[3]));
            Assert.That(path.ReconstructionHints[3].End, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[3].StartP, Is.EqualTo(1/6d).Within(1e-12));
            Assert.That(path.ReconstructionHints[3].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[3].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[4].Start, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[4].End, Is.EqualTo(points[10]));
            Assert.That(path.ReconstructionHints[4].StartP, Is.EqualTo(0.8));
            Assert.That(path.ReconstructionHints[4].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[4].Anchors, Is.Not.Null);
        }

        [Test]
        public void TestSameLayerOverlapsRight() {
            path.AddReconstructionHint(new ReconstructionHint(points[2], points[8], 0, new List<Vector2> {
                new(2,0),
                new(5, 1),
                new(8,0)
            }));

            path.AddReconstructionHint(new ReconstructionHint(points[7], points[9], 0, new List<Vector2> {
                new(7,0),
                new(8, 1),
                new(9, 0)
            }));

            Assert.That(path.ReconstructionHints.Count, Is.EqualTo(5));
            Assert.That(path.ReconstructionHints[0].Start, Is.EqualTo(points[0]));
            Assert.That(path.ReconstructionHints[0].End, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[0].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[0].EndP, Is.EqualTo(0.2));
            Assert.That(path.ReconstructionHints[0].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[1].Start, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[1].End, Is.EqualTo(points[7]));
            Assert.That(path.ReconstructionHints[1].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[1].EndP, Is.EqualTo(1 - 1/6d).Within(1e-12));
            Assert.That(path.ReconstructionHints[1].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[2].Start, Is.EqualTo(points[7]));
            Assert.That(path.ReconstructionHints[2].End, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[2].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[2].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[2].Anchors, Is.Null);
            Assert.That(path.ReconstructionHints[3].Start, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[3].End, Is.EqualTo(points[9]));
            Assert.That(path.ReconstructionHints[3].StartP, Is.EqualTo(0.5));
            Assert.That(path.ReconstructionHints[3].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[3].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[4].Start, Is.EqualTo(points[9]));
            Assert.That(path.ReconstructionHints[4].End, Is.EqualTo(points[10]));
            Assert.That(path.ReconstructionHints[4].StartP, Is.EqualTo(0.9));
            Assert.That(path.ReconstructionHints[4].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[4].Anchors, Is.Not.Null);
        }

        [Test]
        public void TestSameLayerOverlapsMiddle() {
            path.AddReconstructionHint(new ReconstructionHint(points[2], points[8], 0, new List<Vector2> {
                new(2,0),
                new(5, 1),
                new(8,0)
            }));

            path.AddReconstructionHint(new ReconstructionHint(points[3], points[7], 0, new List<Vector2> {
                new(3,0),
                new(5, 1),
                new(7, 0)
            }));

            Assert.That(path.ReconstructionHints.Count, Is.EqualTo(5));
            Assert.That(path.ReconstructionHints[0].Start, Is.EqualTo(points[0]));
            Assert.That(path.ReconstructionHints[0].End, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[0].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[0].EndP, Is.EqualTo(0.2));
            Assert.That(path.ReconstructionHints[0].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[1].Start, Is.EqualTo(points[2]));
            Assert.That(path.ReconstructionHints[1].End, Is.EqualTo(points[3]));
            Assert.That(path.ReconstructionHints[1].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[1].EndP, Is.EqualTo(1/6d).Within(1e-12));
            Assert.That(path.ReconstructionHints[1].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[2].Start, Is.EqualTo(points[3]));
            Assert.That(path.ReconstructionHints[2].End, Is.EqualTo(points[7]));
            Assert.That(path.ReconstructionHints[2].StartP, Is.EqualTo(0));
            Assert.That(path.ReconstructionHints[2].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[2].Anchors, Is.Null);
            Assert.That(path.ReconstructionHints[3].Start, Is.EqualTo(points[7]));
            Assert.That(path.ReconstructionHints[3].End, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[3].StartP, Is.EqualTo(1 - 1/6d).Within(1e-12));
            Assert.That(path.ReconstructionHints[3].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[3].Anchors, Is.Not.Null);
            Assert.That(path.ReconstructionHints[4].Start, Is.EqualTo(points[8]));
            Assert.That(path.ReconstructionHints[4].End, Is.EqualTo(points[10]));
            Assert.That(path.ReconstructionHints[4].StartP, Is.EqualTo(0.8));
            Assert.That(path.ReconstructionHints[4].EndP, Is.EqualTo(1));
            Assert.That(path.ReconstructionHints[4].Anchors, Is.Not.Null);
        }
    }
}