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
        public static readonly string[] ValidSamplePathExtensions = new string[] { ".wav", ".ogg", ".mp3" };

        public static bool ValidateSampleArgs(string path) {
            if (path == "")
                return false;

            string[] split = path.Split('?');
            string first = split[0];

            if (!File.Exists(first))
                return false;

            if (Path.GetExtension(first) == ".sf2") {
                if (split.Length < 2)
                    return false;

                if (!Regex.IsMatch(split[1], @"[0-9]+(\\[0-9]+){4}"))
                    return false;
            } else if (split.Length > 1)
                return false;
            else if (!ValidSamplePathExtensions.Contains(Path.GetExtension(first)))
                return false;

            return true;
        }

        public static bool ValidateSampleArgs(SampleGeneratingArgs args) {
            return ValidateSampleArgs(args.Path);
        }

        public static ISampleProvider ImportSample(SampleGeneratingArgs args) {
            string path = args.Path;
            if (Path.GetExtension(path) == ".sf2") {
                SoundFont sf2 = new SoundFont(path);
                ISampleProvider wave = null;

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
                }

                GC.Collect();
                return wave;
            } else if (Path.GetExtension(path) == ".ogg") {
                return new VorbisWaveReader(path);
            } else {
                return new AudioFileReader(path);
            }
        }

        private static ISampleProvider ImportPreset(SoundFont sf2, Preset preset, SampleGeneratingArgs args) {
            ISampleProvider wave = null;

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
                    if (!(args.Key >= keyLow && args.Key <= keyHigh) && args.Key != -1) {
                        continue;
                    }
                    ushort velRange = izone.VelocityRange();
                    byte velLow = (byte)keyRange;
                    byte velHigh = (byte)(keyRange >> 8);
                    if (!(args.Velocity >= velLow && args.Velocity <= velHigh) && args.Velocity != -1) {
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

        private static ISampleProvider GenerateSample(Zone izone, byte[] sample, SampleGeneratingArgs args) {
            // read the sample mode to apply the correct lengthening algorithm
            // find out what unit that midi length is actually in
            // add volume sample provider for the velocity argument

            var sh = izone.SampleHeader();
            byte key = izone.Key();
            //Console.WriteLine("generating: " + sh.SampleName);

            // Indices in sf2 are numbers of samples, not byte length. So double them.
            int length = (int)(sh.End - sh.Start);
            int loopLength = (int)(sh.EndLoop - sh.StartLoop);

            //Console.WriteLine("length: " + length);
            //Console.WriteLine("loop length: " + loopLength);

            double lengthInSeconds = args.Length / 1000;
            double numberOfSamples = lengthInSeconds * sh.SampleRate;

            int numberOfBytes = (int)(Math.Min(numberOfSamples, length) * 2);

            byte[] buffer = new byte[numberOfBytes];
            Array.Copy(sample, (int)sh.Start * 2, buffer, 0, numberOfBytes);

            ISampleProvider output = new Pcm16BitToSampleProvider(new RawSourceWaveStream(buffer, 0, numberOfBytes, new WaveFormat((int)sh.SampleRate, 16, 1)));

            int correction = args.Key - key;
            //Console.WriteLine("correction: " + correction);
            return PitchShift(output, correction);
        }

        private static ISampleProvider PitchShift(ISampleProvider sample, int correction) {
            float factor = (float)Math.Pow(2, correction / 12f);
            SmbPitchShiftingSampleProvider shifter = new SmbPitchShiftingSampleProvider(sample, 512, 4, factor);
            return shifter;
        }
    }
}
