using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates {
    public class SquareTemplate : ITumourTemplate {
        private const double SideMargin = 0.0001;

        public Vector2 GetOffset(double t) {
            return t < SideMargin ? -t / SideMargin * Vector2.UnitY : t > 1 - SideMargin ? (t - 1) / SideMargin * Vector2.UnitY : -Vector2.UnitY;
        }

        public double GetLength() {
            return 3;
        }

        public double GetDefaultSpan() {
            return 1;
        }

        public IEnumerable<double> GetCriticalPoints() {
            yield return SideMargin;
            yield return 1 - SideMargin;
        }

        public List<Vector2> GetReconstructionHint() {
            return new List<Vector2> { Vector2.Zero, new(SideMargin, -1), new(1 - SideMargin, -1), Vector2.UnitX };
        }

        public PathType GetReconstructionHintPathType() {
            return PathType.Linear;
        }

        public Func<double, double> GetDistanceRelation(double scaleY) {
            return double.IsNaN(scaleY) ? null : t => DistanceRelation(t, scaleY);
        }

        private static double DistanceRelation(double t, double scaleY) {
            var marginLen = Math.Sqrt(scaleY * scaleY + SideMargin * SideMargin);
            var len = 2 * marginLen + 1 - 2 * SideMargin;
            return t < SideMargin ? t / SideMargin * marginLen / len :
                t > 1 - SideMargin ? 1 + (t - 1) / SideMargin * marginLen / len :
                (t - SideMargin) / len + marginLen / len;
        }
    }
}