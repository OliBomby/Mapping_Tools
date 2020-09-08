using System.Collections.Generic;
using NAudio.Midi;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class MidiExporter {
		public void SaveToFile(string fileName) {
            const int MidiFileType = 0;
            const int BeatsPerMinute = 60;
            const int TicksPerQuarterNote = 120;

            const int TrackNumber = 0;
            const int ChannelNumber = 1;

            long absoluteTime = 0;

            var collection = new MidiEventCollection(MidiFileType, TicksPerQuarterNote);

            collection.AddEvent(new TextEvent("Note Stream", MetaEventType.TextEvent, absoluteTime), TrackNumber);
            ++absoluteTime;
            collection.AddEvent(new TempoEvent(CalculateMicrosecondsPerQuaterNote(BeatsPerMinute), absoluteTime), TrackNumber);

            collection.AddEvent(new PatchChangeEvent(0, ChannelNumber, 0), TrackNumber);

            const int NoteVelocity = 100;
            const int NoteDuration = 3 * TicksPerQuarterNote / 4;
            const long SpaceBetweenNotes = TicksPerQuarterNote;

            collection.AddEvent(new NoteOnEvent(absoluteTime, ChannelNumber, 70, NoteVelocity, NoteDuration), TrackNumber);
            collection.AddEvent(new NoteEvent(absoluteTime + NoteDuration, ChannelNumber, MidiCommandCode.NoteOff, 70, 0), TrackNumber);

            absoluteTime += SpaceBetweenNotes;

            collection.PrepareForExport();
            MidiFile.Export(fileName, collection);
        }

        private static int CalculateMicrosecondsPerQuaterNote(int bpm) {
            return 60 * 1000 * 1000 / bpm;
        }
	}
}