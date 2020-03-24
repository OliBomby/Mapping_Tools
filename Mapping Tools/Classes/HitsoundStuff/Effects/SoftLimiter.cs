using NAudio.Wave;

namespace Mapping_Tools.Classes.HitsoundStuff.Effects {
    public class SoftLimiter : Effect {
        private readonly float a = 1.017f;

        private readonly float amp_dB = 8.6562f;
        private readonly float b = -0.025f;
        private readonly float baseline_threshold_dB = -9f;
        private float boost_dB;
        private float limit_dB;
        private float threshold_dB;

        public SoftLimiter(ISampleProvider source) : base(source) {
            RegisterParameters(Boost, Brickwall);
        }

        public override string Name => "Soft Clipper/ Limiter";

        public EffectParameter Boost { get; } = new EffectParameter(0f, 0f, 18f, "Boost");
        public EffectParameter Brickwall { get; } = new EffectParameter(-0.1f, -3.0f, 1f, "Output Brickwall(dB)");

        protected override void ParamsChanged() {
            boost_dB = Boost.CurrentValue;
            limit_dB = Brickwall.CurrentValue;
            threshold_dB = baseline_threshold_dB + limit_dB;
        }

        protected override void Sample(ref float spl0, ref float spl1) {
            var dB0 = amp_dB * log(abs(spl0)) + boost_dB;
            var dB1 = amp_dB * log(abs(spl1)) + boost_dB;

            if (dB0 > threshold_dB) {
                var over_dB = dB0 - threshold_dB;
                over_dB = a * over_dB + b * over_dB * over_dB;
                dB0 = min(threshold_dB + over_dB, limit_dB);
            }

            if (dB1 > threshold_dB) {
                var over_dB = dB1 - threshold_dB;
                over_dB = a * over_dB + b * over_dB * over_dB;
                dB1 = min(threshold_dB + over_dB, limit_dB);
            }

            spl0 = exp(dB0 / amp_dB) * sign(spl0);
            spl1 = exp(dB1 / amp_dB) * sign(spl1);
        }
    }
}