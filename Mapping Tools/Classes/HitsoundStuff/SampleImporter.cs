using NAudio.SoundFont;
using NAudio.Vorbis;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.HitsoundStuff {
    class SampleImporter {
        public static readonly string[] ValidSamplePathExtensions = new string[] { ".wav", ".ogg", ".mp3", ".sf2" };

        public static bool ValidateSampleArgs(string path) {
            if (!File.Exists(path))
                return false;

            else if (!ValidSamplePathExtensions.Contains(Path.GetExtension(path)))
                return false;

            return true;
        }

        public static bool ValidateSampleArgs(SampleGeneratingArgs args) {
            return ValidateSampleArgs(args.Path);
        }

        public static bool ValidateSampleArgs(SampleGeneratingArgs args, Dictionary<SampleGeneratingArgs, SampleSoundGenerator> loadedSamples) {
            if (loadedSamples == null)
                return ValidateSampleArgs(args);
            return loadedSamples.ContainsKey(args) && loadedSamples[args] != null;
        }

        public static WaveStream OpenSample(string path) {
            return Path.GetExtension(path) == ".ogg" ? (WaveStream)new VorbisWaveReader(path) : new MediaFoundationReader(path);
        }

        public static Dictionary<SampleGeneratingArgs, SampleSoundGenerator> ImportSamples(IEnumerable<SampleGeneratingArgs> argsList) {
            var samples = new Dictionary<SampleGeneratingArgs, SampleSoundGenerator>();
            var seperatedByPath = new Dictionary<string, HashSet<SampleGeneratingArgs>>();

            foreach (var args in argsList) {
                if (seperatedByPath.TryGetValue(args.Path, out HashSet<SampleGeneratingArgs> value)) {
                    value.Add(args);
                } else {
                    seperatedByPath.Add(args.Path, new HashSet<SampleGeneratingArgs>() { args });
                }
            }

            foreach (var pair in seperatedByPath) {
                var path = pair.Key;
                if (!ValidateSampleArgs(path))
                    continue;
                try {
                    if (Path.GetExtension(path) == ".sf2") {
                        var sf2 = new SoundFont(path);
                        foreach (var args in pair.Value) {
                            var sample = ImportFromSoundFont(args, sf2);
                            samples.Add(args, sample);
                        }
                    } else if (Path.GetExtension(path) == ".ogg") {
                        foreach (var args in pair.Value) {
                            samples.Add(args, new SampleSoundGenerator(new VorbisWaveReader(path)));
                        }
                    } else {
                        foreach (var args in pair.Value) {
                            samples.Add(args, new SampleSoundGenerator(new AudioFileReader(path)));
                        }
                    }
                } catch (Exception ex) { Console.WriteLine(ex.Message); }
                GC.Collect();
            }
            return samples;
        }

        public static SampleSoundGenerator ImportSample(SampleGeneratingArgs args) {
            string path = args.Path;
            if (Path.GetExtension(path) == ".sf2") {
                SoundFont sf2 = new SoundFont(path);
                SampleSoundGenerator wave = ImportFromSoundFont(args, sf2);
                GC.Collect();
                return wave;
            } else if (Path.GetExtension(path) == ".ogg") {
                return new SampleSoundGenerator(new VorbisWaveReader(path));
            } else {
                return new SampleSoundGenerator(new AudioFileReader(path));
            }
        }

        public static SampleSoundGenerator ImportFromSoundFont(SampleGeneratingArgs args, SoundFont sf2) {
            SampleSoundGenerator wave = null;

            foreach (var preset in sf2.Presets) {
                //Console.WriteLine("Preset: " + preset.Name);
                //Console.WriteLine("Preset num: " + preset.PatchNumber);
                //Console.WriteLine("Preset bank: " + preset.Bank);
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
            return wave;
        }

        private static SampleSoundGenerator ImportPreset(SoundFont sf2, Preset preset, SampleGeneratingArgs args) {
            SampleSoundGenerator wave = null;

            Zone closest = null;
            int bdist = int.MaxValue;
            for (int index = 0; index < preset.Zones.Length; index++) { // perc. bank likely has more than one instrument here.
                var pzone = preset.Zones[index];

                var i = pzone.Instrument();
                if (i == null)
                    continue;

                //if (index < 4) {
                //    Console.WriteLine("Instrument: " + pzone.Instrument().Name);
                //}

                if (index != args.Instrument && args.Instrument != -1) {
                    continue;
                }

                // an Instrument contains a set of zones that contain sample headers.
                foreach (var izone in i.Zones) {
                    var sh = izone.SampleHeader();
                    if (sh == null)
                        continue;

                    //Console.WriteLine(sh.SampleName);
                    //Console.WriteLine(izone.Key());
                    //Console.WriteLine(sh.SampleRate);
                    //Console.WriteLine(sh.Start);

                    // Requested key/velocity must also fit in the key/velocity range of the sample
                    ushort keyRange = izone.KeyRange();
                    byte keyLow = (byte)keyRange;
                    byte keyHigh = (byte)(keyRange >> 8);
                    if (!(args.Key >= keyLow && args.Key <= keyHigh) && args.Key != -1 && keyRange != 0) {
                        continue;
                    }
                    ushort velRange = izone.VelocityRange();
                    byte velLow = (byte)keyRange;
                    byte velHigh = (byte)(keyRange >> 8);
                    if (!(args.Velocity >= velLow && args.Velocity <= velHigh) && args.Velocity != -1 && velRange != 0) {
                        continue;
                    }

                    // Get the closest key possible
                    int dist = Math.Abs(args.Key - izone.Key());

                    if (dist < bdist || args.Key == -1) {
                        closest = izone;
                        bdist = dist;
                    }
                }
            }
            //Console.WriteLine("closest: " + closest);
            if (closest != null) {
                wave = GenerateSample(closest, sf2.SampleData, args);
                return wave;
            }

            return wave;
        }

        private static SampleSoundGenerator GenerateSample(Zone izone, byte[] sample, SampleGeneratingArgs args) {
            // Read the sample mode to apply the correct lengthening algorithm
            // Add volume sample provider for the velocity argument
            
            var sh = izone.SampleHeader();
            int sampleMode = izone.SampleModes();

            byte key = izone.Key();
            int keyCorrection = args.Key != -1 ? args.Key - key : 0;
            byte velocity = izone.Velocity();
            float volumeCorrection = args.Velocity != -1 ? (float)args.Velocity / velocity : 1f;

            var output = GetSampleWithLength(sh, izone, sampleMode, sample, args);

            output.KeyCorrection = keyCorrection;
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
            lengthInSeconds = Math.Min(lengthInSeconds, length / (double)sh.SampleRate);

            int numberOfSamples = (int)Math.Ceiling(lengthInSeconds * sh.SampleRate);
            int numberOfBytes = numberOfSamples * 2;

            byte[] buffer = new byte[numberOfBytes];
            Array.Copy(sample, start * 2, buffer, 0, numberOfBytes);

            var output = new SampleSoundGenerator(BufferToWaveStream(buffer, sh.SampleRate));

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

            var output = new SampleSoundGenerator(BufferToWaveStream(buffer, sh.SampleRate)) {
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

            return new SampleSoundGenerator(BufferToWaveStream(buffer, sh.SampleRate));
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

        public static ISampleProvider VolumeChange(ISampleProvider sample, float mult) {
            return new VolumeSampleProvider(sample) { Volume = mult };
        }
    }
}
