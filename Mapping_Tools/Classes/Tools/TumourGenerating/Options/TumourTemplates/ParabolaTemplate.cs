using System;
using System.Collections.Generic;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates {
    public class ParabolaTemplate : ITumourTemplate {
        public Vector2 GetOffset(double t) {
            return (4 * t * t - 4 * t) * Vector2.UnitY;
        }

        public double GetLength() {
            return 3;
        }

        public double GetDefaultSpan() {
            return 1;
        }

        public IEnumerable<double> GetCriticalPoints() {
            return Enumerable.Empty<double>();
        }

        public List<Vector2> GetReconstructionHint() {
            return new List<Vector2> { Vector2.Zero, new(0.5, -1), Vector2.UnitX };
        }

        public PathType GetReconstructionHintPathType() {
            return PathType.Bezier;
        }

        public Func<double, double> GetDistanceRelation(double _) {
            return null;  // TODO: This is not linear!!!
        }
    }
}