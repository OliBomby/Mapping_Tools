using Mapping_Tools.Classes.SystemTools;
using System;
using Newtonsoft.Json;

namespace Mapping_Tools.Classes.Tools.PatternGallery;

/// <summary>
/// Must store the objects, the greenlines, the timing, the global SV, the tickrate, the difficulty settings,
/// the hitsounds, absolute times and positions, combocolour index, combo numbers, stack leniency, gamemode.
/// Also store additional metadata such as the name, the date it was saved, use count, the map title, artist, diffname, and mapper.
/// </summary>
public class OsuPattern : BindableBase {
    #region Fields

    private bool isSelected;
    [JsonIgnore]
    public bool IsSelected {
        get => isSelected;
        set => Set(ref isSelected, value);
    }

    private string name;
    public string Name {
        get => name;
        set => Set(ref name, value);
    }

    private string group;
    public string Group {
        get => group;
        set => Set(ref group, value);
    }

    private DateTime creationTime;
    public DateTime CreationTime {
        get => creationTime;
        set => Set(ref creationTime, value);
    }

    private DateTime lastUsedTime;
    public DateTime LastUsedTime {
        get => lastUsedTime;
        set => Set(ref lastUsedTime, value);
    }

    private int useCount;
    public int UseCount {
        get => useCount;
        set => Set(ref useCount, value);
    }

    private string fileName;
    public string FileName {
        get => fileName;
        set => Set(ref fileName, value);
    }

    private int objectCount;
    public int ObjectCount {
        get => objectCount;
        set => Set(ref objectCount, value);
    }

    private TimeSpan duration;
    public TimeSpan Duration {
        get => duration;
        set => Set(ref duration, value);
    }

    private double beatLength;
    public double BeatLength {
        get => beatLength;
        set => Set(ref beatLength, value);
    }

    #endregion
}