using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates {
    public class CircleTemplate : ITumourTemplate {
        public Vector2 GetOffset(double t) {
            return t < 0.5 ? -2 * t * Vector2.UnitY : 2 * (-1 + t) * Vector2.UnitY;
        }

        public double GetLength() {
            return 2.5;
        }

        public double GetDefaultSpan() {
            return 1;
        }

        public IEnumerable<double> GetCriticalPoints() {
            yield return 0.5;
        }

        public List<Vector2> GetReconstructionHint() {
            return new List<Vector2> { Vector2.Zero, new(1, -1), Vector2.UnitX };
        }

        public PathType GetReconstructionHintPathType() {
            return PathType.PerfectCurve;
        }

        public Func<double, double> GetDistanceRelation(double _) {
            return null;
        }
    }
}