using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Mapping_Tools_Core.Audio.Midi;
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerImportArgs;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model.LayerSourceRef;
using NAudio.Midi;

namespace Mapping_Tools_Core.Tools.HitsoundStudio.LayerImporters {
    public class MidiLayerImporter : IMidiLayerImporter {
        public IEnumerable<IHitsoundLayer> Import(IMidiLayerImportArgs args) {
            List<HitsoundLayer> hitsoundLayers = new List<HitsoundLayer>();

            var mf = new MidiFile(args.Path, false);

            Console.WriteLine(
                $@"Format {mf.FileFormat}, " +
                $@"Tracks {mf.Tracks}, " +
                $@"Delta Ticks Per Quarter Note {mf.DeltaTicksPerQuarterNote}");

            List<TempoEvent> tempos = new List<TempoEvent>();
            foreach (var track in mf.Events) {
                tempos.AddRange(track.OfType<TempoEvent>());
            }
            tempos = tempos.OrderBy(o => o.AbsoluteTime).ToList();

            List<double> cumulativeTime = CalculateCumulativeTime(tempos, mf.DeltaTicksPerQuarterNote);

            Dictionary<int, int> channelBanks = new Dictionary<int, int>();
            Dictionary<int, int> channelPatches = new Dictionary<int, int>();

            // Loop through every event of every track
            for (int track = 0; track < mf.Tracks; track++) {
                foreach (var midiEvent in mf.Events[track]) {
                    // Find out which kind of even it is
                    if (midiEvent is PatchChangeEvent pc) {
                        channelPatches[pc.Channel] = pc.Patch;
                    } else if (midiEvent is ControlChangeEvent co) {
                        if (co.Controller == MidiController.BankSelect) {
                            channelBanks[co.Channel] = (co.ControllerValue * 128) + (channelBanks.ContainsKey(co.Channel) ? (byte)channelBanks[co.Channel] : 0);
                        } else if (co.Controller == MidiController.BankSelectLsb) {
                            channelBanks[co.Channel] = co.ControllerValue + (channelBanks.ContainsKey(co.Channel) ? (channelBanks[co.Channel] >> 8) * 128 : 0);
                        }
                    } else if (MidiEvent.IsNoteOn(midiEvent)) {
                        var on = (NoteOnEvent) midiEvent;

                        // Extract all the info out of the note event
                        // Calculate the actual time of the note using the tempo information
                        double time = CalculateTime(on.AbsoluteTime, tempos, cumulativeTime, mf.DeltaTicksPerQuarterNote);
                        double length = on.OffEvent != null
                            ? CalculateTime(on.OffEvent.AbsoluteTime,
                                  tempos,
                                  cumulativeTime,
                                  mf.DeltaTicksPerQuarterNote) -
                              time
                            : -1;
                        length = RoundLength(length, args.LengthRoughness);

                        bool keys = args.DiscriminateKeys || on.Channel == 10;

                        int bank = args.DiscriminateInstruments
                            ? on.Channel == 10 ? 128 :
                            channelBanks.ContainsKey(on.Channel) ? channelBanks[on.Channel] : 0
                            : -1;
                        int patch = args.DiscriminateInstruments && channelPatches.ContainsKey(on.Channel)
                            ? channelPatches[on.Channel]
                            : -1;
                        int key = keys ? on.NoteNumber : -1;
                        length = args.DiscriminateLength ? length : -1;
                        int velocity = args.DiscriminateVelocities ? on.Velocity : -1;
                        velocity = (int)RoundVelocity(velocity, args.VelocityRoughness);

                        // Generate a suitable name
                        string lengthString = Math.Round(length).ToString(CultureInfo.InvariantCulture);

                        string instrumentName = on.Channel == 10 ? "Percussion" :
                            patch >= 0 && patch <= 127 ? PatchChangeEvent.GetPatchName(patch) : "Undefined";
                        string keyName = on.NoteName;

                        string name = instrumentName;
                        if (args.DiscriminateKeys)
                            name += "," + keyName;
                        if (args.DiscriminateLength)
                            name += "," + lengthString;
                        if (args.DiscriminateVelocities)
                            name += "," + velocity;

                        // Make sample generating args
                        var note = new MidiNote(bank, patch, key, velocity, length);

                        var sampleArgs = new MidiSampleGenerator(note);

                        // Make source ref
                        var sourceRef = new MidiLayerSourceRef(
                            args.Path,
                            note,
                            args.Offset,
                            args.LengthRoughness,
                            args.VelocityRoughness
                        );

                        // Find the hitsoundlayer with this path
                        HitsoundLayer layer = hitsoundLayers.Find(o => sourceRef.Equals(o.LayerSourceRef));

                        if (layer != null) {
                            // Find hitsound layer with this path and add this time
                            layer.Times.Add(time + args.Offset);
                        } else {
                            // Add new hitsound layer with this path
                            HitsoundLayer newLayer = new HitsoundLayer(sampleArgs) {
                                Name = name,
                                SampleSet = SampleSet.Normal,
                                Hitsound = Hitsound.Normal,
                                LayerSourceRef = sourceRef
                            };

                            newLayer.Times.Add(time + args.Offset);

                            hitsoundLayers.Add(newLayer);
                        }
                    }
                }
            }

            // Sort layers by name
            return hitsoundLayers.OrderBy(o => o.Name);
        }


        private static double RoundVelocity(double length, double roughness) {
            if (length == -1) {
                return length;
            }

            var multi = length / roughness;
            var round = Math.Round(multi);
            return round * roughness;
        }

        private static double RoundLength(double length, double roughness) {
            if (length == -1) {
                return length;
            }

            var pow = Math.Pow(length, 1 / roughness);
            var round = Math.Ceiling(pow);
            return Math.Pow(round, roughness);
        }

        private static double CalculateTime(long absoluteTime, List<TempoEvent> tempos, List<double> cumulativeTime, int dtpq) {
            TempoEvent prev = null;
            double prevTime = 0;
            for (int i = 0; i < tempos.Count; i++) {
                if (tempos[i].AbsoluteTime <= absoluteTime) {
                    prev = tempos[i];
                    prevTime = cumulativeTime[i];
                } else {
                    break;
                }
            }
            if (prev == null) {
                return absoluteTime;
            }

            double deltaTime = prev.MicrosecondsPerQuarterNote / 1000d * (absoluteTime - prev.AbsoluteTime) / dtpq;
            return prevTime + deltaTime;
        }

        private static List<double> CalculateCumulativeTime(List<TempoEvent> tempos, int deltaTicksPerQuarter) {
            // StartTime is in miliseconds
            List<double> times = new List<double>(tempos.Count);

            TempoEvent last = null;
            foreach (TempoEvent te in tempos) {
                if (last == null) {
                    times.Add(te.AbsoluteTime);
                } else {
                    long deltaTicks = te.AbsoluteTime - last.AbsoluteTime;
                    double deltaTime = last.MicrosecondsPerQuarterNote / 1000d * deltaTicks / deltaTicksPerQuarter;

                    times.Add(times.Last() + deltaTime);
                }

                last = te;
            }
            return times;
        }
    }
}