using Mapping_Tools.Classes.MathUtil;
using System;
using System.ComponentModel;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Parabola")]
    [VerticalMirrorInterpolator]
    public class ParabolaInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator {
        public string Name => "Parabola";

        public ParabolaInterpolator() {
            InterpolationFunction = Function;
        }

        public double Function(double t) {
            var p = MathHelper.Clamp(P, -1, 1);
            return -p * Math.Pow(t, 2) + (p + 1) * t;
        }

        public double GetDerivative(double t) {
            var p = MathHelper.Clamp(P, -1, 1);
            return -2 * p * t + p + 1;
        }

        public double GetIntegral(double t1, double t2) {
            return Primitive(t2) - Primitive(t1);
        }

        private double Primitive(double t) {
            var p = MathHelper.Clamp(P, -1, 1);
            return 1d / 3 * -p * Math.Pow(t, 3) + 0.5 * (p + 1) * Math.Pow(t, 2);
        }
    }
}