using System;
using System.Collections.Generic;
using Mapping_Tools_Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;

namespace Mapping_Tools_Core_Tests {
    [TestClass]
    public class FourierTests {
        [TestMethod]
        public void FourierTest() {
            var reader = new MediaFoundationReader("sample.wav");
            var sampleProvider = reader.ToSampleProvider().ToMono();
            var sampleRate = sampleProvider.WaveFormat.SampleRate;

            var allSamples = new List<float>();
            var buffer = new float[sampleProvider.WaveFormat.AverageBytesPerSecond * 4];
            while (true) {
                int samplesRead = sampleProvider.Read(buffer, 0, buffer.Length);
                if (samplesRead == 0) {
                    // end of source provider
                    break;
                }

                allSamples.AddRange(buffer);
            }

            // Now you have the entire wav audio stored in the list

            for (int bpm = 30; bpm < 300; bpm++) {
                double hz = bpm / 60d;

                Vector2 result = Vector2.Zero;
                int i = 0;
                foreach (var sample in allSamples) {
                    Vector2 unit = Vector2.Rotate(Vector2.One, 2 * Math.PI * hz / sampleRate * i);

                    result += unit * Math.Abs(sample);

                    i++;
                }

                Console.WriteLine($"BPM = {bpm}; resonance = {result.Length}");
            }
        }
    }
}
