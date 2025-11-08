using Mapping_Tools.Domain.Beatmaps.Enums;

namespace Mapping_Tools.Domain.Beatmaps.Sections;

/// <summary>
/// Contains all the values in the [General] section of a .osu file.
/// </summary>
public class SectionGeneral {
    public string AudioFilename { get; set; } = string.Empty;
    public int AudioLeadIn { get; set; } = 0;

    /// <summary>
    /// Audio expected MD5. (legacy)
    /// </summary>
    public string? AudioHash { get; set; }

    public int PreviewTime { get; set; } = -1;
    public Countdown Countdown { get; set; }

    /// <summary>
    /// Default sample set.
    /// </summary>
    public SampleSet SampleSet { get; set; } = SampleSet.Normal;

    public float StackLeniency { get; set; } = 0.7f;

    /// <summary>
    /// Play mode.
    /// </summary>
    public GameMode Mode { get; set; } = GameMode.Standard;

    public bool LetterboxInBreaks { get; set; } = false;
    public bool StoryFireInFront { get; set; } = true;
    public bool UseSkinSprites { get; set; }
    public bool AlwaysShowPlayfield { get; set; }
    public OverlayPosition OverlayPosition { get; set; } = OverlayPosition.NoChange;
    public string SkinPreference { get; set; } = string.Empty;
    public bool EpilepsyWarning { get; set; }

    /// <summary>
    /// Simulates a note happening this many beats earlier for countdown purposes.
    /// </summary>
    public int CountdownOffset { get; set; } = 0;

    /// <summary>
    /// Used for 7+1 & 5+1 in mania.
    /// </summary>
    public bool SpecialStyle { get; set; }

    public bool WidescreenStoryboard { get; set; }
    public bool SamplesMatchPlaybackRate { get; set; }

    /// <summary>
    /// Fallback value for timingpoints which dont have a volume value.
    /// </summary>
    public int SampleVolume { get; set; } = 100;
}