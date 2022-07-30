using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGenerating.Options.TumourTemplates {
    public class SquareTemplate : TumourTemplateBase, IRequireInit {
        private const double MinSideMargin = 0.0001;

        private double sideMargin;

        public override bool NeedsParameter => true;

        public override Vector2 GetOffset(double t) {
            return t < sideMargin ? -t * Width / sideMargin * Vector2.UnitY : t > 1 - sideMargin ? (t - 1) * Width / sideMargin * Vector2.UnitY : -Width * Vector2.UnitY;
        }

        public override double GetLength() {
            var marginLen = Math.Sqrt(Width * Width + Length * Length * sideMargin * sideMargin);
            return 2 * marginLen + Length * (1 - 2 * sideMargin);
        }

        public override double GetDefaultSpan() {
            return Length;
        }

        public override int GetDetailLevel() {
            return 1;
        }

        public override IEnumerable<double> GetCriticalPoints() {
            yield return sideMargin;
            yield return 1 - sideMargin;
        }

        public override List<Vector2> GetReconstructionHint() {
            return new List<Vector2> { Vector2.Zero, new(sideMargin * Length, -Width), new((1 - sideMargin) * Length, -Width), Length * Vector2.UnitX };
        }

        public override PathType GetReconstructionHintPathType() {
            return PathType.Linear;
        }

        public override Func<double, double> GetDistanceRelation() {
            return t => DistanceRelation(t, Length, Width, sideMargin);
        }

        private static double DistanceRelation(double t, double scaleX, double scaleY, double sideMargin) {
            var marginLen = Math.Sqrt(scaleY * scaleY + scaleX * scaleX * sideMargin * sideMargin);
            var len = 2 * marginLen + scaleX * (1 - 2 * sideMargin);
            return t < sideMargin ? t / sideMargin * marginLen / len :
                t > 1 - sideMargin ? 1 + (t - 1) / sideMargin * marginLen / len :
                (t - sideMargin) * scaleX / len + marginLen / len;
        }

        public void Init() {
            sideMargin = Precision.AlmostEquals(Length, 0)
                ? MinSideMargin
                : MathHelper.Clamp(Parameter / Length, MinSideMargin, 0.5);
        }
    }
}