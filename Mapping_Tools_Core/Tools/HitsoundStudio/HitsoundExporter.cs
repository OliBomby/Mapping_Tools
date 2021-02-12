using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools_Core.Audio;
using Mapping_Tools_Core.Audio.Effects;
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using Mapping_Tools_Core.BeatmapHelper;
using Mapping_Tools_Core.BeatmapHelper.Editor;
using Mapping_Tools_Core.BeatmapHelper.Enums;
using Mapping_Tools_Core.BeatmapHelper.Events;
using Mapping_Tools_Core.MathUtil;
using Mapping_Tools_Core.ToolHelpers;
using Mapping_Tools_Core.Tools.HitsoundStudio.Model;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mapping_Tools_Core.Tools.HitsoundStudio
{
    public class HitsoundExporter {
        public enum SampleExportFormat {
            Default,
            WaveIeeeFloat,
            WavePcm,
            OggVorbis,
            MidiChords
        }

        public static void ExportHitsounds(ICollection<IHitsoundEvent> hitsounds, 
            string baseBeatmap, 
            string exportFolder, 
            string exportMapName, 
            GameMode exportGameMode, 
            bool useGreenlines, 
            bool useStoryboard,
            IReadWriteEditor<Beatmap> editor = null) {

            editor = editor ?? new BeatmapEditor();
            editor.Path = baseBeatmap;
            Beatmap beatmap = editor.ReadFile();

            if (useStoryboard) {
                beatmap.StoryboardSoundSamples.Clear();
                foreach (var h in hitsounds.Where(h => !String.IsNullOrEmpty(h.Filename))) {
                    beatmap.StoryboardSoundSamples.Add(new StoryboardSoundSample((int) Math.Round(h.Time), 0, h.Filename, h.Volume * 100));
                }
            } else {
                // Make new timing points
                // Add red lines
                var redlines = beatmap.BeatmapTiming.Redlines;
                List<TimingPointsChange> timingPointsChanges = redlines.Select(tp =>
                        new TimingPointsChange(tp, mpb: true, meter: true, unInherited: true, omitFirstBarLine: true))
                    .ToList();

                // Add hitsound stuff
                // Replace all hitobjects with the hitsounds
                beatmap.HitObjects.Clear();
                foreach (IHitsoundEvent h in hitsounds) {
                    var index = h.CustomIndex;
                    var volume = h.Volume;

                    if (useGreenlines) {
                        TimingPoint tp = beatmap.BeatmapTiming.GetTimingPointAtTime(h.Time + 5).Copy();
                        tp.Offset = h.Time;
                        tp.SampleIndex = h.CustomIndex;
                        index = 0; // Set it to default value because it gets handled by greenlines now
                        tp.Volume = Math.Round(tp.Volume * h.Volume);
                        volume = 0; // Set it to default value because it gets handled by greenlines now
                        timingPointsChanges.Add(new TimingPointsChange(tp, index: true, volume: true));
                    }

                    beatmap.HitObjects.Add(new HitObject(h.Pos, h.Time, 5, h.GetHitsounds(), h.SampleSet, h.Additions,
                        index, volume * 100, h.Filename));
                }

                // Replace the old timingpoints
                beatmap.BeatmapTiming.Clear();
                TimingPointsChange.ApplyChanges(beatmap.BeatmapTiming, timingPointsChanges);
            }

            // Change version to hitsounds
            beatmap.General["StackLeniency"] = new TValue("0.0");
            beatmap.General["Mode"] = new TValue(((int) exportGameMode).ToInvariant());
            beatmap.Metadata["Version"] = new TValue(exportMapName);

            if (exportGameMode == GameMode.Mania) {
                // Count the number of distinct X positions
                int numXPositions =hitsounds.Select(h => h.Pos.X).Distinct().Count();
                int numKeys = MathHelper.Clamp(numXPositions, 1, 18);

                beatmap.Difficulty["CircleSize"] = new TValue(numKeys.ToInvariant());
            } else {
                beatmap.Difficulty["CircleSize"] = new TValue("4");
            }

            // Save the file to the export folder
            editor.Path = Path.Combine(exportFolder, beatmap.GetFileName());
            editor.WriteFile(beatmap);
        }

        public static void ExportLoadedSamples(Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples,
            string exportFolder, Dictionary<ISampleGeneratingArgs, string> names = null, 
            SampleExportFormat format=SampleExportFormat.Default) {
            if (names == null) {
                names = GenerateSampleNames(loadedSamples.Keys, loadedSamples);
            }

            foreach (var sample in loadedSamples.Keys.Where(sample => sample.IsValid(loadedSamples))) {
                ExportSample(sample, names[sample], exportFolder, loadedSamples, format);
            }
        }

        private static bool CopySample(string path, string dest) {
            try {
                File.Copy(path, dest, true);
                return true;
            }
            catch( Exception ex ) {
                Console.WriteLine($@"{ex.Message} while copying sample {path} to {dest}.");
                return false;
            }
        }

        private static bool IsCopyCompatible(IPathSampleGenerator pathSample, WaveFormatEncoding waveEncoding, SampleExportFormat exportFormat) {
            switch (exportFormat) {
                case SampleExportFormat.WaveIeeeFloat:
                    return waveEncoding == WaveFormatEncoding.IeeeFloat && Path.GetExtension(pathSample.Path) == ".wav";
                case SampleExportFormat.WavePcm:
                    return waveEncoding == WaveFormatEncoding.Pcm && Path.GetExtension(pathSample.Path) == ".wav";
                case SampleExportFormat.OggVorbis:
                    return Path.GetExtension(pathSample.Path) == ".ogg";
                default:
                    return true;
            }
        }

        public static bool ExportSample(ISampleGeneratingArgs sampleGeneratingArgs, string name,
            string exportFolder, Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples=null, 
            SampleExportFormat format=SampleExportFormat.Default) {

            // Export as midi file with single note
            if (format == SampleExportFormat.MidiChords) {
                MidiExporter.SaveToFile(Path.Combine(exportFolder, name + ".mid"), new[] {sampleGeneratingArgs.ImportArgs as IMidiSampleGenerator});
                return true;
            }

            if (sampleGeneratingArgs.ImportArgs is IPathSampleGenerator p && p.IsDirectSource() &&
                !sampleGeneratingArgs.HasEffects() && format == SampleExportFormat.Default) {

                var dest = Path.Combine(exportFolder, name + Path.GetExtension(p.Path));
                return CopySample(p.Path, dest);
            }

            ISampleSoundGenerator sampleSoundGenerator;
            if (loadedSamples != null) {
                if (sampleGeneratingArgs.IsValid(loadedSamples)) {
                    sampleSoundGenerator = loadedSamples[sampleGeneratingArgs];
                } else {
                    return false;
                }
            } else {
                try {
                    sampleSoundGenerator = sampleGeneratingArgs.Import();
                } catch (Exception ex) {
                    Console.WriteLine($@"{ex.Message} while importing sample {sampleGeneratingArgs}.");
                    return false;
                }
            }

            if (sampleSoundGenerator == null) {
                return false;
            }

            var sourceWaveEncoding = sampleSoundGenerator.GetSourceWaveFormat().Encoding;

            // Either if it is the blank sample or the source file is literally what the user wants to be exported
            if (sampleGeneratingArgs.ImportArgs is IPathSampleGenerator p2 && p2.IsDirectSource() && 
                (sampleSoundGenerator.IsBlank() ||
                 !sampleGeneratingArgs.HasEffects() && IsCopyCompatible(p2, sourceWaveEncoding, format))) {

                var dest = Path.Combine(exportFolder, name + Path.GetExtension(p2.Path));
                return CopySample(p2.Path, dest);
            }

            var sampleProvider = sampleSoundGenerator.GetSampleProvider();

            if ((format == SampleExportFormat.WavePcm || format == SampleExportFormat.OggVorbis) && sourceWaveEncoding == WaveFormatEncoding.IeeeFloat) {
                // When the source is IEEE float and the export format is PCM or Vorbis, then clipping is possible, so we add a limiter
                sampleProvider = new SoftLimiter(sampleProvider);
            }

            switch (format) {
                case SampleExportFormat.WaveIeeeFloat:
                    CreateWaveFile(Path.Combine(exportFolder, name + ".wav"), sampleProvider.ToWaveProvider());
                    break;
                case SampleExportFormat.WavePcm:
                    CreateWaveFile(Path.Combine(exportFolder, name + ".wav"), sampleProvider.ToWaveProvider16());
                    break;
                case SampleExportFormat.OggVorbis:
                    var resampled = new WdlResamplingSampleProvider(sampleProvider,
                        VorbisFileWriter.GetSupportedSampleRate(sampleProvider.WaveFormat.SampleRate));
                    VorbisFileWriter.CreateVorbisFile(Path.Combine(exportFolder, name + ".ogg"), resampled.ToWaveProvider());
                    break;
                default:
                    switch (sourceWaveEncoding) {
                        case WaveFormatEncoding.IeeeFloat:
                            CreateWaveFile(Path.Combine(exportFolder, name + ".wav"), sampleProvider.ToWaveProvider());
                            break;
                        case WaveFormatEncoding.Pcm:
                            CreateWaveFile(Path.Combine(exportFolder, name + ".wav"), sampleProvider.ToWaveProvider16());
                            break;
                        default:
                            CreateWaveFile(Path.Combine(exportFolder, name + ".wav"), sampleProvider.ToWaveProvider());
                            break;
                    }

                    break;
            }

            return true;
        }

        public static void ExportMixedSample(IEnumerable<ISampleGeneratingArgs> sampleGeneratingArgses, string name,
            string exportFolder, Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples=null, 
            SampleExportFormat format=SampleExportFormat.Default, SampleExportFormat mixedFormat=SampleExportFormat.Default) {

            // Export as midi file with single chord
            if (format == SampleExportFormat.MidiChords) {
                var notes = sampleGeneratingArgses.Where(o => o.ImportArgs is IMidiSampleGenerator)
                    .Select(o => (IMidiSampleGenerator) o.ImportArgs);
                MidiExporter.SaveToFile(Path.Combine(exportFolder, name + ".mid"), notes);
                return;
            }

            // Try loading all the valid samples
            // All Values are not null
            var validLoadedSamples = new Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator>();
            
            if (loadedSamples != null) {
                foreach (var args in sampleGeneratingArgses) {
                    if (!args.IsValid(loadedSamples)) continue;

                    var sample = loadedSamples[args];

                    // Make sure the sample generator is not null
                    if (sample == null) continue;

                    validLoadedSamples.Add(args, sample);
                }
            } else {
                // Import each sample individually
                foreach (ISampleGeneratingArgs args in sampleGeneratingArgses) {
                    try {
                        var sample = args.Import();

                        // Make sure the sample generator is not null
                        if (sample == null) continue;

                        validLoadedSamples.Add(args, sample);
                    } catch (Exception ex) {
                        Console.WriteLine($@"{ex.Message} while importing sample {args}.");
                    }
                }
            }

            if (validLoadedSamples.Count == 0) return;

            // If it has only one valid sample, we can just export it with the single sample export
            // If all the valid samples are blank samples, then also export only a single blank sample
            if (validLoadedSamples.Count == 1 || validLoadedSamples.All(o => o.Value.IsBlank())) {
                ExportSample(validLoadedSamples.Keys.First(), name, exportFolder, loadedSamples, format);
            } else if (validLoadedSamples.Count > 1) {
                var sampleProviders = validLoadedSamples.Values.Select(o => o.GetSampleProvider()).ToList();

                // Synchronize the sample rate and channels for all samples and get the sample providers
                int maxSampleRate = sampleProviders.Max(o => o.WaveFormat.SampleRate);
                int maxChannels = sampleProviders.Max(o => o.WaveFormat.Channels);

                // Resample to a supported sample rate when exporting in vorbis format
                if (mixedFormat == SampleExportFormat.OggVorbis) {
                    maxSampleRate = VorbisFileWriter.GetSupportedSampleRate(maxSampleRate);
                }

                IEnumerable<ISampleProvider> sameFormatSamples = sampleProviders.Select(o =>
                    (ISampleProvider) new WdlResamplingSampleProvider(SetChannels(o, maxChannels), maxSampleRate));

                ISampleProvider sampleProvider = new MixingSampleProvider(sameFormatSamples);

                // If the input is Ieee float or you are mixing multiple samples, then clipping is possible,
                // so you can either export as IEEE float or use a compressor and export as 16-bit PCM (half filesize) or Vorbis (even smaller filesize)
                // If the input is only The Blank Sample then it should export The Blank Sample

                if (mixedFormat == SampleExportFormat.WavePcm || mixedFormat == SampleExportFormat.OggVorbis) {
                    // When the sample is mixed and the export format is PCM or Vorbis, then clipping is possible, so we add a limiter
                    sampleProvider = new SoftLimiter(sampleProvider);
                }

                switch (mixedFormat) {
                    case SampleExportFormat.WaveIeeeFloat:
                        CreateWaveFile(Path.Combine(exportFolder, name + ".wav"), sampleProvider.ToWaveProvider());
                        break;
                    case SampleExportFormat.WavePcm:
                        CreateWaveFile(Path.Combine(exportFolder, name + ".wav"), sampleProvider.ToWaveProvider16());
                        break;
                    case SampleExportFormat.OggVorbis:
                        VorbisFileWriter.CreateVorbisFile(Path.Combine(exportFolder, name + ".ogg"), sampleProvider.ToWaveProvider());
                        break;
                    default:
                        CreateWaveFile(Path.Combine(exportFolder, name + ".wav"), sampleProvider.ToWaveProvider());
                        break;
                }
            }
        }

        public static ISampleProvider SetChannels(ISampleProvider sampleProvider, int channels) {
            return channels == 1 ? ToMono(sampleProvider) : ToStereo(sampleProvider);
        }

        private static ISampleProvider ToStereo(ISampleProvider sampleProvider) {
            return sampleProvider.WaveFormat.Channels == 1 ? new MonoToStereoSampleProvider(sampleProvider) : sampleProvider;
        }

        private static ISampleProvider ToMono(ISampleProvider sampleProvider) {
            return sampleProvider.WaveFormat.Channels == 2 ? new StereoToMonoSampleProvider(sampleProvider) : sampleProvider;
        }

        /// <summary>
        /// Exports all samples for a collection of custom indices.
        /// </summary>
        /// <param name="customIndices"></param>
        /// <param name="exportFolder"></param>
        /// <param name="loadedSamples"></param>
        /// <param name="format"></param>
        /// <param name="mixedFormat"></param>
        public static void ExportCustomIndices(List<ICustomIndex> customIndices, string exportFolder, 
            Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples=null, 
            SampleExportFormat format=SampleExportFormat.Default, SampleExportFormat mixedFormat=SampleExportFormat.Default) {
            foreach (ICustomIndex ci in customIndices) {
                foreach (KeyValuePair<string, HashSet<ISampleGeneratingArgs>> kvp in ci.Samples) {
                    if (kvp.Value.Count == 0) {
                        continue;
                    }
                    
                    string filename = ci.Index == 1 ? kvp.Key : kvp.Key + ci.Index;
                    ExportMixedSample(kvp.Value, filename, exportFolder, loadedSamples, format, mixedFormat);
                }
            }
        }

        public static void ExportSampleSchema(ISampleSchema sampleSchema, string exportFolder,
            Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples = null,
            SampleExportFormat format = SampleExportFormat.Default, SampleExportFormat mixedFormat = SampleExportFormat.Default) {
            foreach (var kvp in sampleSchema) {
                ExportMixedSample(kvp.Value, kvp.Key, exportFolder, loadedSamples, format, mixedFormat);
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

        public static Dictionary<ISampleGeneratingArgs, string> GenerateSampleNames(IEnumerable<ISampleGeneratingArgs> samples, 
            Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples) {
            var usedNames = new HashSet<string>();
            var sampleNames = new Dictionary<ISampleGeneratingArgs, string>();
            foreach (var sample in samples) {
                if (!sample.IsValid(loadedSamples)) {
                    sampleNames[sample] = string.Empty;
                    continue;
                }
                
                var baseName = sample.GetName();
                var name = baseName;
                int i = 1;

                while (usedNames.Contains(name)) {
                    name = baseName + "-" + ++i; 
                }

                usedNames.Add(name);
                sampleNames[sample] = name;
            }

            return sampleNames;
        }

        public static void AddNewSampleName(Dictionary<ISampleGeneratingArgs, string> sampleNames, ISampleGeneratingArgs sample,
            Dictionary<ISampleGeneratingArgs, ISampleSoundGenerator> loadedSamples) {
            if (!sample.IsValid(loadedSamples)) {
                sampleNames[sample] = string.Empty;
                return;
            }
            
            var baseName = sample.GetName();
            var name = baseName;
            int i = 1;

            while (sampleNames.ContainsValue(name)) {
                name = baseName + "-" + ++i; 
            }

            sampleNames[sample] = name;
        }

        public static Dictionary<ISampleGeneratingArgs, Vector2> GenerateHitsoundPositions(IEnumerable<ISampleGeneratingArgs> samples) {
            var sampleArray = samples.ToArray();
            var sampleCount = sampleArray.Length;

            // Find the biggest spacing that will still fit all the samples
            int spacingX = 128;
            int spacingY = 128;
            bool reduceX = false;
            while ((int) (512d / spacingX + 1) * (int) (384d / spacingY + 1) < sampleCount && spacingX > 1) {
                reduceX = !reduceX;
                if (reduceX)
                    spacingX /= 2;
                else
                    spacingY /= 2;
            }

            var positions = new Dictionary<ISampleGeneratingArgs, Vector2>();
            int x = 0;
            int y = 0;
            foreach (var sample in sampleArray) {
                positions.Add(sample, new Vector2(x, y));

                x += spacingX;
                if (x > 512) {
                    x = 0;
                    y += spacingY;

                    if (y > 384) {
                        y = 0;
                    }
                }
            }

            return positions;
        }

        public static Dictionary<ISampleGeneratingArgs, Vector2> GenerateManiaHitsoundPositions(IEnumerable<ISampleGeneratingArgs> samples) {
            var sampleArray = samples.ToArray();
            var sampleCount = sampleArray.Length;

            // One key per unique sample but clamped between 1 and 18
            int numKeys = MathHelper.Clamp(sampleCount, 1, 18);

            var positions = new Dictionary<ISampleGeneratingArgs, Vector2>();
            double x = 256d / numKeys;
            foreach (var sample in sampleArray) {
                positions.Add(sample, new Vector2(Math.Round(x), 192));

                x += 512d / numKeys;
                if (x > 512) {
                    x = 256d / numKeys;
                }
            }

            return positions;
        }
    }
}
