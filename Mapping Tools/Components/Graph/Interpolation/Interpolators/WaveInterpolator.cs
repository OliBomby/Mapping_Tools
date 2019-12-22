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
            var cycles = Math.Round((1 - Math.Abs(MathHelper.Clamp(P, -1, 1))) * 50);

            if (P < 0) {
                return SharpWave((cycles + 0.5) * t);
            }

            return (Math.Sin((cycles * 2 + 1) * Math.PI * t - Math.PI / 2) + 1) / 2;
        }

        private static double SharpWave(double t) {
            return 1 - 2 * Math.Abs(Math.Truncate(t) - t + 0.5);
        }
    }
}