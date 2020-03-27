using System;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using OggVorbisEncoder;

namespace Mapping_Tools.Classes.HitsoundStuff {
    public class VorbisFileWriter : IDisposable {
        private Stream outStream;
        private OggStream oggStream;
        private ProcessingState processingState;

        /// <summary>
        ///     WaveFileWriter that actually writes to a stream
        /// </summary>
        /// <param name="outStream">Stream to be written to</param>
        /// <param name="format">Wave format to use</param>
        public VorbisFileWriter(Stream outStream, WaveFormat format) {
            this.outStream = outStream;
            WaveFormat = format;
            
            // Stores all the static vorbis bitstream settings
            var info = VorbisInfo.InitVariableBitRate(format.Channels, format.SampleRate, 0.5f);

            // set up our packet->stream encoder
            var serial = new Random().Next();
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
        }

        /// <summary>
        ///     Creates a new WaveFileWriter
        /// </summary>
        /// <param name="filename">The filename to write to</param>
        /// <param name="format">The Wave Format of the output data</param>
        public VorbisFileWriter(string filename, WaveFormat format)
            : this(new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.Read), format) {
            Filename = filename;
        }

        /// <summary>
        ///     The vorbis file name or null if not applicable
        /// </summary>
        public string Filename { get; }

        /// <summary>
        ///     WaveFormat of the input wave provider
        /// </summary>
        public WaveFormat WaveFormat { get; }

        private static bool CreateVorbisFile(string filename, IWaveProvider sourceProvider) {
            try {
                using (var writer = new VorbisFileWriter(filename, sourceProvider.WaveFormat)) {
                    var buffer = new byte[sourceProvider.WaveFormat.AverageBytesPerSecond * 4];
                    while (true) {
                        int bytesRead = sourceProvider.Read(buffer, 0, buffer.Length);
                        if (bytesRead == 0) {
                            // end of source provider
                            break;
                        }

                        writer.Write(buffer, 0, bytesRead);
                    }
                }

                return true;
            } catch (IndexOutOfRangeException) {
                return false;
            }
        }

        private static int FlushPages(OggStream oggStream, Stream Output, bool Force)
        {
            int bytesWritten = 0;
            while (oggStream.PageOut(out var page, Force))
            {
                Output.Write(page.Header, 0, page.Header.Length);
                Output.Write(page.Body, 0, page.Body.Length);

                bytesWritten += page.Header.Length + page.Body.Length;
            }

            return bytesWritten;
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

        public void WriteFloatSamples(float[][] floatSamples, int count) {
            processingState.WriteData(floatSamples, count);

            while (!oggStream.Finished && processingState.PacketOut(out var packet))
            {
                oggStream.PacketIn(packet);

                FlushPages(oggStream, outStream, false);
            }
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