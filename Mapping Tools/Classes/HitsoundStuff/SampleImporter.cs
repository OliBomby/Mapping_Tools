using NAudio.SoundFont;
using NAudio.Vorbis;
using NAudio.Wave;
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

        public static bool ValidateSamplePath(string path) {
            if (path == "")
                return false;

            string[] split = path.Split('?');
            string first = split[0];

            if (!File.Exists(first))
                return false;

            if (Path.GetExtension(first) == ".sf2") {
                if (split.Length < 2)
                    return false;

                if (!Regex.IsMatch(split[1], @"[0-9]+(\\[0-9]+){3}"))
                    return false;
            } else if (split.Length > 1)
                return false;
            else if (!ValidSamplePathExtensions.Contains(Path.GetExtension(first)))
                return false;

            return true;
        }

        public static WaveStream ImportSample(string path) {
            string[] split = path.Split('?');
            string p = split[0];
            if (Path.GetExtension(p) == ".sf2") {
                int[] args = split[1].Split('\\').Select(o => int.Parse(o)).ToArray();

                SoundFont sf2 = new SoundFont(p);

                foreach (var preset in sf2.Presets) {
                    Console.WriteLine("Processing " + preset.Name);
                    ImportPreset(sf2, preset);
                }

                return null;
            } else if (Path.GetExtension(p) == ".ogg") {
                return new VorbisWaveReader(p);
            } else {
                return new MediaFoundationReader(p);
            }
        }

        private static void ImportPreset(SoundFont sf2, Preset preset) {
            /*
            foreach (var pzone in preset.Zones) { // perc. bank likely has more than one instrument here.
                var i = pzone.Instrument();
                var kr = pzone.KeyRange(); // FIXME: where should I use it?
                if (i == null)
                    continue; // FIXME: is it possible?

                var vr = pzone.VelocityRange();

                // an Instrument contains a set of zones that contain sample headers.
                foreach (var izone in i.Zones) {
                    var ikr = izone.KeyRange();
                    var ivr = izone.VelocityRange();
                    var sh = izone.SampleHeader();
                    if (sh == null)
                        continue; // FIXME: is it possible?

                    // FIXME: sample data must become monoral (panpot neutral)
                    var xs = ReadSample(sh, sf2.SampleData);
                }
            }*/
            return;
        }

        private static WaveStream ReadSample(SampleHeader sh, byte[] sample) {
            // Indices in sf2 are numbers of samples, not byte length. So double them.
            int length = (int)(sh.End - sh.Start) * 2;
            return new RawSourceWaveStream(sample, (int)sh.Start * 2, length, new WaveFormat((int)sh.SampleRate, 16, 1));
        }
    }
}
