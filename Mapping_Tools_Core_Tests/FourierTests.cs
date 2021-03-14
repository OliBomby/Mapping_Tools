using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Mapping_Tools_Core.MathUtil;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NAudio.Wave;

namespace Mapping_Tools_Core_Tests {
    [TestClass]
    public class FourierTests {
        [TestMethod]
        public void FourierTest() {
            const int expectedBPM = 165;
            var reader = new MediaFoundationReader("Resources\\04 Compute It With Some Devilish Alcoholic Steampunk Engines.mp3");
            var sampleProvider = reader.ToSampleProvider().ToMono();
            var sampleRate = sampleProvider.WaveFormat.SampleRate;

            // Just take one minute
            var allSamples = new float[sampleRate * 60];
            int samplesRead = sampleProvider.Read(allSamples, 0, allSamples.Length);

            // Now you have the entire wav audio stored in the list

            var tasks = new List<Task<Tuple<int, double>>>();
            for (int bpm = 30; bpm < 300; bpm++) {
                int bpm2 = bpm;
                tasks.Add(Task.Run(() => {
                    double hz = bpm2 / 60d;
                    Vector2 result = Vector2.Zero;
                    int i = 0;
                    foreach (var sample in allSamples) {
                        Vector2 unit = Vector2.Rotate(Vector2.One, 2 * Math.PI * hz / sampleRate * i++);
                        result += unit * Math.Abs(sample);
                    }

                    return new Tuple<int, double>(bpm2, result.Length);
                }));
            }

            Task t = Task.WhenAll(tasks);
            t.Wait();

            int bestBPM = 0;
            double bestResonance = 0;
            foreach (var task in tasks) {
                if (task.Result.Item2 > bestResonance) {
                    bestResonance = task.Result.Item2;
                    bestBPM = task.Result.Item1;
                }
            }

            Console.WriteLine($"Found BPM: {bestBPM}");
            Assert.AreEqual(expectedBPM, bestBPM);
        }
    }
}
