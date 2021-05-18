using NAudio.Wave;
using OggVorbisEncoder;
using System;
using System.Collections.Generic;
using System.IO;
using Mapping_Tools.Classes.MathUtil;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class VorbisFileWriter : IDisposable {
        private Stream outStream;
        private OggStream oggStream;
        private ProcessingState processingState;

        // Buffer sizes for various sample rates. These values were found empirically
        private static readonly Dictionary<int, int> startBuffers = new Dictionary<int, int> {
            {48000, 1024}, {44100, 1024}, {32000, 1024}, {22050, 512}, {16000, 512}, {11025, 256}, {8000, 256}
        };

        /// <summary>
        /// VorbisFileWriter that actually writes to a stream
        /// </summary>
        /// <param name="outStream">Stream to be written to</param>
        /// <param name="sampleRate">The sample rate to use</param>
        /// <param name="channels">The number of channels to use</param>
        /// <param name="quality">The base quality for Vorbis encoding</param>
        public VorbisFileWriter(Stream outStream, int sampleRate, int channels, float quality=0.5f) {
            this.outStream = outStream;
            SampleRate = sampleRate;
            Channels = channels;

            if (!startBuffers.ContainsKey(sampleRate)) 
                throw new InvalidOperationException($"Vorbis writer does not support {sampleRate} sample rate.");

            // Stores all the static vorbis bitstream settings
            Console.WriteLine($"Initiating variable bit rate: {channels} channels, {sampleRate} sample rate, {quality} quality");
            var info = VorbisInfo.InitVariableBitRate(channels, sampleRate, quality);

            // set up our packet->stream encoder
            var serial = RNG.Next();
            oggStream = new OggStream(serial);

            // =========================================================
            // HEADER
            // =========================================================
            // Vorbis streams begin with three headers; the initial header (with
            // most of the codec setup parameters) which is mandated by the Ogg
            // bitstream spec.  The second header holds any comment fields.  The
            // third header holds the bitstream codebook.
            var headerBuilder = new HeaderPacketBuilder();
            
            var comments = new Comments();

            var infoPacket = headerBuilder.BuildInfoPacket(info);
            var commentsPacket = headerBuilder.BuildCommentsPacket(comments);
            var booksPacket = headerBuilder.BuildBooksPacket(info);

            oggStream.PacketIn(infoPacket);
            oggStream.PacketIn(commentsPacket);
            oggStream.PacketIn(booksPacket);

            // Flush to force audio data onto its own page per the spec
            FlushPages(oggStream, outStream, true);

            // =========================================================
            // BODY (Audio Data)
            // =========================================================
            processingState = ProcessingState.Create(info);

            // Append some zeros at the start so the result has the same length as the input
            int bufferSize = startBuffers[sampleRate];

            float[][] outSamples = new float[channels][];
            for (int ch = 0; ch < channels; ch++)
                outSamples[ch] = new float[bufferSize];

            processingState.WriteData(outSamples, bufferSize);
        }

        /// <summary>
        /// Creates a new VorbisFileWriter
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="sampleRate">The sample rate to use</param>
        /// <param name="channels">The number of channels to use</param>
        /// <param name="quality">The base quality for Vorbis encoding</param>
        public VorbisFileWriter(string filename, int sampleRate, int channels, float quality=0.5f)
            : this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), sampleRate, channels, quality) {
            Filename = filename;
        }

        /// <summary>
        ///     The vorbis file name or null if not applicable
        /// </summary>
        public string Filename { get; }

        /// <summary>
        /// The sample rate of the output audio
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        /// The number of channels for the output audio
        /// </summary>
        public int Channels { get; }

        public static bool CreateVorbisFile(string filename, IWaveProvider sourceProvider, float quality=0.5f) {
            try {
                using (var writer = new VorbisFileWriter(filename, sourceProvider.WaveFormat.SampleRate, sourceProvider.WaveFormat.Channels, quality)) {
                    var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
                    while (true) {
                        int bytesRead = sourceProvider.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) {
                            // end of source provider
                            break;
                        }

                        writer.WriteWaveData(buffer, bytesRead, sourceProvider.WaveFormat);
                    }
                }

                return true;
            } catch (IndexOutOfRangeException) {
                return false;
            }
        }

        /// <summary>
        /// Returns the best sample rate for the provided sample rate that is supported by the vorbis writer.
        /// </summary>
        public static int GetSupportedSampleRate(int oldSampleRate) {
            int newSampleRate = 48000;
            foreach (var sampleRate in startBuffers.Keys) {
                if (sampleRate >= oldSampleRate && sampleRate <= newSampleRate) {
                    newSampleRate = sampleRate;
                }
            }

            return newSampleRate;
        }

        private static void FlushPages(OggStream oggStream, Stream Output, bool Force)
        {
            while (oggStream.PageOut(out var page, Force)) {
                //Console.WriteLine($"Writing page header with {page.Header.Length} bytes of data");
                Output.Write(page.Header, 0, page.Header.Length);
                //Console.WriteLine($"Writing page body with {page.Body.Length} bytes of data");
                Output.Write(page.Body, 0, page.Body.Length);
            }
        }

        /// <summary>
        ///     Appends bytes to the WaveFile (assumes they are already in the correct format)
        /// </summary>
        /// <param name="data">the buffer containing the wave data</param>
        /// <param name="offset">the offset from which to start writing</param>
        /// <param name="count">the number of bytes to write</param>
        public void Write(byte[] data, int offset, int count) {
            outStream.Write(data, offset, count);
        }

        /// <summary>
        /// Encodes and writes a number of float samples
        /// </summary>
        /// <param name="floatSamples">The samples. The array shape is [channel][sample]</param>
        /// <param name="count">The number of samples to write.</param>
        public void WriteFloatSamples(float[][] floatSamples, int count) {
            //Console.WriteLine($"Writing {count} samples!");
            processingState.WriteData(floatSamples, count);

            while (!oggStream.Finished && processingState.PacketOut(out var packet)) {
                //Console.WriteLine($"Got packet with {packet.PacketData.Length} bytes of data");
                oggStream.PacketIn(packet);

                FlushPages(oggStream, outStream, false);
            }
        }

        /// <summary>
        /// Encodes and writes samples from raw Wave data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="count"></param>
        /// <param name="format"></param>
        public void WriteWaveData(byte[] data, int count, WaveFormat format) {
            int bytesPerSample = format.BitsPerSample / 8;

            double sampleConversion = (float)format.SampleRate / SampleRate;

            int numSamples = count / bytesPerSample / format.Channels;
            int numOutputSamples = (int)(numSamples / sampleConversion);

            float[][] outSamples = new float[Channels][];

            for (int ch = 0; ch < Channels; ch++)
                outSamples[ch] = new float[numOutputSamples];

            switch (format.Encoding) {
                case WaveFormatEncoding.Pcm:
                    for(int sampleNumber = 0; sampleNumber < numOutputSamples; sampleNumber++) {
                        for (int ch = 0; ch < Channels; ch++) {
                            int sampleIndex = (int)(sampleNumber * sampleConversion) * format.Channels * bytesPerSample;

                            if (ch < format.Channels) sampleIndex += ch * bytesPerSample;

                            float rawSample = 0f;
                            switch (format.BitsPerSample) {
                                case 8:
                                    rawSample = ByteToSample(data[sampleIndex]);
                                    break;
                                case 16:
                                    rawSample = ShortToSample((short)(data[sampleIndex + 1] << 8 | data[sampleIndex]));
                                    break;
                            }

                            outSamples[ch][sampleNumber] = rawSample;
                        }
                    }

                    break;
                case WaveFormatEncoding.IeeeFloat:
                    for(int sampleNumber = 0; sampleNumber < numOutputSamples; sampleNumber++) {
                        for (int ch = 0; ch < Channels; ch++) {
                            int sampleIndex = (int)(sampleNumber * sampleConversion) * format.Channels * bytesPerSample;

                            if (ch < format.Channels) sampleIndex += ch * bytesPerSample;

                            var rawSample = BitConverter.ToSingle(data, sampleIndex);

                            outSamples[ch][sampleNumber] = rawSample;
                        }
                    }

                    break;
                default:
                    throw new InvalidOperationException($"This Wave encoding is not supported by VorbisFileWriter: {format.Encoding}");
            }

            WriteFloatSamples(outSamples, numOutputSamples);
        }

        private static float ByteToSample(short pcmValue)
        {
            return pcmValue / 128f;
        }

        private static float ShortToSample(short pcmValue)
        {
            return pcmValue / 32768f;
        }

        #region IDisposable Members

        /// <summary>
        ///     Actually performs the close,making sure the header contains the correct data
        /// </summary>
        /// <param name="disposing">True if called from <see>Dispose</see></param>
        private void Dispose(bool disposing) {
            ReleaseUnmanagedResources();
            if (disposing) {
                outStream?.Dispose();
                outStream = null;
            }
        }

        /// <summary>
        ///     Finaliser - should only be called if the user forgot to close this WaveFileWriter
        /// </summary>
        ~VorbisFileWriter() {
            Dispose(false);
        }

        private void ReleaseUnmanagedResources() {
            if (processingState != null && oggStream != null && outStream != null) {
                processingState.WriteEndOfStream();

                while (!oggStream.Finished && processingState.PacketOut(out var packet)) {
                    oggStream.PacketIn(packet);

                    FlushPages(oggStream, outStream, false);
                }
                FlushPages(oggStream, outStream, true);
            }
            oggStream = null;
            processingState = null;
        }

        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}