using NAudio.Wave;
using System.IO;

namespace Mapping_Tools.Core.Audio;

/// <summary>
/// Class for reading from MP3 files
/// </summary>
public class Mp3FileReader : Mp3FileReaderBase {
    /// <summary>Supports opening a MP3 file</summary>
    public Mp3FileReader(string mp3FileName)
        : base(File.OpenRead(mp3FileName), CreateAcmFrameDecompressor, true) {
    }

    /// <summary>
    /// Opens MP3 from a stream rather than a file
    /// Will not dispose of this stream itself
    /// </summary>
    /// <param name="inputStream">The incoming stream containing MP3 data</param>
    public Mp3FileReader(Stream inputStream)
        : base(inputStream, CreateAcmFrameDecompressor, false) {

    }

    /// <summary>
    /// Creates an ACM MP3 Frame decompressor. This is the default with NAudio
    /// </summary>
    /// <param name="mp3Format">A WaveFormat object based </param>
    /// <returns></returns>
    public static IMp3FrameDecompressor CreateAcmFrameDecompressor(WaveFormat mp3Format) {
        return new NLayer.NAudioSupport.Mp3FrameDecompressor(mp3Format);
        //return new DmoMp3FrameDecompressor(mp3Format);
        //return new AcmMp3FrameDecompressor(mp3Format);
    }
}