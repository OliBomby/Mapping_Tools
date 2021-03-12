using NAudio.FileFormats.Wav;
using NAudio.Wave;
using System;
using System.IO;

namespace Mapping_Tools_Core.Audio.Exporting {
    public class PathAudioSampleExporter : IPathAudioSampleExporter {
        public string CopyPath { get; set; }
        public bool CanCopyPaste { get; set; }
        public bool BlankSample { get; set; }
        public bool ClippingPossible { get; set; }

        public string ExportFolder { get; set; }
        public string ExportName { get; set; }

        private readonly IStreamAudioSampleExporter exporter;
        private readonly bool forceSpecificFormat;

        private int audioTracks;

        /// <summary>
        /// Constructs a new PathAudioSampleExporter with no set export destination.
        /// </summary>
        /// <param name="exporter">The exporter to write to file</param>
        /// <param name="forceSpecificFormat">Makes sure the exported sample uses the file format of the <see cref="exporter"/></param>
        public PathAudioSampleExporter(IStreamAudioSampleExporter exporter, bool forceSpecificFormat = false) : this(
            null, null, exporter, forceSpecificFormat) { }

        /// <summary>
        /// Constructs a new PathAudioSampleExporter.
        /// </summary>
        /// <param name="exportFolder">The path to the folder export the sample to</param>
        /// <param name="exportName">The filename of the exported sample without extension</param>
        /// <param name="exporter">The exporter to write to file</param>
        /// <param name="forceSpecificFormat">Makes sure the exported sample uses the file format of the <see cref="exporter"/></param>
        public PathAudioSampleExporter(string exportFolder, string exportName, IStreamAudioSampleExporter exporter, bool forceSpecificFormat=false) {
            ExportFolder = exportFolder;
            ExportName = exportName;
            this.exporter = exporter;
            this.forceSpecificFormat = forceSpecificFormat;
            Reset();
        }

        public void Reset() {
            CopyPath = null;
            CanCopyPaste = true;
            BlankSample = true;
            ClippingPossible = false;
            audioTracks = 0;
            exporter.Reset();
        }

        public virtual bool Flush() {
            string dest;

            if (audioTracks == 1 && CanCopyPaste && File.Exists(CopyPath)) {
                dest = Path.Combine(ExportFolder, ExportName + Path.GetExtension(CopyPath));

                // Try to filter the copying if the exporter has some specific format we want to export with
                if (forceSpecificFormat && !exporter.BlankSample && exporter.GetDesiredExtension() != null) {
                    // Check if the audio format of the file at CopyPath matches that of what the exporter wants to generate
                    if (Path.GetExtension(CopyPath) == exporter.GetDesiredExtension()) {
                        // Do a special check for .wav files. The encoding has to match
                        if (Path.GetExtension(CopyPath) == ".wav" && exporter.GetDesiredWaveEncoding().HasValue) {
                            var waveFormat = GetWaveFormat(CopyPath);
                            // ReSharper disable once PossibleInvalidOperationException
                            if (waveFormat != null && waveFormat.Encoding == exporter.GetDesiredWaveEncoding().Value) {
                                var result = CopySample(CopyPath, dest);
                                Reset();
                                return result;
                            }
                        } else {
                            var result = CopySample(CopyPath, dest);
                            Reset();
                            return result;
                        }
                    }
                } else {
                    var result = CopySample(CopyPath, dest);
                    Reset();
                    return result;
                }
            }

            dest = Path.Combine(ExportFolder, ExportName + Path.GetExtension(CopyPath));
            using (var outStream = new FileStream(dest, FileMode.Create, FileAccess.Write, FileShare.Read)) {
                exporter.OutStream = outStream;
                var result = exporter.Flush();
                Reset();
                return result;
            }
        }

        private static WaveFormat GetWaveFormat(string path) {
            try {
                using var wave = new WaveFileReader(path);
                return wave.WaveFormat;
            }
            catch {
                return null;
            }
        }

        private static bool CopySample(string path, string dest) {
            try {
                File.Copy(path, dest, true);
                return true;
            } catch (Exception ex) {
                Console.WriteLine($@"{ex.Message} while copying sample {path} to {dest}.");
                return false;
            }
        }

        public void AddAudio(ISampleProvider sample) {
            audioTracks++;
            exporter.AddAudio(sample);
        }

        public ISampleProvider PopAudio() {
            audioTracks--;
            return exporter.PopAudio();
        }

        public string GetDesiredExtension() {
            return exporter.GetDesiredExtension();
        }

        public WaveFormatEncoding? GetDesiredWaveEncoding() {
            return exporter.GetDesiredWaveEncoding();
        }
    }
}