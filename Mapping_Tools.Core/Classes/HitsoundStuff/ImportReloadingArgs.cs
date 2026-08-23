// ReSharper disable CompareOfFloatsByEqualityOperator

namespace Mapping_Tools.Core.Classes.HitsoundStuff;

/// <summary>
///     Captures the subset of import settings that determines whether cached source data must be reloaded.
/// </summary>
public class ImportReloadingArgs : IEquatable<ImportReloadingArgs>
{
    /// <inheritdoc />
    public ImportReloadingArgs(string path) : this(ImportType.None, path, -1, -1, -1, -1, false, false, false, 0)
    {
    }

    /// <inheritdoc />
    public ImportReloadingArgs(ImportType importType, string path, double x, double y, double lengthRoughness, double velocityRoughness,
        bool discriminateVolumes, bool detectDuplicateSamples, bool removeDuplicates, double offset)
    {
        ImportType = importType;
        Path = path;
        X = x;
        Y = y;
        LengthRoughness = lengthRoughness;
        VelocityRoughness = velocityRoughness;
        DiscriminateVolumes = discriminateVolumes;
        DetectDuplicateSamples = detectDuplicateSamples;
        RemoveDuplicates = removeDuplicates;
        Offset = offset;
    }

    /// <summary>
    ///     Gets the source interpretation mode.
    /// </summary>
    public ImportType ImportType { get; }

    /// <summary>
    ///     Gets the imported beatmap, MIDI, stack, or hitsound source path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///     Gets the stack X filter, or -1 for any coordinate.
    /// </summary>
    public double X { get; }

    /// <summary>
    ///     Gets the stack Y filter, or -1 for any coordinate.
    /// </summary>
    public double Y { get; }

    /// <summary>
    ///     Gets the MIDI note-length grouping tolerance.
    /// </summary>
    public double LengthRoughness { get; }

    /// <summary>
    ///     Gets the MIDI velocity grouping tolerance.
    /// </summary>
    public double VelocityRoughness { get; }

    /// <summary>
    ///     Indicates whether hitsound imports split otherwise identical samples by volume.
    /// </summary>
    public bool DiscriminateVolumes { get; }

    /// <summary>
    ///     Indicates whether audio content is inspected for duplicate samples.
    /// </summary>
    public bool DetectDuplicateSamples { get; }

    /// <summary>
    ///     Indicates whether detected duplicate imports are discarded.
    /// </summary>
    public bool RemoveDuplicates { get; }

    /// <summary>
    ///     Gets the millisecond offset applied to imported events.
    /// </summary>
    public double Offset { get; }

    /// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
    /// <param name="other">An object to compare with this object.</param>
    /// <returns>true if the current object is equal to the <paramref name="other" /> parameter; otherwise, false.</returns>
    public bool Equals(ImportReloadingArgs other)
    {
        return Path == other.Path
               && ImportType == other.ImportType
               && X == other.X
               && Y == other.Y
               && LengthRoughness == other.LengthRoughness
               && VelocityRoughness == other.VelocityRoughness
               && DiscriminateVolumes == other.DiscriminateVolumes
               && DetectDuplicateSamples == other.DetectDuplicateSamples
               && RemoveDuplicates == other.RemoveDuplicates
               && Offset == other.Offset;
    }

    /// <summary>Determines whether the specified object is equal to the current object.</summary>
    /// <param name="obj">The object to compare with the current object. </param>
    /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
    public override bool Equals(object obj)
    {
        if (!(obj is ImportReloadingArgs)) return false;

        return Equals((ImportReloadingArgs)obj);
    }

    /// <summary>Serves as the default hash function. </summary>
    /// <returns>A hash code for the current object.</returns>
    public override int GetHashCode()
    {
        int hashCode = 1887348610;
        hashCode = hashCode * -1521134295 + ImportType.GetHashCode();
        hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Path);
        hashCode = hashCode * -1521134295 + X.GetHashCode();
        hashCode = hashCode * -1521134295 + Y.GetHashCode();
        hashCode = hashCode * -1521134295 + LengthRoughness.GetHashCode();
        hashCode = hashCode * -1521134295 + VelocityRoughness.GetHashCode();
        hashCode = hashCode * -1521134295 + RemoveDuplicates.GetHashCode();
        hashCode = hashCode * -1521134295 + DiscriminateVolumes.GetHashCode();
        hashCode = hashCode * -1521134295 + DetectDuplicateSamples.GetHashCode();
        hashCode = hashCode * -1521134295 + Offset.GetHashCode();
        return hashCode;
    }
}
