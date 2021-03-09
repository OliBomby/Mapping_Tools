using Mapping_Tools_Core.Audio.Midi;
using NAudio.Midi;
using System;
using System.Collections.Generic;
using System.IO;

namespace Mapping_Tools_Core.Audio.Exporting {
    public class MidiSampleExporter : IMidiSampleExporter {
        public string ExportFolder { get; set; }
        public string ExportName { get; set; }

        private readonly Stack<IMidiNote> midiNotes;

        public MidiSampleExporter() : this(null, null) {}

        public MidiSampleExporter(string exportFolder, string exportName) {
            ExportFolder = exportFolder;
            ExportName = exportName;
            midiNotes = new Stack<IMidiNote>();
        }

        public bool Flush() {
            if (midiNotes.Count == 0) {
                Reset();
                return false;
            }

            const int MidiFileType = 0;
            const int MicrosecondsPerQuaterNote = 1000000;
            const int TicksPerQuarterNote = 120;

            const int TrackNumber = 0;
            const long Time = 0;

            var collection = new MidiEventCollection(MidiFileType, TicksPerQuarterNote);

            collection.AddEvent(new TextEvent("Note Stream", MetaEventType.TextEvent, Time), TrackNumber);
            collection.AddEvent(new TempoEvent(MicrosecondsPerQuaterNote, Time), TrackNumber);

            var channels = new List<Tuple<int, int>>();

            foreach (IMidiNote midiNote in midiNotes) {
                var channelIndex = FindChannel(channels, midiNote.Bank, midiNote.Patch);

                if (channelIndex == -1) {
                    channels.Add(new Tuple<int, int>(midiNote.Bank, midiNote.Patch));

                    channelIndex = channels.Count;
                    collection.AddEvent(new ControlChangeEvent(Time, channelIndex, MidiController.BankSelect, midiNote.Bank >> 8 << 8), TrackNumber);
                    collection.AddEvent(new ControlChangeEvent(Time, channelIndex, MidiController.BankSelectLsb, (byte)midiNote.Bank), TrackNumber);
                    collection.AddEvent(new PatchChangeEvent(Time, channelIndex, midiNote.Patch), TrackNumber);
                }

                var tickDuration = (int)(midiNote.Length * 1000 / MicrosecondsPerQuaterNote * TicksPerQuarterNote);
                collection.AddEvent(new NoteOnEvent(Time, channelIndex, midiNote.Key, midiNote.Velocity, tickDuration), TrackNumber);
                collection.AddEvent(new NoteEvent(Time + tickDuration, channelIndex, MidiCommandCode.NoteOff, midiNote.Key, 0), TrackNumber);
            }

            collection.PrepareForExport();

            var fileName = Path.Combine(ExportFolder, ExportName + GetDesiredExtension());
            MidiFile.Export(fileName, collection);

            Reset();
            return true;
        }

        private static int FindChannel(List<Tuple<int, int>> channels, int bank, int patch) {
            for (int i = 0; i < channels.Count; i++) {
                var item = channels[i];
                if (item.Item1 == bank && item.Item2 == patch) {
                    return i + 1;
                }
            }

            return -1;
        }

        private static int CalculateMicrosecondsPerQuaterNote(int bpm) {
            return 60 * 1000 * 1000 / bpm;
        }

        public void Reset() {
            midiNotes.Clear();
        }

        public string GetDesiredExtension() {
            return ".mid";
        }

        public void AddMidiNote(IMidiNote note) {
            midiNotes.Push(note);
        }
    }
}