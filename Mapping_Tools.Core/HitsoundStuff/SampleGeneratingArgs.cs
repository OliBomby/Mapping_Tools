using Mapping_Tools.Core.MathUtil;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.HitsoundStuff;

/// <summary>
///     Describes how one source file or SoundFont note must be transformed to generate an exported sample.
/// </summary>
public class SampleGeneratingArgs : IEquatable<SampleGeneratingArgs>
{
    /// <summary>
    ///     Creates an empty, unity-volume sample specification with all SoundFont selectors unset.
    /// </summary>
    public SampleGeneratingArgs()
    {
        Path = "";
        Volume = 1;
        Panning = 0;
        PitchShift = 0;
        Bank = -1;
        Patch = -1;
        Instrument = -1;
        Key = -1;
        Length = -1;
    }

    /// <summary>
    ///     Creates an unmodified sample-file specification.
    /// </summary>
    /// <param name="path">The source audio or SoundFont path.</param>
    public SampleGeneratingArgs(string path)
    {
        Path = path;
        Volume = 1;
        Panning = 0;
        PitchShift = 0;
        Bank = -1;
        Patch = -1;
        Instrument = -1;
        Key = -1;
        Length = -1;
    }

    /// <summary>
    ///     Creates a complete sample specification from persisted settings.
    /// </summary>
    /// <param name="path">The source audio or SoundFont path.</param>
    /// <param name="volume">Linear gain, where one is unchanged.</param>
    /// <param name="panning">Stereo pan applied during rendering.</param>
    /// <param name="pitchShift">Pitch adjustment applied during rendering.</param>
    /// <param name="bank">The SoundFont bank, or -1 when unused.</param>
    /// <param name="patch">The SoundFont patch, or -1 when unused.</param>
    /// <param name="instrument">The SoundFont instrument, or -1 when unused.</param>
    /// <param name="key">The MIDI note number, or -1 when unused.</param>
    /// <param name="length">The generated note length, or -1 when unused.</param>
    public SampleGeneratingArgs(string path, double volume, double panning, double pitchShift, int bank, int patch, int instrument, int key, double length)
    {
        Path = path;
        Volume = volume;
        Panning = panning;
        PitchShift = pitchShift;
        Bank = bank;
        Patch = patch;
        Instrument = instrument;
        Key = key;
        Length = length;
    }

    /// <summary>
    ///     Creates a SoundFont-note specification using MIDI-style velocity.
    /// </summary>
    /// <param name="path">The <c>.sf2</c> source path.</param>
    /// <param name="bank">The SoundFont bank.</param>
    /// <param name="patch">The SoundFont patch.</param>
    /// <param name="instrument">The SoundFont instrument.</param>
    /// <param name="key">The MIDI note number.</param>
    /// <param name="length">The generated note length.</param>
    /// <param name="velocity">The MIDI velocity from 0 through 127.</param>
    public SampleGeneratingArgs(string path, int bank, int patch, int instrument, int key, double length, int velocity)
    {
        Path = path;
        Panning = 0;
        PitchShift = 0;
        Bank = bank;
        Patch = patch;
        Instrument = instrument;
        Key = key;
        Length = length;
        Velocity = velocity;
    }

    /// <summary>
    ///     Checks if the specified path is a cafewalk soundfont file.
    /// </summary>
    public bool UsesSoundFont => GetExtension().ToLower() == ".sf2";

    /// <summary>
    ///     Means you can export this sample by simply copy pasting the source file in <see cref="Path" />.
    /// </summary>
    public bool CanCopyPaste => !string.IsNullOrEmpty(GetExtension())
                                && !UsesSoundFont
                                && Math.Abs(Volume - 1) < Precision.DOUBLE_EPSILON
                                && Math.Abs(Panning) < Precision.DOUBLE_EPSILON
                                && Math.Abs(PitchShift) < Precision.DOUBLE_EPSILON;

    /// <summary>
    ///     Gets or sets the source audio or SoundFont path.
    /// </summary>
    public string Path { get; set; } = "";

    /// <summary>
    ///     Gets or sets the linear gain used when rendering the sample.
    /// </summary>
    public double Volume { get; set; }

    /// <summary>
    ///     Gets or sets the stereo pan applied during generation.
    /// </summary>
    public double Panning { get; set; }

    /// <summary>
    ///     Gets or sets the pitch adjustment applied during generation.
    /// </summary>
    public double PitchShift { get; set; }

    /// <summary>
    ///     Gets or sets the SoundFont bank; -1 denotes an unused selector.
    /// </summary>
    public int Bank { get; set; }

    /// <summary>
    ///     Gets or sets the SoundFont patch; -1 denotes an unused selector.
    /// </summary>
    public int Patch { get; set; }

    /// <summary>
    ///     Gets or sets the SoundFont instrument; -1 denotes an unused selector.
    /// </summary>
    public int Instrument { get; set; }

    /// <summary>
    ///     Gets or sets the MIDI note number; -1 denotes an unused selector.
    /// </summary>
    public int Key { get; set; }

    /// <summary>
    ///     Gets or sets the generated SoundFont note length; -1 denotes an unspecified length.
    /// </summary>
    public double Length { get; set; }

    /// <summary>
    ///     Converts between <see cref="Volume" /> and MIDI velocity on a 0-to-127 scale.
    /// </summary>
    [JsonIgnore]
    public int Velocity
    {
        get => (int)Math.Round(Volume * 127);
        set => Volume = value / 127d;
    }

