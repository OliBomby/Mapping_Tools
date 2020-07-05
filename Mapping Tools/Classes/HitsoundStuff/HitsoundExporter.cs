using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.Tools;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mapping_Tools.Classes.BeatmapHelper.Events;
using Mapping_Tools.Classes.HitsoundStuff.Effects;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.HitsoundStuff
{
    public class HitsoundExporter {
        public enum SampleExportFormat {
            Default,
            WaveIeeeFloat,
            WavePcm,
            OggVorbis
        }

        public static void ExportCompleteHitsounds(string exportFolder, string exportMapName, string baseBeatmap, CompleteHitsounds ch, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null) {
            // Export the beatmap with all hitsounds
            ExportHitsounds(ch.Hitsounds, baseBeatmap, exportFolder, exportMapName, GameMode.Standard, true, false);

            // Export the sample files
            ExportCustomIndices(ch.CustomIndices, exportFolder, loadedSamples);
        }

        public static void ExportHitsounds(List<HitsoundEvent> hitsounds, string baseBeatmap, string exportFolder, string exportMapName, GameMode exportGameMode, bool useGreenlines, bool useStoryboard) {
            var editor = EditorReaderStuff.GetNewestVersionOrNot(baseBeatmap);
            Beatmap beatmap = editor.Beatmap;

            if (useStoryboard) {
                beatmap.StoryboardSoundSamples.Clear();
                foreach (var h in hitsounds.Where(h => !string.IsNullOrEmpty(h.Filename))) {
                    beatmap.StoryboardSoundSamples.Add(new StoryboardSoundSample((int) Math.Round(h.Time), 0, h.Filename, h.Volume * 100));
                }
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
            beatmap.General["Mode"] = new TValue(((int) exportGameMode).ToInvariant());
            beatmap.Metadata["Version"] = new TValue(exportMapName);

            if (exportGameMode == GameMode.Mania) {
                // Count the number of distinct X positions
                int numXPositions = new HashSet<double>(hitsounds.Select(h => h.Pos.X)).Count;
                int numKeys = MathHelper.Clamp(numXPositions, 1, 18);

                beatmap.Difficulty["CircleSize"] = new TValue(numKeys.ToInvariant());
            } else {
                beatmap.Difficulty["CircleSize"] = new TValue("4");
            }

            // Save the file to the export folder
            editor.SaveFile(Path.Combine(exportFolder, beatmap.GetFileName()));
        }

        public static void ExportLoadedSamples(Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples,
            string exportFolder, Dictionary<SampleGeneratingArgs, string> names = null, 
            SampleExportFormat format=SampleExportFormat.Default) {
            if (names == null) {
                names = GenerateSampleNames(loadedSamples.Keys, loadedSamples);
            }

            foreach (var sample in loadedSamples.Keys.Where(sample => SampleImporter.ValidateSampleArgs(sample, loadedSamples))) {
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

        private static bool IsCopyCompatible(SampleGeneratingArgs sampleGeneratingArgs, WaveFormatEncoding waveEncoding, SampleExportFormat exportFormat) {
            switch (exportFormat) {
                case SampleExportFormat.WaveIeeeFloat:
                    return waveEncoding == WaveFormatEncoding.IeeeFloat && sampleGeneratingArgs.GetExtension() == ".wav";
                case SampleExportFormat.WavePcm:
                    return waveEncoding == WaveFormatEncoding.Pcm && sampleGeneratingArgs.GetExtension() == ".wav";
                case SampleExportFormat.OggVorbis:
                    return sampleGeneratingArgs.GetExtension() == ".ogg";
                default:
                    return true;
            }
        }

        public static bool ExportSample(SampleGeneratingArgs sampleGeneratingArgs, string name,
            string exportFolder, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples=null, 
            SampleExportFormat format=SampleExportFormat.Default) {

            if (sampleGeneratingArgs.CanCopyPaste && format == SampleExportFormat.Default) {
                var dest = Path.Combine(exportFolder, name + sampleGeneratingArgs.GetExtension());
                return CopySample(sampleGeneratingArgs.Path, dest);
            }

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

            var sourceWaveEncoding = sampleSoundGenerator.Wave.WaveFormat.Encoding;

            // Either if it is the blank sample or the source file is literally what the user wants to be exported
            if (sampleSoundGenerator.BlankSample && sampleGeneratingArgs.GetExtension() == ".wav" || 
                sampleGeneratingArgs.CanCopyPaste && IsCopyCompatible(sampleGeneratingArgs, sourceWaveEncoding, format)) {

                var dest = Path.Combine(exportFolder, name + sampleGeneratingArgs.GetExtension());
                return CopySample(sampleGeneratingArgs.Path, dest);
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
                    VorbisFileWriter.CreateVorbisFile(Path.Combine(exportFolder, name + ".ogg"), sampleProvider.ToWaveProvider());
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

        public static void ExportMixedSample(IEnumerable<SampleGeneratingArgs> sampleGeneratingArgses, string name,
            string exportFolder, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples=null, 
            SampleExportFormat format=SampleExportFormat.Default, SampleExportFormat mixedFormat=SampleExportFormat.Default) {

            // Try loading all the valid samples
            var validLoadedSamples = new Dictionary<SampleGeneratingArgs, SampleSoundGenerator>();
            
            if (loadedSamples != null) {
                foreach (var args in sampleGeneratingArgses) {
                    if (!SampleImporter.ValidateSampleArgs(args, loadedSamples)) continue;

                    var sample = loadedSamples[args];
                    validLoadedSamples.Add(args, sample);
                }
            } else {
                // Import each sample individually
                foreach (SampleGeneratingArgs args in sampleGeneratingArgses) {
                    try {
                        var sample = SampleImporter.ImportSample(args);
                        validLoadedSamples.Add(args, sample);
                    } catch (Exception ex) {
                        Console.WriteLine($@"{ex.Message} while importing sample {args}.");
                    }
                }
            }

            if (validLoadedSamples.Count == 0) return;

            // If all the valid samples are blank samples, then also export only a single blank sample
            if (validLoadedSamples.Count == 1 || validLoadedSamples.All(o => o.Value.BlankSample)) {
                // It has only one valid sample, so we can just export it with the single sample export
                ExportSample(validLoadedSamples.Keys.First(), name, exportFolder, loadedSamples, format);
            } else if (validLoadedSamples.Count > 1) {
                // Synchronize the sample rate and channels for all samples and get the sample providers
                int maxSampleRate = validLoadedSamples.Values.Max(o => o.Wave.WaveFormat.SampleRate);
                int maxChannels = validLoadedSamples.Values.Max(o => o.Wave.WaveFormat.Channels);

                IEnumerable<ISampleProvider> sameFormatSamples = validLoadedSamples.Select(o =>
                    (ISampleProvider) new WdlResamplingSampleProvider(SampleImporter.SetChannels(o.Value.GetSampleProvider(), maxChannels),
                        maxSampleRate));

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

        public static void ExportCustomIndices(List<CustomIndex> customIndices, string exportFolder, 
            Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples=null, 
            SampleExportFormat format=SampleExportFormat.Default, SampleExportFormat mixedFormat=SampleExportFormat.Default) {
            foreach (CustomIndex ci in customIndices) {
                foreach (KeyValuePair<string, HashSet<SampleGeneratingArgs>> kvp in ci.Samples) {
                    if (kvp.Value.Count == 0) {
                        continue;
                    }
                    
                    string filename = ci.Index == 1 ? kvp.Key : kvp.Key + ci.Index;
                    ExportMixedSample(kvp.Value, filename, exportFolder, loadedSamples, format, mixedFormat);
                }
            }
        }

        public static void ExportSampleSchema(SampleSchema sampleSchema, string exportFolder,
            Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples = null,
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

        public static Dictionary<SampleGeneratingArgs, string> GenerateSampleNames(IEnumerable<SampleGeneratingArgs> samples, 
            Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples) {
            var usedNames = new HashSet<string>();
            var sampleNames = new Dictionary<SampleGeneratingArgs, string>();
            foreach (var sample in samples) {
                if (!SampleImporter.ValidateSampleArgs(sample, loadedSamples)) {
                    sampleNames[sample] = string.Empty;
                    continue;
                }
                
                var baseName = sample.GetFilename();
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

        public static void AddNewSampleName(Dictionary<SampleGeneratingArgs, string> sampleNames, SampleGeneratingArgs sample,
            Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples) {
            if (!SampleImporter.ValidateSampleArgs(sample, loadedSamples)) {
                sampleNames[sample] = string.Empty;
                return;
            }
            
            var baseName = sample.GetFilename();
            var name = baseName;
            int i = 1;

            while (sampleNames.ContainsValue(name)) {
                name = baseName + "-" + ++i; 
            }

            sampleNames[sample] = name;
        }

        public static Dictionary<SampleGeneratingArgs, Vector2> GenerateHitsoundPositions(IEnumerable<SampleGeneratingArgs> samples) {
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

            var positions = new Dictionary<SampleGeneratingArgs, Vector2>();
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

        public static Dictionary<SampleGeneratingArgs, Vector2> GenerateManiaHitsoundPositions(IEnumerable<SampleGeneratingArgs> samples) {
            var sampleArray = samples.ToArray();
            var sampleCount = sampleArray.Length;

            // One key per unique sample but clamped between 1 and 18
            int numKeys = MathHelper.Clamp(sampleCount, 1, 18);

            var positions = new Dictionary<SampleGeneratingArgs, Vector2>();
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
