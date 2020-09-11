using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Midi;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class MidiExporter {
        public static void SaveToFile(string fileName, SampleGeneratingArgs[] samples) {
            var validSamples = samples.Where(o => o.Key >= 0).ToArray();
            SaveToFile(fileName,
                validSamples.Select(s => s.Bank < 0 ? 0 : s.Bank).ToArray(),
                validSamples.Select(s => s.Patch < 0 ? 0 : s.Patch).ToArray(),
                validSamples.Select(s => s.Key).ToArray(),
                validSamples.Select(s => s.Length < 0 ? 0 : s.Length).ToArray(),
                validSamples.Select(s => s.Velocity < 0 ? 0 : s.Velocity).ToArray());
        }

		public static void SaveToFile(string fileName, int[] bankNumbers, int[] patchNumbers, int[] noteNumbers, double[] durations, int[] velocities) {
            const int MidiFileType = 0;
            const int MicrosecondsPerQuaterNote = 1000000;
            const int TicksPerQuarterNote = 120;

            const int TrackNumber = 0;
            const long Time = 0;

            var collection = new MidiEventCollection(MidiFileType, TicksPerQuarterNote);

            collection.AddEvent(new TextEvent("Note Stream", MetaEventType.TextEvent, Time), TrackNumber);
            collection.AddEvent(new TempoEvent(MicrosecondsPerQuaterNote, Time), TrackNumber);

            var channels = new List<Tuple<int, int>>();

            int notesAdded = 0;
            for (int i = 0; i < noteNumbers.Length; i++) {
                var channelIndex = FindChannel(channels, bankNumbers[i], patchNumbers[i]);

                if (channelIndex == -1) {
                    channels.Add(new Tuple<int, int>(bankNumbers[i], patchNumbers[i]));

                    channelIndex = channels.Count;
                    collection.AddEvent(new ControlChangeEvent(Time, channelIndex, MidiController.BankSelect, bankNumbers[i] >> 8 << 8), TrackNumber);
                    collection.AddEvent(new ControlChangeEvent(Time, channelIndex, MidiController.BankSelectLsb, (byte)bankNumbers[i]), TrackNumber);
                    collection.AddEvent(new PatchChangeEvent(Time, channelIndex, patchNumbers[i]), TrackNumber);
                }

                var tickDuration = (int) (durations[i] * 1000 / MicrosecondsPerQuaterNote * TicksPerQuarterNote);
                collection.AddEvent(new NoteOnEvent(Time, channelIndex, noteNumbers[i], velocities[i], tickDuration), TrackNumber);
                collection.AddEvent(new NoteEvent(Time + tickDuration, channelIndex, MidiCommandCode.NoteOff, noteNumbers[i], 0), TrackNumber);

                notesAdded++;
            }

            if (notesAdded == 0)
                return;

            collection.PrepareForExport();
            MidiFile.Export(fileName, collection);
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
	}
}