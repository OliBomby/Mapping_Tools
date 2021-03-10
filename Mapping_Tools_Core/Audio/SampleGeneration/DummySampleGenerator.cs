using Mapping_Tools_Core.Audio.Exporting;

namespace Mapping_Tools_Core.Audio.SampleGeneration {
    /// <summary>
    /// A invalid sample generator that doesn't generate anything.
    /// </summary>
    public class DummySampleGenerator : ISampleGenerator {
        public bool Equals(ISampleGenerator other) {
            return other is DummySampleGenerator;
        }

        public object Clone() {
            return new DummySampleGenerator();
        }

        public bool IsValid() {
            return false;
        }

        public string GetName() {
            return @"dummy";
        }

        public void ToExporter(ISampleExporter exporter) { }
    }
}