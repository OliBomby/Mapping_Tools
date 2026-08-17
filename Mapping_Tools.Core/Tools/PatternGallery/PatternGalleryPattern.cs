using System.ComponentModel;
using Mapping_Tools.Core.Classes.SystemTools;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.PatternGallery;

/// <summary>
/// Stores the indexed metadata for one pattern file. The beatmap content stays
/// in the collection's Pattern Files directory and is addressed by
/// <see cref="FileName"/>.
/// </summary>
public sealed class PatternGalleryPattern : BindableBase
{
    private bool _isSelected;
    private string _name = string.Empty;
    private string _group = string.Empty;
    private DateTime _creationTime;
    private DateTime _lastUsedTime;
    private int _useCount;
    private string _fileName = string.Empty;
    private int _objectCount;
    private TimeSpan _duration;
    private double _beatLength;

    /// <summary>Gets or sets whether the gallery action targets this pattern.</summary>
    [JsonIgnore]
    public bool IsSelected
    {
        get => _isSelected;
        set => Set(ref _isSelected, value);
    }

    /// <summary>Gets or sets the display name shown below the thumbnail.</summary>
    [DisplayName("Name")]
    public string Name
    {
        get => _name;
        set => Set(ref _name, value ?? string.Empty);
    }

    /// <summary>Gets or sets the optional group name used by the gallery.</summary>
    public string Group
    {
        get => _group;
        set => Set(ref _group, value ?? string.Empty);
    }

    /// <summary>Gets or sets the local time at which the pattern was indexed.</summary>
    public DateTime CreationTime
    {
        get => _creationTime;
        set => Set(ref _creationTime, value);
    }

    /// <summary>Gets or sets the local time at which the pattern was last placed.</summary>
    public DateTime LastUsedTime
    {
        get => _lastUsedTime;
        set => Set(ref _lastUsedTime, value);
    }

    /// <summary>Gets or sets the number of successful placements.</summary>
    public int UseCount
    {
        get => _useCount;
        set => Set(ref _useCount, value);
    }

    /// <summary>Gets or sets the filename of the pattern's `.osu` document.</summary>
    public string FileName
    {
        get => _fileName;
        set => Set(ref _fileName, value ?? string.Empty);
    }

    /// <summary>Gets or sets the number of hit objects indexed from the file.</summary>
    public int ObjectCount
    {
        get => _objectCount;
        set => Set(ref _objectCount, value);
    }

    /// <summary>Gets or sets the pattern duration from first object to last end.</summary>
    public TimeSpan Duration
    {
        get => _duration;
        set => Set(ref _duration, value);
    }

    /// <summary>Gets or sets the average beat length reported by the pattern timing.</summary>
    public double BeatLength
    {
        get => _beatLength;
        set => Set(ref _beatLength, value);
    }
}
