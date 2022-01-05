using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.Tools.TumourGeneratorStuff;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools_Tests.Classes.Tools.TumourGenerator {
    [TestClass]
    public class PathHelperTests {
        [TestMethod]
        public void InterpolateTest() {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            var path = new LinkedList<PathPoint>(new[] {
                new PathPoint(new Vector2(-9, 0), new Vector2(1, 0), 0, 0),
                new PathPoint(new Vector2(1, 0), new Vector2(1, 1), 1, 1),
                new PathPoint(new Vector2(2, 1), new Vector2(1, 0), 2, 3),
                new PathPoint(new Vector2(12, 1), new Vector2(1, 0), 1, 4),
            });

            var p1 = path.First.Next;
            PathHelper.Interpolate(p1, Enumerable.Range(1, 9).Select(i => i / 10d));

            foreach (var p in path) {
                Debug.WriteLine(p.Pos.ToInvariant());
            }

            var path2 = new LinkedList<PathPoint>(new[] {
                new PathPoint(new Vector2(-9, 0), new Vector2(1, 0), 0, 0),
                new PathPoint(new Vector2(1, 0), new Vector2(1, 1), 1, 1),
                new PathPoint(new Vector2(2, 1), new Vector2(1, 0), 2, 3, 0, true),
                new PathPoint(new Vector2(12, 1), new Vector2(1, 0), 1, 4),
            });

            var p2 = path2.First.Next;
            PathHelper.Interpolate(p2, Enumerable.Range(1, 9).Select(i => i / 10d));

            foreach (var p in path2) {
                Debug.WriteLine(p.Pos.ToInvariant());
            }
        }
    }
}
