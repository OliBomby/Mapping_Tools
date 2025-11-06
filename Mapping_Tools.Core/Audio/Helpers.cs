using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Mapping_Tools.Core.Audio.SampleGeneration;
using Mapping_Tools.Core.BeatmapHelper.Enums;

namespace Mapping_Tools.Core.Audio;

public static class Helpers {
    public static WaveStream OpenSample(string filename) {
        return OpenSample(filename, File.OpenRead(filename));
    }

    public static WaveStream OpenSample(string filename, Stream stream) {
        return Path.GetExtension(filename) switch {
            ".wav" => new WaveFileReader(stream),
            ".aiff" => new AiffFileReader(stream),
            ".aif" => new AiffFileReader(stream),
            ".mp3" => new Mp3FileReader(stream),
            ".ogg" => new VorbisWaveReader(stream),
            _ => throw new ArgumentException("Unrecognized file extension.", nameof(filename))
        };
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
    /// Creates a new wave file at the given location using the wave audio.
    /// </summary>
    /// <param name="filename">The file path</param>
    /// <param name="sourceProvider">The audio to write</param>
    /// <returns>Whether the write was a success</returns>
    public static bool CreateWaveFile(string filename, IWaveProvider sourceProvider) {
        try {
            using var writer = new WaveFileWriter(filename, sourceProvider.WaveFormat);
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

            return true;
        } catch (IndexOutOfRangeException) {
            return false;
        }
    }

    /// <summary>
    /// Writes a new wave file into the stream using the wave audio.
    /// </summary>
    /// <param name="outStream">The stream to write to</param>
    /// <param name="sourceProvider">The audio to write</param>
    /// <returns>Whether the write was a success</returns>
    public static bool CreateWaveFile(Stream outStream, IWaveProvider sourceProvider) {
        try {
            using (var writer = new WaveFileWriter(outStream, sourceProvider.WaveFormat)) {
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

    /// <summary>
    /// Preloads all sample generators efficiently.
    /// </summary>
    /// <param name="sampleGenerators">The samples to import</param>
    public static void PreloadSampleGenerators(IEnumerable<ISampleGenerator> sampleGenerators) {
        // Group the args by path so the SoundFont importer can benefit of caching
        var separatedByPath = new Dictionary<string, HashSet<ISampleGenerator>>();
        var otherGenerators = new HashSet<ISampleGenerator>();

        foreach (var generator in sampleGenerators) {
            if (generator is IFromPathGenerator pathGenerator) {
                if (separatedByPath.TryGetValue(pathGenerator.Path, out HashSet<ISampleGenerator> value)) {
                    value.Add(generator);
                } else {
                    separatedByPath.Add(pathGenerator.Path, new HashSet<ISampleGenerator> { generator });
                }
            } else if (generator != null) {
                otherGenerators.Add(generator);
            }
        }

        // Import all samples
        foreach (var pair in separatedByPath) {
            PreloadFast(pair.Value);

            if (Path.GetExtension(pair.Key) == ".sf2") {
                // Collect garbage to clean up big SoundFont object
                GC.Collect();
            }
        }

        PreloadFast(otherGenerators);
    }

    private static void PreloadFast(IEnumerable<ISampleGenerator> sampleGenerators) {
        foreach (var generator in sampleGenerators) {
            try {
                if (generator is IPreloadableGenerator preloadableGenerator) {
                    preloadableGenerator.PreloadSample();
                }
            } catch (Exception e) {
                Console.WriteLine(e);
            }
        }
    }

    /// <summary>
    /// Gets the hitsound file name without extension. For example: "soft-sliderslide"
    /// </summary>
    /// <param name="sampleSet">The sample set of the hitsound.</param>
    /// <param name="name">The name of the hitsound.</param>
    /// <param name="index">The sample index of the hitsound.</param>
    /// <returns>The filename without extension.</returns>
    public static string GetHitsoundFilename(SampleSet sampleSet, string name, int index = 1) {
        return $"{sampleSet.ToString().ToLower()}-{name}{(index == 1 ? string.Empty : index.ToInvariant())}";
    }


    /// <summary>
    /// Gets the hitsound file name without extension. For example: "normal-hitclap3"
    /// </summary>
    /// <param name="sampleSet">The sample set of the hitsound.</param>
    /// <param name="hitsound">The type of hitsound.</param>
    /// <param name="index">The sample index of the hitsound.</param>
    /// <returns>The filename without extension.</returns>
    public static string GetHitsoundFilename(SampleSet sampleSet, Hitsound hitsound, int index = 1) {
        return $"{sampleSet.ToString().ToLower()}-hit{hitsound.ToString().ToLower()}{(index == 1 ? string.Empty : index.ToInvariant())}";
    }

    /// <summary>
    /// Gets the sample set from a standard osu! hitsound filename notation.
    /// Returns <see cref="SampleSet.None"/> for invalid input.
    /// </summary>
    /// <param name="filename">The filename to get the sample set from.</param>
    /// <returns>The sample set in the filename.</returns>
    public static SampleSet GetSamplesetFromFilename(string filename) {
        string[] split = filename.Split('-');
        if (split.Length == 0)
            return SampleSet.None;
        string sampleset = split[0];
        switch (sampleset) {
            case "none":
                return SampleSet.None;
            case "normal":
                return SampleSet.Normal;
            case "soft":
                return SampleSet.Soft;
            case "drum":
                return SampleSet.Drum;
            default:
                return SampleSet.None;
        }
    }

    /// <summary>
    /// Gets the hitsound type from a standard osu! hitsound filename notation.
    /// Returns <see cref="Hitsound.Normal"/> for invalid input.
    /// </summary>
    /// <param name="filename">The filename to get the hitsound from.</param>
    /// <returns>The hitsound type in the filename.</returns>
    public static Hitsound GetHitsoundFromFilename(string filename) {
        string[] split = filename.Split('-');
        if (split.Length < 2)
            return Hitsound.Normal;
        string hitsound = split[1];
        if (hitsound.Contains("hitnormal"))
            return Hitsound.Normal;
        if (hitsound.Contains("hitwhistle"))
            return Hitsound.Whistle;
        if (hitsound.Contains("hitfinish"))
            return Hitsound.Finish;
        if (hitsound.Contains("hitclap"))
            return Hitsound.Clap;
        return Hitsound.Normal;
    }

    /// <summary>
    /// Gets the custom sample index from a standard osu! hitsound filename notation.
    /// Returns 0 for invalid input.
    /// </summary>
    /// <param name="filename">The filename to get custom sample index from.</param>
    /// <returns>The custom sample index in the filename.</returns>
    public static int GetIndexFromFilename(string filename) {
        var match = Regex.Match(filename, "^(normal|soft|drum)-(hit(normal|whistle|finish|clap)|slidertick|sliderslide)");

        var remainder = filename.Substring(match.Index + match.Length);
        int index = 0;
        if (!string.IsNullOrEmpty(remainder)) {
            InputParsers.TryParseInt(remainder, out index);
        }

        return index;
    }
}