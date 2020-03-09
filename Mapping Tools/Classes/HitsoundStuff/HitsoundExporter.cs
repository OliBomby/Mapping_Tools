using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.Tools;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.HitsoundStuff
{
    class HitsoundExporter {
        public static void ExportCompleteHitsounds(string exportFolder, string baseBeatmap, CompleteHitsounds ch, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null) {
            // Export the beatmap with all hitsounds
            ExportHitsounds(ch.Hitsounds, baseBeatmap, exportFolder);

            // Export the sample files
            ExportCustomIndices(ch.CustomIndices, exportFolder, loadedSamples);
        }

        public static void ExportHitsounds(IEnumerable<HitsoundEvent> hitsounds, string baseBeatmap, string exportFolder, bool useGreenlines=true, bool useStoryboard=false) {
            EditorReaderStuff.TryGetNewestVersion(baseBeatmap, out var editor);
            Beatmap beatmap = editor.Beatmap;

            if (useStoryboard) {
                beatmap.StoryboardSoundSamples = hitsounds.Select(h =>
                        new StoryboardSoundSample(h.Time, 0, h.Filename, h.Volume))
                    .ToList();
            } else {
                // Make new timing points
                // Add red lines
                List<TimingPoint> timingPoints = beatmap.BeatmapTiming.GetAllRedlines();
                List<TimingPointsChange> timingPointsChanges = timingPoints.Select(tp =>
                        new TimingPointsChange(tp, mpb: true, meter: true, inherited: true, omitFirstBarLine: true))
                    .ToList();

                // Add hitsound stuff
                // Replace all hitobjects with the hitsounds
                beatmap.HitObjects.Clear();
                foreach (HitsoundEvent h in hitsounds) {
                    if (useGreenlines) {
                        TimingPoint tp = beatmap.BeatmapTiming.GetTimingPointAtTime(h.Time + 5).Copy();
                        tp.Offset = h.Time;
                        tp.SampleIndex = h.CustomIndex;
                        h.CustomIndex = 0; // Set it to default value because it gets handled by greenlines now
                        tp.Volume = Math.Round(tp.Volume * h.Volume);
                        h.Volume = 0; // Set it to default value because it gets handled by greenlines now
                        timingPointsChanges.Add(new TimingPointsChange(tp, index: true, volume: true));
                    }

                    beatmap.HitObjects.Add(new HitObject(h.Pos, h.Time, 5, h.GetHitsounds(), h.SampleSet, h.Additions,
                        h.CustomIndex, h.Volume * 100, h.Filename));
                }

                // Replace the old timingpoints
                beatmap.BeatmapTiming.TimingPoints.Clear();
                TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);
            }

            // Change version to hitsounds
            beatmap.General["StackLeniency"] = new TValue("0.0");
            beatmap.General["Mode"] = new TValue("0");
            beatmap.Metadata["Version"] = new TValue("Hitsounds");
            beatmap.Difficulty["CircleSize"] = new TValue("4");

            // Save the file to the export folder
            editor.SaveFile(Path.Combine(exportFolder, beatmap.GetFileName()));
        }

        public static void ExportLoadedSamples(Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples,
            string exportFolder, Dictionary<SampleGeneratingArgs, string> names = null) {
            if (names == null) {
                names = GenerateSampleNames(loadedSamples.Keys);
            }

            foreach (var sample in loadedSamples.Keys) {
                ExportSample(sample, names[sample], exportFolder, loadedSamples);
            }
        }

        public static bool ExportSample(SampleGeneratingArgs sampleGeneratingArgs, string name,
            string exportFolder, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples=null) {
            SampleSoundGenerator sampleSoundGenerator;
            if (loadedSamples != null) {
                if (SampleImporter.ValidateSampleArgs(sampleGeneratingArgs, loadedSamples)) {
                    sampleSoundGenerator = loadedSamples[sampleGeneratingArgs];
                } else {
                    return false;
                }
            } else {
                try {
                    sampleSoundGenerator = SampleImporter.ImportSample(sampleGeneratingArgs);
                } catch (Exception ex) {
                    Console.WriteLine($@"{ex.Message} while importing sample {sampleGeneratingArgs}.");
                    return false;
                }
            }

            // TODO: Allow mp3, ogg and aif export.
            string filename = name + ".wav";
            CreateWaveFile(Path.Combine(exportFolder, filename), sampleSoundGenerator.GetSampleProvider().ToWaveProvider16());

            return true;
        }

        public static void ExportMixedSample(IEnumerable<SampleGeneratingArgs> sampleGeneratingArgses, string name,
            string exportFolder, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples=null) {
            var samples = new List<ISampleProvider>();
            var volumes = new List<double>();
            int soundsAdded = 0;
            
            if (loadedSamples != null) {
                foreach (var sample in from args in sampleGeneratingArgses where SampleImporter.ValidateSampleArgs(args, loadedSamples) select loadedSamples[args]) {
                    samples.Add(sample.GetSampleProvider());
                    volumes.Add(Math.Abs(sample.VolumeCorrection - -1) > Precision.DOUBLE_EPSILON ? sample.VolumeCorrection : 1f);
                    soundsAdded++;
                }
            } else {
                foreach (SampleGeneratingArgs args in sampleGeneratingArgses) {
                    try {
                        var sample = SampleImporter.ImportSample(args);
                        samples.Add(sample.GetSampleProvider());
                        volumes.Add(Math.Abs(sample.VolumeCorrection - -1) > Precision.DOUBLE_EPSILON
                            ? sample.VolumeCorrection
                            : 1f);
                        soundsAdded++;
                    } catch (Exception ex) {
                        Console.WriteLine($@"{ex.Message} while importing sample {args}.");
                    }
                }
            }

            if (soundsAdded == 0) {
                return;
            }

            int maxSampleRate = samples.Max(o => o.WaveFormat.SampleRate);
            int maxChannels = samples.Max(o => o.WaveFormat.Channels);
            IEnumerable<ISampleProvider> sameFormatSamples = samples.Select(o => (ISampleProvider)new WdlResamplingSampleProvider(SampleImporter.SetChannels(o, maxChannels), maxSampleRate));

            ISampleProvider result = new MixingSampleProvider(sameFormatSamples);

            if (soundsAdded > 1) {
                result = new VolumeSampleProvider(result) {
                    Volume = (float)(1 / Math.Sqrt(soundsAdded * volumes.Average()))
                };
                result = new SimpleCompressorEffect(result) {
                    Threshold = 16,
                    Ratio = 6,
                    Attack = 0.1,
                    Release = 0.1,
                    Enabled = true,
                    MakeUpGain = 15 * Math.Log10(Math.Sqrt(soundsAdded * volumes.Average()))
                };
            }

            // TODO: Allow mp3, ogg and aif export.
            string filename = name + ".wav";
            CreateWaveFile(Path.Combine(exportFolder, filename), result.ToWaveProvider16());
        }

        public static void ExportCustomIndices(List<CustomIndex> customIndices, string exportFolder, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples=null) {
            foreach (CustomIndex ci in customIndices) {
                foreach (KeyValuePair<string, HashSet<SampleGeneratingArgs>> kvp in ci.Samples) {
                    if (kvp.Value.Count == 0) {
                        continue;
                    }
                    
                    string filename = ci.Index == 1 ? kvp.Key : kvp.Key + ci.Index;
                    ExportMixedSample(kvp.Value, filename, exportFolder, loadedSamples);
                }
            }
        }

        private static bool CreateWaveFile(string filename, IWaveProvider sourceProvider) {
            try {
                using (var writer = new WaveFileWriter(filename, sourceProvider.WaveFormat)) {
                    var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
                    while (true) {
                        int bytesRead = sourceProvider.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) {
                            // end of source provider
                            break;
                        }

                        // Write will throw exception if WAV file becomes too large
                        writer.Write(buffer, 0, bytesRead);
                    }
                }

                return true;
            } catch (IndexOutOfRangeException) {
                return false;
            }
        }

        public static Dictionary<SampleGeneratingArgs, string> GenerateSampleNames(IEnumerable<SampleGeneratingArgs> samples) {
            var usedNames = new HashSet<string>();
            var sampleNames = new Dictionary<SampleGeneratingArgs, string>();
            foreach (var sample in samples) {
                var baseName = sample.GetFilename();
                var ext = sample.GetExtension();
                var name = baseName + ext;
                int i = 1;

                while (usedNames.Contains(name)) {
                    name = baseName + "-" + ++i + ext; 
                }

                usedNames.Add(name);
                sampleNames.Add(sample, name);
            }

            return sampleNames;
        }

        public static Dictionary<SampleGeneratingArgs, Vector2> GenerateHitsoundPositions(IEnumerable<SampleGeneratingArgs> samples) {
            var sampleArray = samples.ToArray();
            var sampleCount = sampleArray.Length;

            // Find the biggest spacing that will still fit all the samples
            int spacing = 64;
            while ((int) (512d / spacing + 1) * (int) (384d / spacing + 1) < sampleCount && spacing > 1) {
                spacing /= 2;
            }

            var positions = new Dictionary<SampleGeneratingArgs, Vector2>();
            int x = 0;
            int y = 0;
            foreach (var sample in sampleArray) {
                positions.Add(sample, new Vector2(x, y));

                x += spacing;
                if (x > 512) {
                    x = 0;
                    y += spacing;

                    if (y > 384) {
                        y = 0;
                    }
                }
            }

            return positions;
        }
    }
}
