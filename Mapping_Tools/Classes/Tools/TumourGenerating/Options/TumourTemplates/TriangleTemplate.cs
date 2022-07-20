using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates {
    public class TriangleTemplate : TumourTemplateBase {
        public override Vector2 GetOffset(double t) {
            return t < 0.5 ? -2 * Width * t * Vector2.UnitY : 2 * Width * (-1 + t) * Vector2.UnitY;
        }

        public override double GetLength() {
            return 2 * Math.Sqrt(0.25 * Length * Length + Width * Width);
        }

        public override double GetDefaultSpan() {
            return Length;
        }

        public override IEnumerable<double> GetCriticalPoints() {
            yield return 0.5;
        }

        public override int GetDetailLevel() {
            return 1;
        }

        public override List<Vector2> GetReconstructionHint() {
            return new List<Vector2> { Vector2.Zero, new(0.5 * Length, -Width), Length * Vector2.UnitX };
        }

        public override PathType GetReconstructionHintPathType() {
            return PathType.Linear;
        }

        public override Func<double, double> GetDistanceRelation() {
            return null;
        }
    }
}