    /// <summary>
    ///     Compares paths and all transformation/SoundFont fields, using numeric tolerance for doubles.
    /// </summary>
    /// <param name="other">The specification to compare.</param>
    /// <returns><see langword="true" /> when both specifications generate the same configured sample.</returns>
    public bool Equals(SampleGeneratingArgs other)
    {
        if (other is null) return false;

        return Path == other.Path
               && Math.Abs(Volume - other.Volume) < Precision.DOUBLE_EPSILON
               && Math.Abs(Panning - other.Panning) < Precision.DOUBLE_EPSILON
               && Math.Abs(PitchShift - other.PitchShift) < Precision.DOUBLE_EPSILON
               && Bank == other.Bank
               && Patch == other.Patch
               && Instrument == other.Instrument
               && Key == other.Key
               && Math.Abs(Length - other.Length) < Precision.DOUBLE_EPSILON;
    }

    /// <summary>Returns a string that represents the current object and can be used as a filename.</summary>
    public string GetFilename()
    {
        string filename = System.IO.Path.GetFileNameWithoutExtension(Path);
        return GetExtension().ToLower() == ".sf2"
            ?
            Math.Abs(Panning) < Precision.DOUBLE_EPSILON && Math.Abs(PitchShift) < Precision.DOUBLE_EPSILON
                ? $"{filename}-{Bank}-{Patch}-{Instrument}-{Key}-{(int)Length}-{Velocity}"
                : $"{filename}-{(int)(Panning * 100)}-{(int)(PitchShift * 100)}-{Bank}-{Patch}-{Instrument}-{Key}-{(int)Length}-{Velocity}"
            : Math.Abs(Volume - 1) < Precision.DOUBLE_EPSILON && Math.Abs(Panning) < Precision.DOUBLE_EPSILON && Math.Abs(PitchShift) < Precision.DOUBLE_EPSILON
                ? filename
                :
                $"{filename}-{(int)(Volume * 100)}-{(int)(Panning * 100)}-{(int)(PitchShift * 100)}";
    }

    /// <summary>
    ///     Gets the extension of the file in <see cref="Path" />
    /// </summary>
    /// <returns>The extension including its leading period, or an empty string.</returns>
    public string GetExtension()
    {
        return System.IO.Path.GetExtension(Path);
    }

    /// <summary>Returns a string that represents the current object.</summary>
    /// <returns>A string that represents the current object.</returns>
    public override string ToString()
    {
        return GetExtension().ToLower() == ".sf2"
            ? $"{Path} p{Panning:N1} s{PitchShift:N3} {Bank},{Patch},{Instrument},{Key},{Length},{Velocity}"
            : $"{Path} {Volume * 100}% p{Panning:N1} s{PitchShift:N2}";
    }

    /// <summary>
    ///     Copies all rendering parameters into a new independent instance.
    /// </summary>
    /// <returns>An independently mutable sample specification.</returns>
    public SampleGeneratingArgs Copy()
    {
        return new SampleGeneratingArgs(Path, Volume, Panning, PitchShift, Bank, Patch, Instrument, Key, Length);
    }

    /// <summary>
    ///     Determines whether an object is an equivalent sample specification.
    /// </summary>
    /// <param name="obj">The object to compare.</param>
    /// <returns><see langword="true" /> when it is an equal <see cref="SampleGeneratingArgs" />.</returns>
    public override bool Equals(object obj)
    {
        if (!(obj is SampleGeneratingArgs)) return false;

        return Equals((SampleGeneratingArgs)obj);
    }

    /// <summary>Serves as the default hash function. </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        int hashCode = 0x34894079;
        hashCode = hashCode * -0x5AAAAAD7 + EqualityComparer<string>.Default.GetHashCode(Path);
        hashCode = hashCode * -0x5AAAAAD7 + Volume.GetHashCode();
        hashCode = hashCode * -0x5AAAAAD7 + Panning.GetHashCode();
        hashCode = hashCode * -0x5AAAAAD7 + PitchShift.GetHashCode();
        hashCode = hashCode * -0x5AAAAAD7 + Bank.GetHashCode();
        hashCode = hashCode * -0x5AAAAAD7 + Patch.GetHashCode();
        hashCode = hashCode * -0x5AAAAAD7 + Instrument.GetHashCode();
        hashCode = hashCode * -0x5AAAAAD7 + Key.GetHashCode();
        hashCode = hashCode * -0x5AAAAAD7 + Length.GetHashCode();
        return hashCode;
    }

    /// <summary>
    ///     Applies the == operator.
    /// </summary>
    /// <param name="left">The sample specification.</param>
    /// <param name="right">The value to compare.</param>
    /// <returns><see langword="true" /> when the left instance is non-null and equal to the right value.</returns>
    public static bool operator ==(SampleGeneratingArgs left, object right) => !(left is null) && left.Equals(right);

    /// <summary>
    ///     Applies the != operator.
    /// </summary>
    /// <param name="left">The sample specification.</param>
    /// <param name="right">The value to compare.</param>
    /// <returns><see langword="true" /> when the values are not equal.</returns>
    public static bool operator !=(SampleGeneratingArgs left, object right) => left is null || !left.Equals(right);
}
