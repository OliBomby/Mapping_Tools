using Mapping_Tools_Core.Audio.SampleImportArgs;
using Mapping_Tools_Core.Audio.SampleSoundGeneration;
using Mapping_Tools_Core.Audio.SampleSoundGeneration.Decorators;
using NAudio.SoundFont;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Mapping_Tools_Core.Audio.SampleImporters {
    public class SoundFontSampleImporter : ISampleImporter<ISoundFontSampleImportArgs> {
        private static SoundFontSampleImporter instance;

        public static SoundFontSampleImporter GetInstance() {
            return instance ?? (instance = new SoundFontSampleImporter());
        }

        // Store last SoundFont to allow multi-import from the same SoundFont object.
        private WeakReference<SoundFont> lastSoundFont;
        private string lastPath;

        public ISampleSoundGenerator Import(ISoundFontSampleImportArgs args) {
            SoundFont soundFont = null;
            if (lastPath == args.Path) {
                lastSoundFont?.TryGetTarget(out soundFont);
            }

            if (soundFont == null) {
                // Either the path is different or there was no SoundFont stored
                soundFont = new SoundFont(args.Path);

                lastSoundFont = new WeakReference<SoundFont>(soundFont, true);
                lastPath = args.Path;
            }

            return ImportFromSoundFont(args, soundFont);
        }

        
        // TODO: format soundfont import to detect file versions and types of files. 
        public static ISampleSoundGenerator ImportFromSoundFont(ISoundFontSampleImportArgs args, SoundFont sf2) {
            ISampleSoundGenerator wave = null;

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

        private static ISampleSoundGenerator ImportPreset(SoundFont sf2, Preset preset, ISoundFontSampleImportArgs args) {
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

        private static ISampleSoundGenerator ImportInstruments(SoundFont sf2, ISoundFontSampleImportArgs args) {
            return ImportInstruments(sf2, sf2.Instruments, args);
        }

        private static ISampleSoundGenerator ImportInstruments(SoundFont sf2, IEnumerable<Instrument> instruments, ISoundFontSampleImportArgs args) {
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

        private static Zone ImportInstrument(Instrument i, ISoundFontSampleImportArgs args) {
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

        private static ISampleSoundGenerator GenerateSample(Zone izone, byte[] sample, ISoundFontSampleImportArgs args) {
            // Read the sample mode to apply the correct lengthening algorithm
            // Add volume sample provider for the velocity argument
            
            var sh = izone.SampleHeader();
            int sampleMode = izone.SampleModes();

            byte key = izone.Key();
            byte velocity = izone.Velocity();
            float volumeCorrection = args.Velocity != -1 ? (float)args.Velocity / velocity : 1f;

            ISampleSoundGenerator output = GetSampleWithLength(sh, izone, sampleMode, sample, args);

            output = new AmplitudeSampleSoundDecorator(output, volumeCorrection);

            return output;
        }

        private static ISampleSoundGenerator GetSampleWithLength(SampleHeader sh, Zone izone, int sampleMode, byte[] sample, ISoundFontSampleImportArgs args) {
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

        private static ISampleSoundGenerator GetSampleWithoutLoop(SampleHeader sh, Zone izone, byte[] sample, ISoundFontSampleImportArgs args) {
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

            var sampleSoundGenerator = new WaveStreamSampleSoundGenerator(BufferToWaveStream(buffer, (uint)(sh.SampleRate * factor)));

            double fadeStart;
            double fadeLength;
            if (lengthInSeconds <= 0.4) {
                fadeStart = lengthInSeconds * 0.7;
                fadeLength = lengthInSeconds * 0.2;
            } else {
                fadeStart = lengthInSeconds - 0.4;
                fadeLength = 0.3;
            }

            var output = new FadingSampleSoundDecorator(sampleSoundGenerator, fadeStart, fadeLength);

            return output;
        }

        private static ISampleSoundGenerator GetSampleContinuous(SampleHeader sh, Zone izone, byte[] sample, ISoundFontSampleImportArgs args) {
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

            var output = new FadingSampleSoundDecorator(
                new WaveStreamSampleSoundGenerator(BufferToWaveStream(buffer, (uint)(sh.SampleRate * factor))),
                lengthInSeconds - 0.4,
                0.3
            );

            return output;
        }

        private static ISampleSoundGenerator GetSampleRemainder(SampleHeader sh, Zone izone, byte[] sample, ISoundFontSampleImportArgs args) {
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

            return new WaveStreamSampleSoundGenerator(BufferToWaveStream(buffer, (uint)(sh.SampleRate * factor)));
        }

        private static WaveStream BufferToWaveStream(byte[] buffer, uint sampleRate) {
            return new RawSourceWaveStream(buffer, 0, buffer.Length, new WaveFormat((int)sampleRate, 16, 1));
        }
    }
}