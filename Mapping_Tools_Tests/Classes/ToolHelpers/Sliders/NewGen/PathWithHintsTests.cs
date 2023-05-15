using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.ToolHelpers.Sliders.NewGen {
    [TestClass]
    public class PathWithHintsTests {
        private PathWithHints path;
        private List<LinkedListNode<PathPoint>> points;
        private const int NumPoints = 11;

        [TestInitialize]
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

        [TestMethod]
        public void TestZeroLengthHint() {
            Assert.ThrowsException<ArgumentException>(() => path.AddReconstructionHint(new ReconstructionHint(points[2], points[2], 0, new List<Vector2> {
                new(2,0),
                new(2, 1),
                new(2,0)
            })));
        }

        [TestMethod]
        public void TestBasicOverlaps() {
            path.AddReconstructionHint(new ReconstructionHint(points[2], points[8], 0, new List<Vector2> {
                new(2,0),
                new(5, 1),
                new(8,0)
            }));

            Assert.AreEqual(3, path.ReconstructionHints.Count);
            Assert.AreEqual(points[0], path.ReconstructionHints[0].Start);
            Assert.AreEqual(points[2], path.ReconstructionHints[0].End);
            Assert.AreEqual(0, path.ReconstructionHints[0].StartP);
            Assert.AreEqual(0.2, path.ReconstructionHints[0].EndP);
            Assert.IsNotNull(path.ReconstructionHints[0].Anchors);
            Assert.AreEqual(points[2], path.ReconstructionHints[1].Start);
            Assert.AreEqual(points[8], path.ReconstructionHints[1].End);
            Assert.AreEqual(0, path.ReconstructionHints[1].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[1].EndP);
            Assert.IsNotNull(path.ReconstructionHints[1].Anchors);
            Assert.AreEqual(points[8], path.ReconstructionHints[2].Start);
            Assert.AreEqual(points[10], path.ReconstructionHints[2].End);
            Assert.AreEqual(0.8, path.ReconstructionHints[2].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[2].EndP);
            Assert.IsNotNull(path.ReconstructionHints[2].Anchors);

            path.AddReconstructionHint(new ReconstructionHint(points[0], points[1], 0, new List<Vector2> {
                new(0,0),
                new(0.5, 1),
                new(1,0)
            }));

            Assert.AreEqual(4, path.ReconstructionHints.Count);
            Assert.AreEqual(points[0], path.ReconstructionHints[0].Start);
            Assert.AreEqual(points[1], path.ReconstructionHints[0].End);
            Assert.AreEqual(0, path.ReconstructionHints[0].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[0].EndP);
            Assert.IsNotNull(path.ReconstructionHints[0].Anchors);
            Assert.AreEqual(points[1], path.ReconstructionHints[1].Start);
            Assert.AreEqual(points[2], path.ReconstructionHints[1].End);
            Assert.AreEqual(0.1, path.ReconstructionHints[1].StartP);
            Assert.AreEqual(0.2, path.ReconstructionHints[1].EndP);
            Assert.IsNotNull(path.ReconstructionHints[1].Anchors);
            Assert.AreEqual(points[2], path.ReconstructionHints[2].Start);
            Assert.AreEqual(points[8], path.ReconstructionHints[2].End);
            Assert.AreEqual(0, path.ReconstructionHints[2].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[2].EndP);
            Assert.IsNotNull(path.ReconstructionHints[2].Anchors);
            Assert.AreEqual(points[8], path.ReconstructionHints[3].Start);
            Assert.AreEqual(points[10], path.ReconstructionHints[3].End);
            Assert.AreEqual(0.8, path.ReconstructionHints[3].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[3].EndP);
            Assert.IsNotNull(path.ReconstructionHints[3].Anchors);

            path.AddReconstructionHint(new ReconstructionHint(points[9], points[10], 0, new List<Vector2> {
                new(9,0),
                new(9.5, 1),
                new(10,0)
            }));

            Assert.AreEqual(5, path.ReconstructionHints.Count);
            Assert.AreEqual(points[0], path.ReconstructionHints[0].Start);
            Assert.AreEqual(points[1], path.ReconstructionHints[0].End);
            Assert.AreEqual(0, path.ReconstructionHints[0].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[0].EndP);
            Assert.IsNotNull(path.ReconstructionHints[0].Anchors);
            Assert.AreEqual(points[1], path.ReconstructionHints[1].Start);
            Assert.AreEqual(points[2], path.ReconstructionHints[1].End);
            Assert.AreEqual(0.1, path.ReconstructionHints[1].StartP);
            Assert.AreEqual(0.2, path.ReconstructionHints[1].EndP);
            Assert.IsNotNull(path.ReconstructionHints[1].Anchors);
            Assert.AreEqual(points[2], path.ReconstructionHints[2].Start);
            Assert.AreEqual(points[8], path.ReconstructionHints[2].End);
            Assert.AreEqual(0, path.ReconstructionHints[2].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[2].EndP);
            Assert.IsNotNull(path.ReconstructionHints[2].Anchors);
            Assert.AreEqual(points[8], path.ReconstructionHints[3].Start);
            Assert.AreEqual(points[9], path.ReconstructionHints[3].End);
            Assert.AreEqual(0.8, path.ReconstructionHints[3].StartP);
            Assert.AreEqual(0.9, path.ReconstructionHints[3].EndP);
            Assert.IsNotNull(path.ReconstructionHints[3].Anchors);
            Assert.AreEqual(points[9], path.ReconstructionHints[4].Start);
            Assert.AreEqual(points[10], path.ReconstructionHints[4].End);
            Assert.AreEqual(0, path.ReconstructionHints[4].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[4].EndP);
            Assert.IsNotNull(path.ReconstructionHints[4].Anchors);

            path.AddReconstructionHint(new ReconstructionHint(points[1], points[2], 0, null));

            Assert.AreEqual(5, path.ReconstructionHints.Count);
            Assert.AreEqual(points[0], path.ReconstructionHints[0].Start);
            Assert.AreEqual(points[1], path.ReconstructionHints[0].End);
            Assert.AreEqual(0, path.ReconstructionHints[0].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[0].EndP);
            Assert.IsNotNull(path.ReconstructionHints[0].Anchors);
            Assert.AreEqual(points[1], path.ReconstructionHints[1].Start);
            Assert.AreEqual(points[2], path.ReconstructionHints[1].End);
            Assert.AreEqual(0, path.ReconstructionHints[1].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[1].EndP);
            Assert.IsNull(path.ReconstructionHints[1].Anchors);
            Assert.AreEqual(points[2], path.ReconstructionHints[2].Start);
            Assert.AreEqual(points[8], path.ReconstructionHints[2].End);
            Assert.AreEqual(0, path.ReconstructionHints[2].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[2].EndP);
            Assert.IsNotNull(path.ReconstructionHints[2].Anchors);
            Assert.AreEqual(points[8], path.ReconstructionHints[3].Start);
            Assert.AreEqual(points[9], path.ReconstructionHints[3].End);
            Assert.AreEqual(0.8, path.ReconstructionHints[3].StartP);
            Assert.AreEqual(0.9, path.ReconstructionHints[3].EndP);
            Assert.IsNotNull(path.ReconstructionHints[3].Anchors);
            Assert.AreEqual(points[9], path.ReconstructionHints[4].Start);
            Assert.AreEqual(points[10], path.ReconstructionHints[4].End);
            Assert.AreEqual(0, path.ReconstructionHints[4].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[4].EndP);
            Assert.IsNotNull(path.ReconstructionHints[4].Anchors);
        }

        [TestMethod]
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

            Assert.AreEqual(5, path.ReconstructionHints.Count);
            Assert.AreEqual(points[0], path.ReconstructionHints[0].Start);
            Assert.AreEqual(points[1], path.ReconstructionHints[0].End);
            Assert.AreEqual(0, path.ReconstructionHints[0].StartP);
            Assert.AreEqual(0.1, path.ReconstructionHints[0].EndP);
            Assert.IsNotNull(path.ReconstructionHints[0].Anchors);
            Assert.AreEqual(points[1], path.ReconstructionHints[1].Start);
            Assert.AreEqual(points[2], path.ReconstructionHints[1].End);
            Assert.AreEqual(0, path.ReconstructionHints[1].StartP);
            Assert.AreEqual(0.5, path.ReconstructionHints[1].EndP);
            Assert.IsNotNull(path.ReconstructionHints[1].Anchors);
            Assert.AreEqual(points[2], path.ReconstructionHints[2].Start);
            Assert.AreEqual(points[3], path.ReconstructionHints[2].End);
            Assert.AreEqual(0, path.ReconstructionHints[2].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[2].EndP);
            Assert.IsNull(path.ReconstructionHints[2].Anchors);
            Assert.AreEqual(points[3], path.ReconstructionHints[3].Start);
            Assert.AreEqual(points[8], path.ReconstructionHints[3].End);
            Assert.AreEqual(1/6d, path.ReconstructionHints[3].StartP, Precision.DoubleEpsilon);
            Assert.AreEqual(1, path.ReconstructionHints[3].EndP);
            Assert.IsNotNull(path.ReconstructionHints[3].Anchors);
            Assert.AreEqual(points[8], path.ReconstructionHints[4].Start);
            Assert.AreEqual(points[10], path.ReconstructionHints[4].End);
            Assert.AreEqual(0.8, path.ReconstructionHints[4].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[4].EndP);
            Assert.IsNotNull(path.ReconstructionHints[4].Anchors);
        }

        [TestMethod]
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

            Assert.AreEqual(5, path.ReconstructionHints.Count);
            Assert.AreEqual(points[0], path.ReconstructionHints[0].Start);
            Assert.AreEqual(points[2], path.ReconstructionHints[0].End);
            Assert.AreEqual(0, path.ReconstructionHints[0].StartP);
            Assert.AreEqual(0.2, path.ReconstructionHints[0].EndP);
            Assert.IsNotNull(path.ReconstructionHints[0].Anchors);
            Assert.AreEqual(points[2], path.ReconstructionHints[1].Start);
            Assert.AreEqual(points[7], path.ReconstructionHints[1].End);
            Assert.AreEqual(0, path.ReconstructionHints[1].StartP);
            Assert.AreEqual(1 - 1/6d, path.ReconstructionHints[1].EndP, Precision.DoubleEpsilon);
            Assert.IsNotNull(path.ReconstructionHints[1].Anchors);
            Assert.AreEqual(points[7], path.ReconstructionHints[2].Start);
            Assert.AreEqual(points[8], path.ReconstructionHints[2].End);
            Assert.AreEqual(0, path.ReconstructionHints[2].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[2].EndP);
            Assert.IsNull(path.ReconstructionHints[2].Anchors);
            Assert.AreEqual(points[8], path.ReconstructionHints[3].Start);
            Assert.AreEqual(points[9], path.ReconstructionHints[3].End);
            Assert.AreEqual(0.5, path.ReconstructionHints[3].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[3].EndP);
            Assert.IsNotNull(path.ReconstructionHints[3].Anchors);
            Assert.AreEqual(points[9], path.ReconstructionHints[4].Start);
            Assert.AreEqual(points[10], path.ReconstructionHints[4].End);
            Assert.AreEqual(0.9, path.ReconstructionHints[4].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[4].EndP);
            Assert.IsNotNull(path.ReconstructionHints[4].Anchors);
        }

        [TestMethod]
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

            Assert.AreEqual(5, path.ReconstructionHints.Count);
            Assert.AreEqual(points[0], path.ReconstructionHints[0].Start);
            Assert.AreEqual(points[2], path.ReconstructionHints[0].End);
            Assert.AreEqual(0, path.ReconstructionHints[0].StartP);
            Assert.AreEqual(0.2, path.ReconstructionHints[0].EndP);
            Assert.IsNotNull(path.ReconstructionHints[0].Anchors);
            Assert.AreEqual(points[2], path.ReconstructionHints[1].Start);
            Assert.AreEqual(points[3], path.ReconstructionHints[1].End);
            Assert.AreEqual(0, path.ReconstructionHints[1].StartP);
            Assert.AreEqual(1/6d, path.ReconstructionHints[1].EndP, Precision.DoubleEpsilon);
            Assert.IsNotNull(path.ReconstructionHints[1].Anchors);
            Assert.AreEqual(points[3], path.ReconstructionHints[2].Start);
            Assert.AreEqual(points[7], path.ReconstructionHints[2].End);
            Assert.AreEqual(0, path.ReconstructionHints[2].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[2].EndP);
            Assert.IsNull(path.ReconstructionHints[2].Anchors);
            Assert.AreEqual(points[7], path.ReconstructionHints[3].Start);
            Assert.AreEqual(points[8], path.ReconstructionHints[3].End);
            Assert.AreEqual(1 - 1/6d, path.ReconstructionHints[3].StartP, Precision.DoubleEpsilon);
            Assert.AreEqual(1, path.ReconstructionHints[3].EndP);
            Assert.IsNotNull(path.ReconstructionHints[3].Anchors);
            Assert.AreEqual(points[8], path.ReconstructionHints[4].Start);
            Assert.AreEqual(points[10], path.ReconstructionHints[4].End);
            Assert.AreEqual(0.8, path.ReconstructionHints[4].StartP);
            Assert.AreEqual(1, path.ReconstructionHints[4].EndP);
            Assert.IsNotNull(path.ReconstructionHints[4].Anchors);
        }
    }
}