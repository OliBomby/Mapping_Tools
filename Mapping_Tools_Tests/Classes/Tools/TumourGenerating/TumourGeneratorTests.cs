using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen;
using Mapping_Tools.Classes.Tools.TumourGenerating;
using Mapping_Tools.Classes.Tools.TumourGenerating.Options;
using NUnit.Framework;

namespace Mapping_Tools_Tests.Classes.Tools.TumourGenerating {
    [TestFixture]
    public class TumourGeneratorTests {
        private HitObject hitObject;
        private PathWithHints pathWithHints;

        [SetUp]
        public void Initialize() {
            hitObject = new HitObject("0,0,384,2,0,B|192:0|192:0|192:192,1,384");
            pathWithHints = PathHelper.CreatePathWithHints(hitObject.GetSliderPath());
        }

        [Test]
        public void TestPlaceTumour() {
            const int res = 10;
            var tumourGenerator = new TumourGenerator {
                Resolution = res
            };
            var tumourLayer = TumourLayer.GetDefaultLayer();
            tumourLayer.TumourLength = TumourLayer.GetGraphState(10);
            tumourLayer.TumourScale = TumourLayer.GetGraphState(5);
            const int layer = 0;
            const double startT = 0;
            const double endT = 1;

            var current = pathWithHints.Path.First;
            var start = PathHelper.FindFirstOccurrenceExact(current, 100, epsilon:0.5);
            var end = PathHelper.FindLastOccurrenceExact(start, 110, epsilon:0.5);
            var end2 = PathHelper.FindLastOccurrenceExact(start, 115, epsilon:0.5);

            tumourGenerator.PlaceTumour(pathWithHints, tumourLayer, layer, start, end, startT, endT, 100, 110, false, 384);

            current = start;
            while (current is not null && current != end) {
                var pos = current.Value.Pos;

                switch (pos.X) {
                    case >= 100 and <= 105:
                        Assert.AreEqual(-pos.X + 100, pos.Y, Precision.DoubleEpsilon);
                        break;
                    case > 105 and <= 110:
                        Assert.AreEqual(pos.X - 110, pos.Y, Precision.DoubleEpsilon);
                        break;
                }

                current = current.Next;
            }

            var mid = PathHelper.FindFirstOccurrence(start, 105);
            Assert.AreEqual(105, mid.Value.CumulativeLength, Precision.DoubleEpsilon);

            // Check hint
            Assert.AreEqual(4, pathWithHints.ReconstructionHints.Count);

            // Add another overlapping tumour
            tumourGenerator.PlaceTumour(pathWithHints, tumourLayer, layer, mid, end2, startT, endT, 105, 115, false, 384);

            current = start;
            while (current is not null && current != end2) {
                var pos = current.Value.Pos;

                switch (pos.X) {
                    case >= 100 and <= 105:
                        Assert.AreEqual(-pos.X + 100, pos.Y, Precision.DoubleEpsilon);
                        break;
                    case > 105 and <= 110:
                        Assert.AreEqual(-5, pos.Y, Precision.DoubleEpsilon);
                        break;
                    case > 110 and <= 115:
                        Assert.AreEqual(pos.X - 115, pos.Y, Precision.DoubleEpsilon);
                        break;
                }

                current = current.Next;
            }

            // Check hint
            Assert.AreEqual(6, pathWithHints.ReconstructionHints.Count);

            Assert.AreEqual(-1, pathWithHints.ReconstructionHints[0].Layer);
            Assert.AreEqual(layer, pathWithHints.ReconstructionHints[1].Layer);
            Assert.IsNotNull(pathWithHints.ReconstructionHints[1].Anchors);
            Assert.AreEqual(layer, pathWithHints.ReconstructionHints[2].Layer);
            Assert.IsNull(pathWithHints.ReconstructionHints[2].Anchors);
            Assert.AreEqual(layer, pathWithHints.ReconstructionHints[3].Layer);
            Assert.IsNotNull(pathWithHints.ReconstructionHints[3].Anchors);
            Assert.AreEqual(-1, pathWithHints.ReconstructionHints[4].Layer);
            Assert.AreEqual(-1, pathWithHints.ReconstructionHints[5].Layer);

            var reconstructor = new Reconstructor();
            var (anchors, pathType) = reconstructor.Reconstruct(pathWithHints);

            Assert.AreEqual(PathType.Bezier, pathType);
            Assert.AreEqual(12, anchors.Count);

            AssertEqual(new Vector2(0, 0), anchors[0]);
            AssertEqual(new Vector2(100, 0), anchors[1]);
            AssertEqual(new Vector2(100, 0), anchors[2]);
            AssertEqual(new Vector2(105, -5), anchors[3]);
            AssertEqual(new Vector2(105, -5), anchors[4]);
            AssertEqual(new Vector2(110, -5), anchors[5]);
            AssertEqual(new Vector2(110, -5), anchors[6]);
            AssertEqual(new Vector2(115, 0), anchors[7]);
            AssertEqual(new Vector2(115, 0), anchors[8]);
            AssertEqual(new Vector2(192, 0), anchors[9]);
            AssertEqual(new Vector2(192, 0), anchors[10]);
            AssertEqual(new Vector2(192, 192), anchors[11]);
        }

        private void AssertEqual(Vector2 l, Vector2 r) {
            Assert.AreEqual(l.X, r.X);
            Assert.AreEqual(l.Y, r.Y);
        }
    }
}