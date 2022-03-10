using System;
using System.Collections.Generic;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.Tools.TumourGeneratorStuff.Options.TumourTemplates {
    public class TriangleTemplate : ITumourTemplate {
        public Vector2 GetOffset(double t) {
            return t < 0.5 ? -t * Vector2.UnitY : (-1 + t) * Vector2.UnitY;
        }

        public double GetLength() {
            return 2 * Math.Sqrt(0.5);
        }

        public IEnumerable<double> GetCriticalPoints() {
            yield return 0.5;
        }

        public List<Vector2> GetReconstructionHint() {
            return new() { Vector2.Zero, new Vector2(0.5, -0.5), Vector2.UnitX };
        }
    }
}