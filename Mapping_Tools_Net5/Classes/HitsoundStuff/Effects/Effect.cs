using System;
using NAudio.Wave;

namespace Mapping_Tools.Classes.HitsoundStuff.Effects {
    public abstract class Effect : ISampleProvider {
        // helper base methods these are primarily to enable derived classes to use a similar
        // syntax to REAPER's JS effects
        protected const float log2db = 8.6858896380650365530225783783321f; // 20 / ln(10)
        protected const float db2log = 0.11512925464970228420089957273422f; // ln(10) / 20 
        private bool paramsChanged;
        private readonly ISampleProvider source;

        protected Effect(ISampleProvider source) {
            this.source = source;
            SampleRate = source.WaveFormat.SampleRate;
        }

        public float SampleRate { get; set; }
        public abstract string Name { get; }
        protected static float PI => (float) Math.PI;

        public int Read(float[] samples, int offset, int count) {
            if (paramsChanged) {
                ParamsChanged();
                paramsChanged = false;
            }

            var samplesAvailable = source.Read(samples, offset, count);
            Block(samplesAvailable);
            if (WaveFormat.Channels == 1)
                for (var n = 0; n < samplesAvailable; n++) {
                    var right = 0.0f;
                    Sample(ref samples[n], ref right);
                }
            else if (WaveFormat.Channels == 2)
                for (var n = 0; n < samplesAvailable; n += 2)
                    Sample(ref samples[n], ref samples[n + 1]);

            return samplesAvailable;
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        protected void RegisterParameters(params EffectParameter[] parameters) {
            paramsChanged = true;
            foreach (var param in parameters) param.ValueChanged += (s, a) => paramsChanged = true;
        }

        protected abstract void ParamsChanged();

        protected static float min(float a, float b) {
            return Math.Min(a, b);
        }

        protected static float max(float a, float b) {
            return Math.Max(a, b);
        }

        protected static float abs(float a) {
            return Math.Abs(a);
        }

        protected static float exp(float a) {
            return (float) Math.Exp(a);
        }

        protected static float sqrt(float a) {
            return (float) Math.Sqrt(a);
        }

        protected static float sin(float a) {
            return (float) Math.Sin(a);
        }

        protected static float tan(float a) {
            return (float) Math.Tan(a);
        }

        protected static float cos(float a) {
            return (float) Math.Cos(a);
        }

        protected static float pow(float a, float b) {
            return (float) Math.Pow(a, b);
        }

        protected static float sign(float a) {
            return Math.Sign(a);
        }

        protected static float log(float a) {
            return (float) Math.Log(a);
        }

        /// <summary>
        ///     called before each block is processed
        /// </summary>
        /// <param name="samplesblock">number of samples in this block</param>
        public virtual void Block(int samplesblock) { }

        /// <summary>
        ///     called for each sample
        /// </summary>
        protected abstract void Sample(ref float spl0, ref float spl1);

        public override string ToString() {
            return Name;
        }
    }
}