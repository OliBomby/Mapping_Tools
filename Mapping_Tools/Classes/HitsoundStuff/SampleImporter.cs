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
            return (File.Exists(path) && ValidSamplePathExtensions.Contains(Path.GetExtension(path)));
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
            return Path.GetExtension(path) == ".ogg" ? (WaveStream)new VorbisWaveReader(path) : new MediaFoundationReader(path);
        }

        /// <summary>
        /// Imports all samples specified by <see cref="SampleGeneratingArgs"/> and returns a dictionary which maps the <see cref="SampleGeneratingArgs"/>
        /// to their <see cref="SampleSoundGenerator"/>. If a sample couldn't be imported then it has a null instead.
        /// </summary>
        /// <param name="argsList"></param>
        /// <returns></returns>
        public static Dictionary<SampleGeneratingArgs, SampleSoundGenerator> ImportSamples(IEnumerable<SampleGeneratingArgs> argsList, SampleGeneratingArgsComparer comparer = null) {
            if (comparer == null)
                comparer = new SampleGeneratingArgsComparer();

            var samples = new Dictionary<SampleGeneratingArgs, SampleSoundGenerator>(comparer);
            var separatedByPath = new Dictionary<string, HashSet<SampleGeneratingArgs>>();

            foreach (var args in argsList) {
                if (separatedByPath.TryGetValue(args.Path, out HashSet<SampleGeneratingArgs> value)) {
                    value.Add(args);
                } else {
                    separatedByPath.Add(args.Path, new HashSet<SampleGeneratingArgs>(comparer) { args });
                }
            }

            foreach (var pair in separatedByPath) {
                var path = pair.Key;
                if (!ValidateSamplePath(path)) {
                    foreach (var args in pair.Value) {
                        samples.Add(args, null);
                    }
                    continue;
                }

                try {
                    switch (Path.GetExtension(path)) {
                        case ".sf2": {
                            var sf2 = new SoundFont(path);
                            foreach (var args in pair.Value) {
                                var sample = ImportFromSoundFont(args, sf2);
                                samples.Add(args, sample);
                            }

                            break;
                        }
                        case ".ogg": {
                            foreach (var args in pair.Value) {
                                samples.Add(args, ImportFromVorbis(args));
                            }

                            break;
                        }
                        default: {
                            foreach (var args in pair.Value) {
                                samples.Add(args, ImportFromAudio(args));
                            }

                            break;
                        }
                    }
                } catch (Exception ex) {
                    Console.WriteLine(ex.Message); 
                    
                    foreach (var args in pair.Value) {
                        samples.Add(args, null);
                    }
                }
                GC.Collect();
            }
            return samples;
        }

        public static SampleSoundGenerator ImportSample(SampleGeneratingArgs args) {
            string path = args.Path;
            switch (Path.GetExtension(path)) {
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
            var generator = new SampleSoundGenerator(new AudioFileReader(args.Path)) {
                VolumeCorrection = (float)args.Volume
            };
            return generator;
        }

        public static SampleSoundGenerator ImportFromVorbis(SampleGeneratingArgs args) {
            var generator = new SampleSoundGenerator(new VorbisWaveReader(args.Path)) {
                VolumeCorrection = (float)args.Volume
            };
            return generator;
        }
        
        // TODO: format soundfont import to detect file versions and types of files. 
        public static SampleSoundGenerator ImportFromSoundFont(SampleGeneratingArgs args, SoundFont sf2) {
            SampleSoundGenerator wave = null;

            foreach (var preset in sf2.Presets) {
                if (preset.PatchNumber != args.Patch && args.Patch != -1) {
                    continue;
                }
                if (preset.Bank != args.Bank && args.Bank != -1) {
                    continue;
                }

                wave = ImportPreset(sf2, preset, args);
                if (wave != null)
                    break;
            }

            return wave ?? ImportInstruments(sf2, args);
        }

        private static void SoundFontDebug(SoundFont sf) {
            Console.WriteLine(sf);
            Console.WriteLine(@"Number of presets: " + sf.Presets.Length);
            Console.WriteLine(@"Number of instruments: " + sf.Instruments.Length);
            Console.WriteLine(@"Number of instruments: " + sf.SampleHeaders.Length);
        }

        private static SampleSoundGenerator ImportPreset(SoundFont sf2, Preset preset, SampleGeneratingArgs args) {
            /*
                == Aproximate Pesdo Code of importing a preset from sf2 ==
                -    Get all preset zones from soundfont (sf2)
                -    get the instrument and double check if it's not null (if it is, "continue" the method)
                -    is the instrument grabbed the same as what args is asking for? (if not, "continue" method)
                -    get each zone within instrument and create wav file from information
                    -   get Sample Header of instrument zone and double check if null (if so, "continue")
                    -   get the Key range of spesified zone and create high and low keys 8 bit's in difference.
                        -   is the key not the same as what the args asked for? (yes, then "continue")
                    -   Get the velocity range of spesified zone and create high and low keys 8 bits' in difference.
                        -   is the velocity not the same as what the args asked for? (yes, then "continue")
                    -   Find the closest key from instrument zone to the key spesified from args.
                        - is the closest key lower than the maximum integer value or, is the key of args just not spesified?
                            - if so, set the closest zone (initial = null) to the current zone, 
                            - if so, set the bdist (initial = maximum integer value) to the closest key.
                    -   Is there a zone found from above?
                        - If so, create a wave from zone information using the SampleData.
            */
            return ImportInstruments(sf2, preset.Zones.Select(z => z.Instrument()), args);
        }

        private static SampleSoundGenerator ImportInstruments(SoundFont sf2, SampleGeneratingArgs args) {
            return ImportInstruments(sf2, sf2.Instruments, args);
        }

        private static SampleSoundGenerator ImportInstruments(SoundFont sf2, IEnumerable<Instrument> instruments, SampleGeneratingArgs args) {
            Zone closest = null;
            int i = 0;
            int bdist = int.MaxValue;
            
            foreach (var instrument in instruments) { // perccusion bank likely has more than one instrument here.
                if (instrument == null)
                    continue;

                if (i++ != args.Instrument && args.Instrument != -1) {
                    continue;
                }

                var iZone = ImportInstrument(instrument, args);

                if (iZone == null) continue;

                // Get closest instrument from the zones in the preset
                int dist = Math.Abs(args.Key - iZone.Key());

                if (dist < bdist || args.Key == -1) {
                    closest = iZone;
                    bdist = dist;
                }
            }

            if (closest == null) return null;

            //Console.WriteLine("closest: " + closest.Key());
            var wave = GenerateSample(closest, sf2.SampleData, args);
            return wave;
        }

        private static Zone ImportInstrument(Instrument i, SampleGeneratingArgs args) {
            Zone closest = null;
            int bdist = int.MaxValue;

            // an Instrument contains a set of zones that contain sample headers.
            foreach (var instrumentZone in i.Zones) {
                var sh = instrumentZone.SampleHeader();
                if (sh == null)
                    continue;

                // Requested key/velocity must also fit in the key/velocity range of the sample
                ushort keyRange = instrumentZone.KeyRange();
                byte keyLow = (byte)keyRange;
                byte keyHigh = (byte)(keyRange >> 8);
                if (!(args.Key >= keyLow && args.Key <= keyHigh) && args.Key != -1 && keyRange != 0) {
                    continue;
                }
                ushort velRange = instrumentZone.VelocityRange();
                byte velLow = (byte)keyRange;
                byte velHigh = (byte)(keyRange >> 8);
                if (!(args.Velocity >= velLow && args.Velocity <= velHigh) && args.Velocity != -1 && velRange != 0) {
                    continue;
                }

                // Get the closest key possible
                int dist = Math.Abs(args.Key - instrumentZone.Key());

                if (dist < bdist || args.Key == -1) {
                    closest = instrumentZone;
                    bdist = dist;
                }
            }

            return closest;
        }

        private static SampleSoundGenerator GenerateSample(Zone izone, byte[] sample, SampleGeneratingArgs args) {
            // Read the sample mode to apply the correct lengthening algorithm
            // Add volume sample provider for the velocity argument
            
            var sh = izone.SampleHeader();
            int sampleMode = izone.SampleModes();

            byte key = izone.Key();
            byte velocity = izone.Velocity();
            double volumeCorrection = args.Velocity != -1 ? (double)args.Velocity / velocity : 1d;

            var output = GetSampleWithLength(sh, izone, sampleMode, sample, args);

            // Key correction is 0 to not use the shitty pitch shifter
            output.KeyCorrection = 0;
            output.VolumeCorrection = volumeCorrection;

            return output;
        }

        private static SampleSoundGenerator GetSampleWithLength(SampleHeader sh, Zone izone, int sampleMode, byte[] sample, SampleGeneratingArgs args) {
            if (sampleMode == 0 || sampleMode == 2) {
                // Don't loop
                return GetSampleWithoutLoop(sh, izone, sample, args);
            } else if (sampleMode == 1) {
                // Loop continuously
                return GetSampleContinuous(sh, izone, sample, args);
            } else {
                // Loops for the duration of key depression then proceed to play the remainder of the sample
                return GetSampleRemainder(sh, izone, sample, args);
            }
        }

        private static SampleSoundGenerator GetSampleWithoutLoop(SampleHeader sh, Zone izone, byte[] sample, SampleGeneratingArgs args) {
            // Indices in sf2 are numbers of samples, not byte length. So double them
            int start = (int)sh.Start + izone.FullStartAddressOffset();
            int end = (int)sh.End + izone.FullEndAddressOffset();

            int length = end - start;

            double lengthInSeconds = args.Length != -1 ? (args.Length / 1000) + 0.4 : length / (double)sh.SampleRate;

            // Sample rate key correction
            int keyCorrection = args.Key != -1 ? args.Key - izone.Key() : 0;
            double factor = Math.Pow(2, keyCorrection / 12d);
            lengthInSeconds *= factor;

            lengthInSeconds = Math.Min(lengthInSeconds, length / (double)sh.SampleRate);

            int numberOfSamples = (int)Math.Ceiling(lengthInSeconds * sh.SampleRate);
            int numberOfBytes = numberOfSamples * 2;

            byte[] buffer = new byte[numberOfBytes];
            Array.Copy(sample, start * 2, buffer, 0, numberOfBytes);

            var output = new SampleSoundGenerator(BufferToWaveStream(buffer, (uint)(sh.SampleRate * factor)));

            if (lengthInSeconds <= 0.4) {
                output.FadeStart = lengthInSeconds * 0.7;
                output.FadeLength = lengthInSeconds * 0.2;
            } else {
                output.FadeStart = lengthInSeconds - 0.4;
                output.FadeLength = 0.3;
            }

            return output;
        }

        private static SampleSoundGenerator GetSampleContinuous(SampleHeader sh, Zone izone, byte[] sample, SampleGeneratingArgs args) {
            // Indices in sf2 are numbers of samples, not byte length. So double them
            int start = (int)sh.Start + izone.FullStartAddressOffset();
            int end = (int)sh.End + izone.FullEndAddressOffset();
            int startLoop = (int)sh.StartLoop + izone.FullStartLoopAddressOffset();
            int endLoop = (int)sh.EndLoop + izone.FullEndLoopAddressOffset();

            int length = end - start;
            int lengthBytes = length * 2;
            int loopLength = endLoop - startLoop;
            int loopLengthBytes = loopLength * 2;

            double lengthInSeconds = args.Length != -1 ? (args.Length / 1000) + 0.4 : length / (double)sh.SampleRate + 0.4;

            // Sample rate key correction
            int keyCorrection = args.Key != -1 ? args.Key - izone.Key() : 0;
            double factor = Math.Pow(2, keyCorrection / 12d);
            lengthInSeconds *= factor;

            int numberOfSamples = (int)Math.Ceiling(lengthInSeconds * sh.SampleRate);
            int numberOfLoopSamples = numberOfSamples - length;

            if (numberOfLoopSamples < 0) {
                return GetSampleWithoutLoop(sh, izone, sample, args);
            }

            int numberOfBytes = numberOfSamples * 2;

            byte[] buffer = new byte[numberOfBytes];

            Array.Copy(sample, start * 2, buffer, 0, lengthBytes);
            for (int i = 0; i < (numberOfLoopSamples + loopLength - 1) / loopLength; i++) {
                Array.Copy(sample, startLoop * 2, buffer, lengthBytes + i * loopLengthBytes, Math.Min(loopLengthBytes, numberOfBytes - (lengthBytes + i * loopLengthBytes)));
            }

            var output = new SampleSoundGenerator(BufferToWaveStream(buffer, (uint)(sh.SampleRate * factor))) {
                FadeStart = lengthInSeconds - 0.4,
                FadeLength = 0.3
            };

            return output;
        }

        private static SampleSoundGenerator GetSampleRemainder(SampleHeader sh, Zone izone, byte[] sample, SampleGeneratingArgs args) {
            // Indices in sf2 are numbers of samples, not byte length. So double them
            int start = (int)sh.Start + izone.FullStartAddressOffset();
            int end = (int)sh.End + izone.FullEndAddressOffset();
            int startLoop = (int)sh.StartLoop + izone.FullStartLoopAddressOffset();
            int endLoop = (int)sh.EndLoop + izone.FullEndLoopAddressOffset();

            int length = end - start;
            int loopLength = endLoop - startLoop;
            int loopLengthBytes = loopLength * 2;

            int lengthFirstHalf = startLoop - start;
            int lengthFirstHalfBytes = lengthFirstHalf * 2;

            int lengthSecondHalf = end - endLoop;
            int lengthSecondHalfBytes = lengthSecondHalf * 2;
            
            double lengthInSeconds = args.Length != -1 ? (args.Length / 1000) : length / (double)sh.SampleRate;

            // Sample rate key correction
            int keyCorrection = args.Key != -1 ? args.Key - izone.Key() : 0;
            double factor = Math.Pow(2, keyCorrection / 12d);
            lengthInSeconds *= factor;

            int numberOfSamples = (int)Math.Ceiling(lengthInSeconds * sh.SampleRate);
            numberOfSamples += lengthSecondHalf;
            int numberOfLoopSamples = numberOfSamples - lengthFirstHalf - lengthSecondHalf;

            if (numberOfLoopSamples < loopLength) {
                return GetSampleWithoutLoop(sh, izone, sample, args);
            }

            int numberOfBytes = numberOfSamples * 2;
            int numberOfLoopBytes = numberOfLoopSamples * 2;

            byte[] buffer = new byte[numberOfBytes];
            byte[] bufferLoop = new byte[numberOfLoopBytes];

            Array.Copy(sample, start * 2, buffer, 0, lengthFirstHalfBytes);
            for (int i = 0; i < (numberOfLoopSamples + loopLength - 1) / loopLength; i++) {
                Array.Copy(sample, startLoop * 2, bufferLoop, i * loopLengthBytes, Math.Min(loopLengthBytes, numberOfLoopBytes - i * loopLengthBytes));
            }
            bufferLoop.CopyTo(buffer, lengthFirstHalfBytes);
            Array.Copy(sample, start * 2, buffer, lengthFirstHalfBytes + numberOfLoopBytes, lengthSecondHalfBytes);

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

        public static ISampleProvider PitchShift(ISampleProvider sample, int correction) {
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
