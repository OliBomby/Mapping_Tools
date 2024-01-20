using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
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
            const int midiFileType = 0;
            const int microsecondsPerQuaterNote = 1000000;
            const int ticksPerQuarterNote = 120;

            const int trackNumber = 0;
            const long time = 0;

            var collection = new MidiEventCollection(midiFileType, ticksPerQuarterNote);

            collection.AddEvent(new TextEvent("Note Stream", MetaEventType.TextEvent, time), trackNumber);
            collection.AddEvent(new TempoEvent(microsecondsPerQuaterNote, time), trackNumber);

            var channels = new List<Tuple<int, int>>();

            int notesAdded = 0;
            for (int i = 0; i < noteNumbers.Length; i++) {
                var channelIndex = FindChannel(channels, bankNumbers[i], patchNumbers[i]);

                if (channelIndex == -1) {
                    channels.Add(new Tuple<int, int>(bankNumbers[i], patchNumbers[i]));

                    channelIndex = channels.Count >= 10 ? channels.Count + 1 : channels.Count;  // Dont use the percussion channel
                    collection.AddEvent(new ControlChangeEvent(time, channelIndex, MidiController.BankSelect, bankNumbers[i] >> 7), trackNumber);
                    collection.AddEvent(new ControlChangeEvent(time, channelIndex, MidiController.BankSelectLsb, (byte)bankNumbers[i] & 0x01111111), trackNumber);
                    collection.AddEvent(new PatchChangeEvent(time, channelIndex, patchNumbers[i]), trackNumber);
                }

                var tickDuration = (int) (durations[i] * 1000 / microsecondsPerQuaterNote * ticksPerQuarterNote);
                collection.AddEvent(new NoteOnEvent(time, channelIndex, noteNumbers[i], velocities[i], tickDuration), trackNumber);
                collection.AddEvent(new NoteEvent(time + tickDuration, channelIndex, MidiCommandCode.NoteOff, noteNumbers[i], 0), trackNumber);

                notesAdded++;
            }

            if (notesAdded == 0)
                return;

            collection.PrepareForExport();
            MidiFile.Export(fileName, collection);
        }

        private static int FindChannel(List<Tuple<int, int>> channels, int bank, int patch) {
            if (bank == 128)
                return 10;  // Standard MIDI percussion channel

            for (int i = 0; i < channels.Count; i++) {
                var item = channels[i];
                if (item.Item1 == bank && item.Item2 == patch) {
                    return i >= 9 ? i + 2 : i + 1;  // We dont want to output to the percussion channel
                }
            }

            return -1;
        }

        private static int CalculateMicrosecondsPerQuaterNote(double bpm) {
            return (int) (60 * 1000 * 1000 / bpm);
        }

        public static void ExportAsMidi(List<SamplePackage> samplePackages, Beatmap baseBeatmap, string fileName, bool addGreenLineVolume) {
            const int midiFileType = 0;
            const int ticksPerQuarterNote = 120;

            const int trackNumber = 0;

            int microsecondsPerQuaterNote = baseBeatmap.BeatmapTiming.Redlines.Count > 0 ?
                CalculateMicrosecondsPerQuaterNote(baseBeatmap.BeatmapTiming.Redlines[0].GetBpm()) : 1000000;
            long tick = baseBeatmap.BeatmapTiming.Redlines.Count > 0 ?
                (long) (baseBeatmap.BeatmapTiming.Redlines[0].Offset * 1000 / microsecondsPerQuaterNote * ticksPerQuarterNote) : 0;

            var collection = new MidiEventCollection(midiFileType, ticksPerQuarterNote);

            collection.AddEvent(new TextEvent("Note stream", MetaEventType.TextEvent, tick), trackNumber);
            collection.AddEvent(new TempoEvent(microsecondsPerQuaterNote, tick), trackNumber);

            foreach (var samplePackage in samplePackages) {
                tick = (long) (samplePackage.Time * 1000 / microsecondsPerQuaterNote * ticksPerQuarterNote);

                var channels = new List<Tuple<int, int>>();

                foreach (var sample in samplePackage.Samples) {
                    var sampleArgs = sample.SampleArgs;

                    int tickDuration = (int) Math.Max(sampleArgs.Length * 1000 / microsecondsPerQuaterNote * ticksPerQuarterNote, 0);
                    int bank = Math.Max(sampleArgs.Bank, 0);
                    int patch = MathHelper.Clamp(sampleArgs.Patch, 0, 127);
                    int key = MathHelper.Clamp(sampleArgs.Key, 0, 127);
                    int velocity = MathHelper.Clamp(sampleArgs.Velocity, 0, 127);

                    var channelIndex = FindChannel(channels, bank, patch);

                    if (channelIndex == -1) {
                        channels.Add(new Tuple<int, int>(bank, patch));

                        channelIndex = channels.Count >= 10 ? channels.Count + 1 : channels.Count;  // Dont use the percussion channel
                        collection.AddEvent(new ControlChangeEvent(tick, channelIndex, MidiController.BankSelect, bank >> 7), trackNumber);
                        collection.AddEvent(new ControlChangeEvent(tick, channelIndex, MidiController.BankSelectLsb, (byte)bank & 0x01111111), trackNumber);
                        collection.AddEvent(new PatchChangeEvent(tick, channelIndex, patch), trackNumber);
                    }

                    collection.AddEvent(new NoteOnEvent(tick, channelIndex, key, velocity, tickDuration), trackNumber);
                    collection.AddEvent(new NoteEvent(tick + tickDuration, channelIndex, MidiCommandCode.NoteOff, key, 0), trackNumber);
                }
            }

            if (addGreenLineVolume) {
                // Add the greenline volume of the base beatmap as a track with volume change events
                const int volumeTrackNumber = 1;

                collection.AddEvent(new TextEvent("Green line volume", MetaEventType.TextEvent, 0), volumeTrackNumber);

                foreach (var tp in baseBeatmap.BeatmapTiming.TimingPoints) {
                    tick = (long) (tp.Offset * 1000 / microsecondsPerQuaterNote * ticksPerQuarterNote);

                    for (int i = 1; i <= 16; i++) {
                        collection.AddEvent(new ControlChangeEvent(tick, i, MidiController.MainVolume, (int) (tp.Volume * 127 / 100)), volumeTrackNumber);
                    }
                }
            }

            collection.PrepareForExport();
            MidiFile.Export(fileName, collection);
        }
    }
}