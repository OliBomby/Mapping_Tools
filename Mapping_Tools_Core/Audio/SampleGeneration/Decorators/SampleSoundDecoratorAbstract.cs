using Mapping_Tools_Core.Audio.Exporting;
using NAudio.Wave;

namespace Mapping_Tools_Core.Audio.SampleGeneration.Decorators {
    public abstract class SampleDecoratorAbstract : ISampleGenerator {
        private readonly ISampleGenerator baseGenerator;

        protected SampleDecoratorAbstract(ISampleGenerator baseGenerator) {
            this.baseGenerator = baseGenerator;
        }

        protected abstract ISampleProvider Decorate(ISampleProvider baseSampleProvider);

        public bool Equals(ISampleGenerator other) {
            throw new System.NotImplementedException();
        }

        public object Clone() {
            throw new System.NotImplementedException();
        }

        public bool IsValid() {
            return baseGenerator.IsValid();
        }

        public string GetName() {
            return baseGenerator.GetName();
        }

        public void ToExporter(ISampleExporter exporter) {
            baseGenerator.ToExporter(exporter);
        }
    }
}