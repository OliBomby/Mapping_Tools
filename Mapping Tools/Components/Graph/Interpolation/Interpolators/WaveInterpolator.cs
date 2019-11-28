using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Wave")]
    public class WaveInterpolator : IGraphInterpolator {
        public string Name => "Wave";

        public double GetInterpolation(double t, double h1, double h2, double parameter) {
            var cycles = Math.Round((1 - Math.Abs(MathHelper.Clamp(parameter, -1, 1))) * 50);

            if (parameter < 0) {
                return h1 + (h2 - h1) * SharpWave((cycles + 0.5) * t);
            }

            return h1 + (h2 - h1) * (Math.Sin((cycles * 2 + 1) * Math.PI * t - Math.PI / 2) + 1) / 2;
        }

        private static double SharpWave(double t) {
            return 1 - 2 * Math.Abs(Math.Truncate(t) - t + 0.5);
        }
    }
}