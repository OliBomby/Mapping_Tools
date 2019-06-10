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
                
                SoundFont soundFont = new SoundFont(p);
                Console.WriteLine(soundFont.FileInfo.ToString());

                foreach (Zone z in soundFont.Instruments[4].Zones) {
                    Console.WriteLine("Zone:");
                    foreach (Generator g in z.Generators) {
                        Console.WriteLine(g.ToString());
                        Console.WriteLine(g.SampleHeader);

                        if (g.SampleHeader != null) {
                            SampleHeader s = g.SampleHeader;

                            if (s.OriginalPitch == args[1]) {
                                Console.WriteLine("yeet found it");
                                int length = (int)(s.End - s.Start);
                                Console.WriteLine("length: " + length);
                                byte[] buffer = new byte[length];
                                Array.Copy(soundFont.SampleData, s.Start, buffer, 0, length);
                                return new RawSourceWaveStream(buffer, 0, length, new WaveFormat((int)s.SampleRate, 2));
                            }
                        }
                    }
                }

                Console.WriteLine("Instruments:");
                foreach (Instrument i in soundFont.Instruments) {
                    Console.WriteLine(i.Name);
                    foreach (Zone z in i.Zones) {
                        Console.WriteLine("Zone:");
                        foreach (Generator g in z.Generators) {
                            Console.WriteLine(g.ToString());
                            Console.WriteLine(g.SampleHeader);

                            if (g.SampleHeader != null) {
                                SampleHeader s = g.SampleHeader;

                                if (s.OriginalPitch == args[1]) {
                                    Console.WriteLine("yeet found it");
                                    int length = (int)(s.End - s.Start);
                                    Console.WriteLine("length: " + length);
                                    byte[] buffer = new byte[length];
                                    Array.Copy(soundFont.SampleData, s.Start, buffer, 0, length);
                                    return new RawSourceWaveStream(buffer, 0, length, new WaveFormat((int)s.SampleRate, 2));
                                }
                            }
                        }
                    }
                }

                return null;
            } else if (Path.GetExtension(p) == ".ogg") {
                return new VorbisWaveReader(p);
            } else {
                return new MediaFoundationReader(p);
            }
        }
    }
}
