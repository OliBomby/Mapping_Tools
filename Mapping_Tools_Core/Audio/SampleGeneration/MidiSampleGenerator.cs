using Mapping_Tools_Core.Audio.Exporting;
using Mapping_Tools_Core.Audio.Midi;

namespace Mapping_Tools_Core.Audio.SampleGeneration {
    public class MidiSampleGenerator : IMidiSampleGenerator {
        public IMidiNote Note { get; }

        /// <summary>
        /// Initializes a new <see cref="MidiSampleGenerator"/>.
        /// </summary>
        /// <param name="note">The MIDI note to represent.</param>
        public MidiSampleGenerator(IMidiNote note) {
            Note = note;
        }

        public bool Equals(ISampleGenerator other) {
            return other is IMidiSampleGenerator o &&
                   Note.Equals(o.Note);
        }

        public object Clone() {
            return new MidiSampleGenerator(Note);
        }

        public bool IsValid() {
            return true;
        }

        public string GetName() {
            return "MidiNote-" + Note;
        }

        public void ToExporter(ISampleExporter exporter) {
            if (exporter is IMidiSampleExporter midiSampleExporter) {
                midiSampleExporter.AddMidiNote(Note);
            }
        }
    }
}