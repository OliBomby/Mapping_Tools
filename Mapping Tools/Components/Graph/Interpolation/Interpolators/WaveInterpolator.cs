using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Wave")]
    public class WaveInterpolator : CustomInterpolator {
        public string Name => "Wave";

        public WaveInterpolator() {
            InterpolationFunction = Function;
        }

        public double Function(double t) {
            var cycles = Math.Round((1 - Math.Abs(MathHelper.Clamp(P, -1, 1))) * 50) + 0.5;

            return P < 0 ? 
                TriangleWave(t, 1 / cycles) : 
                SineWave(t * cycles * 2 * Math.PI);
        }

        private static double SineWave(double t) {
            return (Math.Sin(t - Math.PI / 2) + 1) / 2;
        }

        private static double TriangleWave(double t, double T) {
            var modT = t % T;
            return modT < T / 2 ? 2 * modT / T : 2 - 2 * modT / T;
        }
    }
}