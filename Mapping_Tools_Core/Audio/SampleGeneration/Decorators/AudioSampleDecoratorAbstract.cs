using Mapping_Tools_Core.Audio.Exporting;
using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.SampleGeneration.Decorators {
    public abstract class AudioSampleDecoratorAbstract : ISampleDecorator, IAudioSampleGenerator {
        public IAudioSampleGenerator BaseAudioGenerator { get; }

        public ISampleGenerator BaseGenerator => BaseAudioGenerator;

        protected AudioSampleDecoratorAbstract(IAudioSampleGenerator baseAudioGenerator) {
            BaseAudioGenerator = baseAudioGenerator;
        }

        public virtual bool Equals(ISampleGenerator other) {
            if (other is ISampleDecorator sampleDecorator)
                return !HasEffect() && !sampleDecorator.HasEffect() &&
                       BaseAudioGenerator.Equals(sampleDecorator.BaseGenerator);
            return !HasEffect() && BaseGenerator.Equals(other);
        }

        public abstract object Clone();

        public virtual bool IsValid() {
            return BaseAudioGenerator.IsValid();
        }

        public string GetName() {
            return HasEffect() ?
                BaseAudioGenerator.GetName() + GetNameExtension() :
                BaseAudioGenerator.GetName();
        }

        /// <summary>
        /// String to add to the end of the name to represent the decoration.
        /// </summary>
        /// <returns></returns>
        protected abstract string GetNameExtension();

        public virtual void ToExporter(ISampleExporter exporter) {
            BaseAudioGenerator.ToExporter(exporter);

            if (exporter is IAudioSampleExporter audioSampleExporter) {
                var sampleProvider = audioSampleExporter.PopAudio();

                audioSampleExporter.AddAudio(Decorate(sampleProvider));

                audioSampleExporter.ClippingPossible = audioSampleExporter.ClippingPossible ||
                                                       HasClippingPossible();

                audioSampleExporter.BlankSample = audioSampleExporter.BlankSample &&
                                                       !HasAddedAudio();
            }

            if (exporter is IPathAudioSampleExporter pathAudioSampleExporter) {
                // Either this decorator didn't have any effect on the audio
                // or the audio is blank so the decorator couldn't have any effect regardless.
                pathAudioSampleExporter.CanCopyPaste = pathAudioSampleExporter.CanCopyPaste && 
                                                       (!HasEffect() || pathAudioSampleExporter.BlankSample);
            }
        }

        public virtual ISampleProvider GetSampleProvider() {
            return Decorate(BaseAudioGenerator.GetSampleProvider());
        }

        public virtual double GetAmplitudeFactor() {
            return BaseAudioGenerator.GetAmplitudeFactor();
        }

        public virtual void PreloadSample() {
            BaseAudioGenerator.PreloadSample();
        }

        public abstract bool HasEffect();

        /// <summary>
        /// Determines whether the decorator amplifies the audio signal or adds samples
        /// in a way such that clipping becomes possible.
        /// </summary>
        /// <returns></returns>
        protected virtual bool HasClippingPossible() {
            return false;
        }

        /// <summary>
        /// Determines whether the decorator concatenates additional audio samples
        /// to the original audio.
        /// </summary>
        /// <returns></returns>
        protected virtual bool HasAddedAudio() {
            return false;
        }

        protected abstract ISampleProvider Decorate(ISampleProvider sampleProvider);
    }
}