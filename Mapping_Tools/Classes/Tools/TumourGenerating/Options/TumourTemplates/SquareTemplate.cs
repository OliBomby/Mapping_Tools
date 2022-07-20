using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates {
    public class SquareTemplate : TumourTemplateBase {
        private const double SideMargin = 0.0001;

        public override Vector2 GetOffset(double t) {
            return t < SideMargin ? -t * Width / SideMargin * Vector2.UnitY : t > 1 - SideMargin ? (t - 1) * Width / SideMargin * Vector2.UnitY : -Width * Vector2.UnitY;
        }

        public override double GetLength() {
            var marginLen = Math.Sqrt(Width * Width + Length * Length * SideMargin * SideMargin);
            return 2 * marginLen + Length * (1 - 2 * SideMargin);
        }

        public override double GetDefaultSpan() {
            return Length;
        }

        public override int GetDetailLevel() {
            return 1;
        }

        public override IEnumerable<double> GetCriticalPoints() {
            yield return SideMargin;
            yield return 1 - SideMargin;
        }

        public override List<Vector2> GetReconstructionHint() {
            return new List<Vector2> { Vector2.Zero, new(SideMargin * Length, -Width), new((1 - SideMargin) * Length, -Width), Length * Vector2.UnitX };
        }

        public override PathType GetReconstructionHintPathType() {
            return PathType.Linear;
        }

        public override Func<double, double> GetDistanceRelation() {
            return t => DistanceRelation(t, Length, Width);
        }

        private static double DistanceRelation(double t, double scaleX, double scaleY) {
            var marginLen = Math.Sqrt(scaleY * scaleY + scaleX * scaleX * SideMargin * SideMargin);
            var len = 2 * marginLen + scaleX * (1 - 2 * SideMargin);
            return t < SideMargin ? t / SideMargin * marginLen / len :
                t > 1 - SideMargin ? 1 + (t - 1) / SideMargin * marginLen / len :
                (t - SideMargin) * scaleX / len + marginLen / len;
        }
    }
}