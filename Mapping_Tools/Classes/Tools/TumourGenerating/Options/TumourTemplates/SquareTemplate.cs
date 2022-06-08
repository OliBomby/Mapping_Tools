using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates {
    public class SquareTemplate : ITumourTemplate {
        public Vector2 GetOffset(double t) {
            return t is 0 or 1 ? Vector2.Zero : Vector2.UnitY;
        }

        public double GetLength() {
            return 3;
        }

        public double GetDefaultSpan() {
            return 1;
        }

        public IEnumerable<double> GetCriticalPoints() {
            yield return 0.0001;
            yield return 0.9999;
        }

        public List<Vector2> GetReconstructionHint() {
            return new List<Vector2> { Vector2.Zero, new(0, -1), new(1, -1), Vector2.UnitX };
        }

        public PathType GetReconstructionHintPathType() {
            return PathType.Linear;
        }
    }
}