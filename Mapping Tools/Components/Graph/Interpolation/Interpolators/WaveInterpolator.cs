using System;
using System.ComponentModel;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Components.Graph.Interpolation.Interpolators {
    [DisplayName("Wave")]
    public class WaveInterpolator : CustomInterpolator, IDerivableInterpolator, IIntegrableInterpolator {
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
            return (-Math.Cos(t) + 1) / 2;
        }

        private static double SineWaveDerivative(double t) {
            return Math.Sin(t) / 2;
        }

        private static double SineWavePrimitive(double t, double c) {
            return t / 2 - Math.Sin(2 * Math.PI * c * t) / (4 * Math.PI * c);
        }

        private static double TriangleWave(double t, double T) {
            var modT = t % T;
            return modT < T / 2 ? 2 * modT / T : 2 - 2 * modT / T;
        }

        // This is a square wave
        private static double TriangleWaveDerivative(double t, double T) {
            var modT = t % T;
            return modT < T / 2 ? 2 / T : -2 / T;
        }

        /// <summary>
        /// https://math.stackexchange.com/questions/178079/integration-of-sawtooth-square-and-triangle-wave-functions
        /// </summary>
        /// <param name="t"></param>
        /// <param name="T"></param>
        /// <returns></returns>
        private static double TriangleWaveIntegral(double t, double T) {
            var modT = t % T;
            var n = Math.Floor(t / T);
            var integral = modT < T / 2 ? Math.Pow(modT, 2) / T : 2 * modT - Math.Pow(modT, 2) / T - T / 2;
            return n * T * 0.5 + integral;
        }

        public double GetIntegral(double t1, double t2) {
            var cycles = Math.Round((1 - Math.Abs(MathHelper.Clamp(P, -1, 1))) * 50) + 0.5;

            return P < 0 ? 
                TriangleWaveIntegral(t2, 1 / cycles) - TriangleWaveIntegral(t1, 1 / cycles) : 
                SineWavePrimitive(t2, cycles) - SineWavePrimitive(t1, cycles);
        }

        public double GetDerivative(double t) {
            var cycles = Math.Round((1 - Math.Abs(MathHelper.Clamp(P, -1, 1))) * 50) + 0.5;

            return P < 0 ? 
                TriangleWaveDerivative(t, 1 / cycles) : 
                SineWaveDerivative(t * cycles * 2 * Math.PI);
        }
    }
}