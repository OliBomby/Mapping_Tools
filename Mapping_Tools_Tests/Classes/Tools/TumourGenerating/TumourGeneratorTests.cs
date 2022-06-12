using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.ToolHelpers.Sliders.Newgen;
using Mapping_Tools.Classes.Tools.TumourGenerating;
using Mapping_Tools.Classes.Tools.TumourGenerating.Enums;
using Mapping_Tools.Classes.Tools.TumourGenerating.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.Tools.TumourGenerating {
    [TestClass]
    public class TumourGeneratorTests {
        private HitObject hitObject;
        private PathWithHints pathWithHints;

        [TestInitialize]
        public void Initialize() {
            hitObject = new HitObject("0,0,384,2,0,B|192:0|192:0|192:192,1,384");
            pathWithHints = PathHelper.CreatePathWithHints(hitObject.GetSliderPath());
        }

        [TestMethod]
        public void TestPlaceTumour() {
            const int res = 10;
            var tumourGenerator = new TumourGenerator {
                WrappingMode = WrappingMode.Simple,
                Resolution = res
            };
            var tumourLayer = TumourLayer.GetDefaultLayer();
            tumourLayer.TumourLength = TumourLayer.GetGraphState(10);
            tumourLayer.TumourScale = TumourLayer.GetGraphState(10);
            const int layer = 0;
            const double startT = 0;
            const double endT = 1;

            var current = pathWithHints.Path.First;
            var start = PathHelper.FindFirstOccurrenceExact(current, 100, epsilon:0.5);
            var end = PathHelper.FindLastOccurrenceExact(start, 110, epsilon:0.5);
            var end2 = PathHelper.FindLastOccurrenceExact(start, 115, epsilon:0.5);

            tumourGenerator.PlaceTumour(pathWithHints, tumourLayer, layer, start, end, startT, endT, false);

            current = start;
            var count = 1;
            while (current is not null && current != end) {
                var pos = current.Value.Pos;
                count++;

                switch (pos.X) {
                    case >= 100 and <= 105:
                        Assert.AreEqual(-pos.X + 100, pos.Y, Precision.DOUBLE_EPSILON);
                        break;
                    case > 105 and <= 110:
                        Assert.AreEqual(pos.X - 110, pos.Y, Precision.DOUBLE_EPSILON);
                        break;
                }

                current = current.Next;
            }
            
            Assert.IsTrue(count >= 2 + res);
            var mid = PathHelper.FindFirstOccurrence(start, 105);
            Assert.AreEqual(105, mid.Value.CumulativeLength, Precision.DOUBLE_EPSILON);

            // Check hint
            Assert.AreEqual(4, pathWithHints.ReconstructionHints.Count);

            // Add another overlapping tumour
            tumourGenerator.PlaceTumour(pathWithHints, tumourLayer, layer, mid, end2, startT, endT, false);

            current = start;
            count = 1;
            while (current is not null && current != end2) {
                var pos = current.Value.Pos;
                count++;

                switch (pos.X) {
                    case >= 100 and <= 105:
                        Assert.AreEqual(-pos.X + 100, pos.Y, Precision.DOUBLE_EPSILON);
                        break;
                    case > 105 and <= 110:
                        Assert.AreEqual(-5, pos.Y, Precision.DOUBLE_EPSILON);
                        break;
                    case > 110 and <= 115:
                        Assert.AreEqual(pos.X - 115, pos.Y, Precision.DOUBLE_EPSILON);
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
        }
    }
}