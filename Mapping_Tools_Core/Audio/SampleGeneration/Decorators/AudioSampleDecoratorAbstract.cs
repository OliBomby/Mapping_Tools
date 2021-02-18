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
            }

            if (exporter is IPathAudioSampleExporter pathAudioSampleExporter) {
                pathAudioSampleExporter.CanCopyPaste = pathAudioSampleExporter.CanCopyPaste && !HasEffect();
            }
        }

        public virtual ISampleProvider GetSampleProvider() {
            return Decorate(BaseAudioGenerator.GetSampleProvider());
        }

        public virtual void PreLoadSample() {
            BaseAudioGenerator.PreLoadSample();
        }

        public abstract bool HasEffect();

        protected abstract ISampleProvider Decorate(ISampleProvider sampleProvider);
    }
}