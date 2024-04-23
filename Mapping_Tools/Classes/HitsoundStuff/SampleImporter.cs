using NAudio.SoundFont;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mapping_Tools.Classes.HitsoundStuff {
    class SampleImporter {
        // TODO: Redo importing of soundfonts to include all legacy
        //          and new versions of soundfonts (which should be included in NAudio)
        // ".sfz", ".sf1", ".ssx", ".sfpack", ".sfark"
        public static readonly string[] ValidSamplePathExtensions = { 
            ".wav", ".ogg", ".mp3", ".sf2"};

        public static bool ValidateSamplePath(string path) {
            return File.Exists(path) && ValidSamplePathExtensions.Contains(Path.GetExtension(path).ToLower());
        }

        public static bool ValidateSampleArgs(SampleGeneratingArgs args, bool validateSampleFile = true) {
            return !validateSampleFile || ValidateSamplePath(args.Path);
        }

        public static bool ValidateSampleArgs(SampleGeneratingArgs args, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples, bool validateSampleFile = true) {
            if (loadedSamples == null)
                return ValidateSampleArgs(args, validateSampleFile);
            return !validateSampleFile || loadedSamples.ContainsKey(args) && loadedSamples[args] != null;
        }
  
        public static WaveStream OpenSample(string path) {
            return Path.GetExtension(path).ToLower() == ".ogg" ? (WaveStream)new VorbisWaveReader(path) : new MediaFoundationReader(path);
        }

        /// <summary>
        /// Imports all samples specified by <see cref="SampleGeneratingArgs"/> and returns a dictionary which maps the <see cref="SampleGeneratingArgs"/>
        /// to their <see cref="SampleSoundGenerator"/>. If a sample couldn't be imported then it has a null instead.
        /// </summary>
        /// <param name="argsList">The samples to import.</param>
        /// <param name="comparer">Custom equality comparer.</param>
        /// <returns></returns>
        public static Dictionary<SampleGeneratingArgs, SampleSoundGenerator> ImportSamples(IEnumerable<SampleGeneratingArgs> argsList, SampleGeneratingArgsComparer comparer = null) {
            comparer ??= new SampleGeneratingArgsComparer();

            var samples = new Dictionary<SampleGeneratingArgs, SampleSoundGenerator>(comparer);
            var separatedByPath = new Dictionary<string, HashSet<SampleGeneratingArgs>>();

            foreach (var args in argsList) {
                if (separatedByPath.TryGetValue(args.Path, out HashSet<SampleGeneratingArgs> value)) {
                    value.Add(args);
                } else {
                    separatedByPath.Add(args.Path, new HashSet<SampleGeneratingArgs>(comparer) { args });
                }
            }

            foreach ((string path, HashSet<SampleGeneratingArgs> value) in separatedByPath) {
                if (!ValidateSamplePath(path)) {
                    foreach (var args in value) {
                        samples.Add(args, null);
                    }
                    continue;
                }

                try {
                    switch (Path.GetExtension(path).ToLower()) {
                        case ".sf2": {
                            var sf2 = new SoundFont(path);
                            foreach (var args in value) {
                                var sample = ImportFromSoundFont(args, sf2);
                                samples.Add(args, sample);
                            }

                            break;
                        }
                        case ".ogg": {
                            foreach (var args in value) {
                                samples.Add(args, ImportFromVorbis(args));
                            }

                            break;
                        }
                        default: {
                            foreach (var args in value) {
                                samples.Add(args, ImportFromAudio(args));
                            }

                            break;
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message);

                    foreach (var args in value) {
                        samples.Add(args, null);
                    }
                }
                GC.Collect();
            }

            return samples;
        }

        /// <summary>
        /// Imports all samples specified by <see cref="SampleGeneratingArgs"/> and returns a dictionary which maps the <see cref="SampleGeneratingArgs"/>
        /// to an <see cref="Exception"/> if a sample couldn't be imported for whatever reason.
        /// </summary>
        /// <param name="argsList">The samples to validate.</param>
        /// <param name="comparer">Custom equality comparer.</param>
        /// <returns></returns>
        public static Dictionary<SampleGeneratingArgs, Exception> ValidateSamples(IEnumerable<SampleGeneratingArgs> argsList, SampleGeneratingArgsComparer comparer = null) {
            comparer ??= new SampleGeneratingArgsComparer();

            var sampleExceptions = new Dictionary<SampleGeneratingArgs, Exception>(comparer);
            var separatedByPath = new Dictionary<string, HashSet<SampleGeneratingArgs>>();

            foreach (var args in argsList) {
                if (separatedByPath.TryGetValue(args.Path, out HashSet<SampleGeneratingArgs> value)) {
                    value.Add(args);
                } else {
                    separatedByPath.Add(args.Path, new HashSet<SampleGeneratingArgs>(comparer) { args });
                }
            }

            foreach ((string path, HashSet<SampleGeneratingArgs> value) in separatedByPath) {
                if (!File.Exists(path)) {
                    foreach (var args in value) {
                        sampleExceptions.Add(args, new FileNotFoundException("File not found", path));
                    }
                    continue;
                }

                if (!ValidateSamplePath(path)) {
                    foreach (var args in value) {
                        sampleExceptions.Add(args, new InvalidDataException("Invalid file extension"));
                    }
                    continue;
                }

                switch (Path.GetExtension(path).ToLower()) {
                    case ".sf2": {
                        var sf2 = new SoundFont(path);

                        foreach (var args in value) {
                            try {
                                ImportFromSoundFont(args, sf2);
                                sampleExceptions.Add(args, null);
                            } catch (Exception ex) {
                                sampleExceptions.Add(args, ex);
                            }
                        }

                        break;
                    }
                    case ".ogg": {
                        foreach (var args in value) {
                            try {
                                ImportFromVorbis(args);
                                sampleExceptions.Add(args, null);
                            } catch (Exception ex) {
                                sampleExceptions.Add(args, ex);
                            }
                        }

                        break;
                    }
                    default: {
                        foreach (var args in value) {
                            try {
                                ImportFromAudio(args);
                                sampleExceptions.Add(args, null);
                            } catch (Exception ex) {
                                sampleExceptions.Add(args, ex);
                            }
                        }

                        break;
                    }
                }

                GC.Collect();
            }

            return sampleExceptions;
        }

        public static SampleSoundGenerator ImportSample(SampleGeneratingArgs args) {
            string path = args.Path;
            switch (Path.GetExtension(path).ToLower()) {
                case ".sf2": {
                    SoundFont sf2 = new SoundFont(path);
                    SampleSoundGenerator wave = ImportFromSoundFont(args, sf2);
                    GC.Collect();
                    return wave;
                }
                case ".ogg":
                    return ImportFromVorbis(args);
                default:
                    return ImportFromAudio(args);
            }
        }

        public static SampleSoundGenerator ImportFromAudio(SampleGeneratingArgs args) {
            var generator = new SampleSoundGenerator(new MediaFoundationReader(args.Path)) {
                VolumeCorrection = args.Volume,
                Panning = args.Panning,
                PitchShift = args.PitchShift
            };
            return generator;
        }

        public static SampleSoundGenerator ImportFromVorbis(SampleGeneratingArgs args) {
            var generator = new SampleSoundGenerator(new VorbisWaveReader(args.Path)) {
                VolumeCorrection = args.Volume,
                Panning = args.Panning,
                PitchShift = args.PitchShift
            };
            return generator;
        }
        
        // TODO: format soundfont import to detect file versions and types of files. 
        public static SampleSoundGenerator ImportFromSoundFont(SampleGeneratingArgs args, SoundFont sf2) {
            SampleSoundGenerator[] sounds = Array.Empty<SampleSoundGenerator>();

            foreach (var preset in sf2.Presets) {
                if (preset.PatchNumber != args.Patch && args.Patch != -1) {
                    continue;
                }
                if (preset.Bank != args.Bank && args.Bank != -1) {
                    continue;
                }

                sounds = ImportPreset(sf2, preset, args).ToArray();
                if (sounds.Length > 0)
                    break;
            }

            // If there are no presets, import all instruments
            if (sounds.Length == 0) {
                sounds = ImportInstruments(sf2, args).ToArray();
            }

            // If no samples were found, return null
            if (sounds.Length == 0) {
                return null;
            }

            // Synchronize the sample rate and channels for all samples so they can be mixed
            int maxSampleRate = Math.Min(sounds.Max(o => o.Wave.WaveFormat.SampleRate), 44100);
            foreach (SampleSoundGenerator sound in sounds) {
                sound.SampleRate = maxSampleRate;
                sound.Channels = 2;
            }

            // Mix into single sound generator
            var generator = new SampleSoundGenerator(sounds) {
                Panning = args.Panning,
                PitchShift = args.PitchShift
            };

            return generator;
        }

        private static void SoundFontDebug(SoundFont sf) {
            Console.WriteLine(sf);
            Console.WriteLine(@"Number of presets: " + sf.Presets.Length);
            Console.WriteLine(@"Number of instruments: " + sf.Instruments.Length);
            Console.WriteLine(@"Number of instruments: " + sf.SampleHeaders.Length);
        }

        private static IEnumerable<SampleSoundGenerator> ImportPreset(SoundFont sf2, Preset preset, SampleGeneratingArgs args) {
            if (args.Instrument != -1)
                return ImportInstrument(sf2, preset.Zones[args.Instrument].Instrument(), args);

            // Import all layers that match the key range and velocity range
            return ValidZones(preset.Zones, args).SelectMany(zone => ImportInstrument(sf2, zone.Instrument(), args));
        }

        private static IEnumerable<Zone> ValidZones(IEnumerable<Zone> zones, SampleGeneratingArgs args) {
            var validZones = zones.Where(z => IsZoneValid(z, args.Key, args.Velocity)).ToList();

            if (validZones.Count == 0)
                return validZones;

            if (args.Key != -1 && args.Velocity != -1)
                return validZones;

            // If there are multiple valid zones, and we have a wildcard key/velocity, we want to select the zones that could occur in a single key/velocity
            // so we find all the zones with overlap with the first zone
            var firstZone = validZones[0];
            return validZones.Where(z =>
                    RangeOverlap(firstZone.KeyRange(), z.KeyRange()) && RangeOverlap(firstZone.VelocityRange(), z.VelocityRange())).ToList();
        }

        private static bool RangeOverlap(ushort range1, ushort range2) {
            byte low1 = (byte)range1;
            byte high1 = (byte)(range1 >> 8);
            byte low2 = (byte)range2;
            byte high2 = (byte)(range2 >> 8);
            return range1 == 0 || range2 == 0 || low1 <= high2 && high1 >= low2;
        }

        private static bool IsZoneValid(Zone zone, int key, int velocity) {
            // Requested key/velocity must also fit in the key/velocity range of the sample
            ushort keyRange = zone.KeyRange();
            byte keyLow = (byte)keyRange;
            byte keyHigh = (byte)(keyRange >> 8);

            ushort velRange = zone.VelocityRange();
            byte velLow = (byte)velRange;
            byte velHigh = (byte)(velRange >> 8);

            return (velRange == 0 || velocity == -1 || velocity >= velLow && velocity <= velHigh) &&
                   (keyRange == 0 || key      == -1 || key      >= keyLow && key      <= keyHigh);
        }

        private static IEnumerable<SampleSoundGenerator> ImportInstruments(SoundFont sf2, SampleGeneratingArgs args) {
            return args.Instrument != -1
                ? ImportInstrument(sf2, sf2.Instruments[args.Instrument], args).ToList()
                : sf2.Instruments.SelectMany(i => ImportInstrument(sf2, i, args)).ToList();
        }

        private static IEnumerable<SampleSoundGenerator> ImportInstrument(SoundFont sf2, Instrument i, SampleGeneratingArgs args) {
            return ValidZones(i.Zones, args).Where(z => z.SampleHeader() is not null).Select(z => GenerateSample(sf2, z, args));
        }

        private static SampleSoundGenerator GenerateSample(SoundFont sf2, Zone zone, SampleGeneratingArgs args) {
            // Read the sample mode to apply the correct lengthening algorithm
            // Add volume sample provider for the velocity argument
            
            var sh = zone.SampleHeader();
            int sampleMode = zone.SampleModes();

            var output = GetSampleWithLength(sh, zone, sampleMode, sf2.SampleData, args);

            // Velocity is a linear multiplier of the amplitude
            byte velocity = zone.Velocity();
            double volumeCorrection = args.Velocity != -1 ? (double)args.Velocity / velocity : 1d;
            double attenuationCorrection = Math.Pow(10, zone.Attenuation() / -10d);
            output.AmplitudeCorrection = volumeCorrection * attenuationCorrection;
            output.Panning = zone.Pan();

            return output;
        }

        private static SampleSoundGenerator GetSampleWithLength(SampleHeader sh, Zone zone, int sampleMode, byte[] sample, SampleGeneratingArgs args) {
            switch (sampleMode) {
                case 1:
                    // Loop continuously
                    return GetSampleContinuous(sh, zone, sample, args);
                case 3:
                    // Loops for the duration of key depression then proceed to play the remainder of the sample
                    return GetSampleRemainder(sh, zone, sample, args);
                default:
                    // Don't loop
                    return GetSampleWithoutLoop(sh, zone, sample, args);
            }
        }

        private static readonly int bytesPerSample = 2;

        private static SampleSoundGenerator GetSampleWithoutLoop(SampleHeader sh, Zone zone, byte[] sample, SampleGeneratingArgs args) {
            // Indices in sf2 are numbers of samples, not byte length. So double them
            int start = (int)sh.Start + zone.FullStartAddressOffset();
            int end = (int)sh.End + zone.FullEndAddressOffset();
            int length = end - start;

            bool doFade = args.Length >= 0 && args.Length / 1000 < length / (double)sh.SampleRate;

            // Sample rate key correction
            int keyCorrection = args.Key != -1 ? args.Key - zone.Key() : 0;
            double factor = Math.Pow(2, keyCorrection / 12d);

            int numberOfBytes = length * bytesPerSample;
            byte[] buffer = new byte[numberOfBytes];
            Array.Copy(sample, start * bytesPerSample, buffer, 0, numberOfBytes);

            var output = new SampleSoundGenerator(BufferToWaveStream(buffer, (uint)(sh.SampleRate * factor)));

            if (!doFade)
                return output;

            output.FadeStart = args.Length / 1000;
            output.FadeLength = 0.3;

            return output;
        }

        private static SampleSoundGenerator GetSampleContinuous(SampleHeader sh, Zone zone, byte[] sample, SampleGeneratingArgs args) {
            if (args.Length < 0)
                return GetSampleWithoutLoop(sh, zone, sample, args);

            // Indices in sf2 are numbers of samples, not byte length. So double them
            int start = (int)sh.Start + zone.FullStartAddressOffset();
            int startLoop = (int)sh.StartLoop + zone.FullStartLoopAddressOffset();
            int endLoop = (int)sh.EndLoop + zone.FullEndLoopAddressOffset();

            int lengthFirstHalf = startLoop - start;
            int loopLength = endLoop - startLoop;

            double lengthInSeconds = args.Length / 1000;

            // Sample rate key correction
            int keyCorrection = args.Key != -1 ? args.Key - zone.Key() : 0;
            double factor = Math.Pow(2, keyCorrection / 12d);
            lengthInSeconds *= factor;
            lengthInSeconds += 0.4;  // The last 0.4 seconds is fade-out

            int numberOfSamples = (int)Math.Ceiling(lengthInSeconds * sh.SampleRate);
            int numberOfLoopSamples = numberOfSamples - lengthFirstHalf;

            if (numberOfLoopSamples <= 0)
                return GetSampleWithoutLoop(sh, zone, sample, args);

            int lengthFirstHalfBytes = lengthFirstHalf * bytesPerSample;
            int loopLengthBytes = loopLength * bytesPerSample;
            int numberOfBytes = numberOfSamples * bytesPerSample;
            byte[] buffer = new byte[numberOfBytes];

            Array.Copy(sample, start * bytesPerSample, buffer, 0, lengthFirstHalf * bytesPerSample);
            for (int i = 0; i < (numberOfLoopSamples + loopLength - 1) / loopLength; i++) {
                Array.Copy(sample, startLoop * bytesPerSample, buffer,
                    lengthFirstHalfBytes + i * loopLengthBytes, Math.Min(loopLengthBytes, numberOfBytes - (lengthFirstHalfBytes + i * loopLengthBytes)));
            }

            var output = new SampleSoundGenerator(BufferToWaveStream(buffer, (uint)(sh.SampleRate * factor))) {
                FadeStart = lengthInSeconds - 0.4,
                FadeLength = 0.3
            };

            return output;
        }

        private static SampleSoundGenerator GetSampleRemainder(SampleHeader sh, Zone zone, byte[] sample, SampleGeneratingArgs args) {
            if (args.Length < 0)
                return GetSampleWithoutLoop(sh, zone, sample, args);

            // Indices in sf2 are numbers of samples, not byte length. So double them
            int start = (int)sh.Start + zone.FullStartAddressOffset();
            int end = (int)sh.End + zone.FullEndAddressOffset();
            int startLoop = (int)sh.StartLoop + zone.FullStartLoopAddressOffset();
            int endLoop = (int)sh.EndLoop + zone.FullEndLoopAddressOffset();

            int loopLength = endLoop - startLoop;
            int loopLengthBytes = loopLength * bytesPerSample;

            int lengthFirstHalf = startLoop - start;
            int lengthFirstHalfBytes = lengthFirstHalf * bytesPerSample;

            int lengthSecondHalf = end - endLoop;
            int lengthSecondHalfBytes = lengthSecondHalf * bytesPerSample;
            
            double lengthInSeconds = args.Length / 1000;

            // Sample rate key correction
            int keyCorrection = args.Key != -1 ? args.Key - zone.Key() : 0;
            double factor = Math.Pow(2, keyCorrection / 12d);
            lengthInSeconds *= factor;

            int numberOfSamples = (int) Math.Ceiling(lengthInSeconds * sh.SampleRate);
            int numberOfLoopSamples = numberOfSamples - lengthFirstHalf;
            numberOfLoopSamples = (numberOfLoopSamples + loopLength - 1) / loopLength * loopLength;
            numberOfSamples = lengthFirstHalf + numberOfLoopSamples + lengthSecondHalf;

            if (numberOfLoopSamples <= 0)
                return GetSampleWithoutLoop(sh, zone, sample, args);

            int numberOfBytes = numberOfSamples * bytesPerSample;
            int numberOfLoopBytes = numberOfLoopSamples * bytesPerSample;

            byte[] buffer = new byte[numberOfBytes];

            Array.Copy(sample, start * bytesPerSample, buffer, 0, lengthFirstHalfBytes);
            for (int i = 0; i < numberOfLoopSamples / loopLength; i++) {
                Array.Copy(sample, startLoop * bytesPerSample, buffer,
                    lengthFirstHalfBytes + i * loopLengthBytes, loopLengthBytes);
            }
            Array.Copy(sample, endLoop * bytesPerSample, buffer, lengthFirstHalfBytes + numberOfLoopBytes, lengthSecondHalfBytes);

            return new SampleSoundGenerator(BufferToWaveStream(buffer, (uint)(sh.SampleRate * factor)));
        }

        private static WaveStream BufferToWaveStream(byte[] buffer, uint sampleRate) {
            return new RawSourceWaveStream(buffer, 0, buffer.Length, new WaveFormat((int)sampleRate, 16, 1));
        }

        public static ISampleProvider SetChannels(ISampleProvider sampleProvider, int channels) {
            if (channels == 1) {
                return MakeMono(sampleProvider);
            } else {
                return MakeStereo(sampleProvider);
            }
        }

        public static ISampleProvider MakeStereo(ISampleProvider sampleProvider) {
            if (sampleProvider.WaveFormat.Channels == 1) {
                return new MonoToStereoSampleProvider(sampleProvider);
            } else {
                return sampleProvider;
            }
        }

        public static ISampleProvider MakeMono(ISampleProvider sampleProvider) {
            if (sampleProvider.WaveFormat.Channels == 2) {
                return new StereoToMonoSampleProvider(sampleProvider);
            } else {
                return sampleProvider;
            }
        }

        public static ISampleProvider PitchShift(ISampleProvider sample, double correction) {
            float factor = (float)Math.Pow(2, correction / 12f);
            SmbPitchShiftingSampleProvider shifter = new SmbPitchShiftingSampleProvider(sample, 1024, 4, factor);
            return shifter;
        }

        public static ISampleProvider VolumeChange(ISampleProvider sample, double volume) {
            return new VolumeSampleProvider(sample) { Volume = (float) VolumeToAmplitude(volume) };
        }

        private static double HeightAt005 => 0.995 * Math.Pow(0.05, 1.5) + 0.005;

        public static double VolumeToAmplitude(double volume) {
            if (volume < 0.05) {
                return HeightAt005 / 0.05 * volume;
            }

            // This formula seems to convert osu! volume to amplitude multiplier
            return 0.995 * Math.Pow(volume, 1.5) + 0.005;
        }

        public static double AmplitudeToVolume(double amplitude) {
            if (amplitude < HeightAt005) {
                return 0.05 / HeightAt005 * amplitude;
            }

            return Math.Pow((amplitude - 0.005) / 0.995, 1 / 1.5);
        }
    }
}
