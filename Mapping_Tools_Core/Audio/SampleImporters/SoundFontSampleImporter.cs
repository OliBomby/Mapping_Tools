using Mapping_Tools_Core.Audio.Midi;
using Mapping_Tools_Core.Audio.SampleGeneration;
using Mapping_Tools_Core.Audio.SampleGeneration.Decorators;
using NAudio.SoundFont;
using NAudio.Wave;
using System;

namespace Mapping_Tools_Core.Audio.SampleImporters {
    public class SoundFontSampleImporter {
        // Store last SoundFont to allow multi-import from the same SoundFont object.
        private static WeakReference<SoundFont> lastSoundFont;
        private static string lastPath;

        private readonly string path;
        private readonly SoundFont soundFontArg;

        public SoundFontSampleImporter(string path) {
            this.path = path;
        }

        public SoundFontSampleImporter(SoundFont soundFont) {
            soundFontArg = soundFont;
        }

        public IAudioSampleGenerator Import(IMidiNote args) {
            SoundFont soundFont = null;
            if (soundFontArg == null) {
                if (lastPath == path) {
                    lastSoundFont?.TryGetTarget(out soundFont);
                }

                if (soundFont == null) {
                    // Either the path is different or there was no SoundFont stored
                    soundFont = new SoundFont(path);

                    lastSoundFont = new WeakReference<SoundFont>(soundFont, true);
                    lastPath = path;
                }
            } else {
                soundFont = soundFontArg;
            }

            return ImportFromSoundFont(soundFont, args);
        }

        
        // TODO: format soundfont import to detect file versions and types of files. 
        public IAudioSampleGenerator ImportFromSoundFont(SoundFont sf2, IMidiNote args) {
            IAudioSampleGenerator wave = null;

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

        private void SoundFontDebug(SoundFont sf) {
            Console.WriteLine(sf);
            Console.WriteLine(@"Number of presets: " + sf.Presets.Length);
            Console.WriteLine(@"Number of instruments: " + sf.Instruments.Length);
            Console.WriteLine(@"Number of instruments: " + sf.SampleHeaders.Length);
        }

        private static bool InRange(int val, ushort range) {
            byte velLow = (byte)range;
            byte velHigh = (byte)(range >> 8);
            return (val >= velLow && val <= velHigh) || val != -1 || range != 0;
        }

        private IAudioSampleGenerator ImportPreset(SoundFont sf2, Preset preset, IMidiNote args) {
            /*
                == Aproximate Pseudo Code of importing a preset from sf2 ==
                -  Find the first instrument in the preset that has a compatible key range and velocity range.
                -    Find the first sample in the instrument that has a compatible key range and velocity range.
                -      Generate the sample with the right key and velocity.
                -    Apply any modulators from the sample zone in the instrument. (currently none compatible)
                -  Apply any modulators from the instrument zone in the preset. (currently none compatible)
            */

            foreach (var instrumentZone in preset.Zones) {
                if (!(InRange(args.Key, instrumentZone.KeyRange()) && 
                      InRange(args.Velocity, instrumentZone.VelocityRange()))) {
                    continue;
                }

                var instrument = instrumentZone.Instrument();

                if (instrument == null)
                    continue;

                var sampleGenerator = ImportInstrument(sf2, instrument, args);

                if (sampleGenerator != null) {
                    return sampleGenerator;
                }
            }

            return null;
        }

        private IAudioSampleGenerator ImportInstruments(SoundFont sf2, IMidiNote args) {
            foreach (var instrument in sf2.Instruments) { // perccusion bank likely has more than one instrument here.
                if (instrument == null)
                    continue;

                var sampleGenerator = ImportInstrument(sf2, instrument, args);

                if (sampleGenerator != null) {
                    return sampleGenerator;
                };
            }

            return null;
        }

        private IAudioSampleGenerator ImportInstrument(SoundFont sf2, Instrument i, IMidiNote args) {
            // An Instrument contains a set of zones that contain sample headers.
            foreach (var sampleZone in i.Zones) {
                var sh = sampleZone.SampleHeader();
                if (sh == null)
                    continue;

                // Requested key/velocity must also fit in the key/velocity range of the sample
                ushort keyRange = sampleZone.KeyRange();
                if (InRange(args.Key, keyRange)) {
                    continue;
                }

                ushort velRange = sampleZone.VelocityRange();
                if (InRange(args.Velocity, velRange)) {
                    continue;
                }

                var wave = GenerateSample(sampleZone, sf2.SampleData, args);
                return wave;
            }

            return null;
        }

        private IAudioSampleGenerator GenerateSample(Zone sampleZone, byte[] sample, IMidiNote args) {
            // Read the sample mode to apply the correct lengthening algorithm
            // Add volume sample provider for the velocity argument
            IAudioSampleGenerator output = GetSampleWithLength(sampleZone, sample, args);

            byte velocity = sampleZone.Velocity();
            float volumeCorrection = args.Velocity != -1 ? (float) args.Velocity / velocity : 1f;
            output = new AmplitudeSampleDecorator(output, volumeCorrection);

        return output;
        }

        private IAudioSampleGenerator GetSampleWithLength(Zone sampleZone, byte[] sample, IMidiNote args) {
            int sampleMode = sampleZone.SampleModes();
            switch (sampleMode) {
                case 0:
                case 2:
                    // Don't loop
                    return GetSampleWithoutLoop(sampleZone, sample, args);
                case 1:
                    // Loop continuously
                    return GetSampleContinuous(sampleZone, sample, args);
                default:
                    // Loops for the duration of key depression then proceed to play the remainder of the sample
                    return GetSampleRemainder(sampleZone, sample, args);
            }
        }

        private IAudioSampleGenerator GetSampleWithoutLoop(Zone sampleZone, byte[] sample, IMidiNote args) {
            var sh = sampleZone.SampleHeader();

            // Indices in sf2 are numbers of samples, not byte length. So double them
            int start = (int)sh.Start + sampleZone.FullStartAddressOffset();
            int end = (int)sh.End + sampleZone.FullEndAddressOffset();

            int length = end - start;

            double lengthInSeconds = args.Length != -1 ? (args.Length / 1000) + 0.4 : length / (double)sh.SampleRate;

            // Sample rate key correction
            int keyCorrectionCents = args.Key != -1 ? (args.Key - sampleZone.Key()) * 100 + sampleZone.TotalTuningCents() : 0;
            double factor = Math.Pow(2, keyCorrectionCents / 12000d);
            lengthInSeconds *= factor;

            lengthInSeconds = Math.Min(lengthInSeconds, length / (double)sh.SampleRate);

            int numberOfSamples = (int)Math.Ceiling(lengthInSeconds * sh.SampleRate);
            int numberOfBytes = numberOfSamples * 2;

            byte[] buffer = new byte[numberOfBytes];
            Array.Copy(sample, start * 2, buffer, 0, numberOfBytes);

            var wave = BufferToWaveStream(buffer, (uint) (sh.SampleRate * factor));
            var sampleGenerator = new RawAudioSampleGenerator(wave);

            double fadeStart;
            double fadeLength;
            if (lengthInSeconds <= 0.4) {
                fadeStart = lengthInSeconds * 0.7;
                fadeLength = lengthInSeconds * 0.2;
            } else {
                fadeStart = lengthInSeconds - 0.4;
                fadeLength = 0.3;
            }

            var output = new FadingSampleDecorator(sampleGenerator, fadeStart * 1000, fadeLength * 1000);

            return output;
        }

        private IAudioSampleGenerator GetSampleContinuous(Zone sampleZone, byte[] sample, IMidiNote args) {
            var sh = sampleZone.SampleHeader();

            // Indices in sf2 are numbers of samples, not byte length. So double them
            int start = (int)sh.Start + sampleZone.FullStartAddressOffset();
            int end = (int)sh.End + sampleZone.FullEndAddressOffset();
            int startLoop = (int)sh.StartLoop + sampleZone.FullStartLoopAddressOffset();
            int endLoop = (int)sh.EndLoop + sampleZone.FullEndLoopAddressOffset();

            int length = end - start;
            int lengthBytes = length * 2;
            int loopLength = endLoop - startLoop;
            int loopLengthBytes = loopLength * 2;

            double lengthInSeconds = args.Length != -1 ? (args.Length / 1000) + 0.4 : length / (double)sh.SampleRate + 0.4;

            // Sample rate key correction
            int keyCorrectionCents = args.Key != -1 ? (args.Key - sampleZone.Key()) * 100 + sampleZone.TotalTuningCents() : 0;
            double factor = Math.Pow(2, keyCorrectionCents / 12000d);
            lengthInSeconds *= factor;

            int numberOfSamples = (int)Math.Ceiling(lengthInSeconds * sh.SampleRate);
            int numberOfLoopSamples = numberOfSamples - length;

            if (numberOfLoopSamples < 0) {
                return GetSampleWithoutLoop(sampleZone, sample, args);
            }

            int numberOfBytes = numberOfSamples * 2;

            byte[] buffer = new byte[numberOfBytes];

            Array.Copy(sample, start * 2, buffer, 0, lengthBytes);
            for (int i = 0; i < (numberOfLoopSamples + loopLength - 1) / loopLength; i++) {
                Array.Copy(sample, startLoop * 2, buffer, lengthBytes + i * loopLengthBytes, Math.Min(loopLengthBytes, numberOfBytes - (lengthBytes + i * loopLengthBytes)));
            }

            var wave = BufferToWaveStream(buffer, (uint) (sh.SampleRate * factor));
            var sampleGenerator = new RawAudioSampleGenerator(wave);
            var output = new FadingSampleDecorator(sampleGenerator, (lengthInSeconds - 0.4) * 1000, 0.3 * 1000);

            return output;
        }

        private IAudioSampleGenerator GetSampleRemainder(Zone sampleZone, byte[] sample, IMidiNote args) {
            var sh = sampleZone.SampleHeader();

            // Indices in sf2 are numbers of samples, not byte length. So double them
            int start = (int)sh.Start + sampleZone.FullStartAddressOffset();
            int end = (int)sh.End + sampleZone.FullEndAddressOffset();
            int startLoop = (int)sh.StartLoop + sampleZone.FullStartLoopAddressOffset();
            int endLoop = (int)sh.EndLoop + sampleZone.FullEndLoopAddressOffset();

            int length = end - start;
            int loopLength = endLoop - startLoop;
            int loopLengthBytes = loopLength * 2;

            int lengthFirstHalf = startLoop - start;
            int lengthFirstHalfBytes = lengthFirstHalf * 2;

            int lengthSecondHalf = end - endLoop;
            int lengthSecondHalfBytes = lengthSecondHalf * 2;
            
            double lengthInSeconds = args.Length != -1 ? (args.Length / 1000) : length / (double)sh.SampleRate;

            // Sample rate key correction
            int keyCorrectionCents = args.Key != -1 ? (args.Key - sampleZone.Key()) * 100 + sampleZone.TotalTuningCents() : 0;
            double factor = Math.Pow(2, keyCorrectionCents / 12000d);
            lengthInSeconds *= factor;

            int numberOfSamples = (int)Math.Ceiling(lengthInSeconds * sh.SampleRate);
            numberOfSamples += lengthSecondHalf;
            int numberOfLoopSamples = numberOfSamples - lengthFirstHalf - lengthSecondHalf;

            if (numberOfLoopSamples < loopLength) {
                return GetSampleWithoutLoop(sampleZone, sample, args);
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

            var wave = BufferToWaveStream(buffer, (uint) (sh.SampleRate * factor));
            var sampleGenerator = new RawAudioSampleGenerator(wave);

            return sampleGenerator;
        }

        private static WaveStream BufferToWaveStream(byte[] buffer, uint sampleRate) {
            return new RawSourceWaveStream(buffer, 0, buffer.Length, new WaveFormat((int)sampleRate, 16, 1));
        }
    }
}