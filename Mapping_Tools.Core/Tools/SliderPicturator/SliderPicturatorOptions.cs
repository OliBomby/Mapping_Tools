using Mapping_Tools.Core.Classes.BeatmapHelper;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.SliderPicturator;

/// <summary>Stores the Slider Picturator settings using the legacy property names.</summary>
public class SliderPicturatorOptions
{
    /// <summary>Gets or sets the GPU viewport-size choice.</summary>
    public long ViewportSize { get; set; } = 32768;

    /// <summary>Gets or sets the preview quality from one through 101.</summary>
    public int Quality { get; set; } = 1;

    /// <summary>Gets or sets the approximate preview segment count.</summary>
    public long SegmentCount { get; set; }

    /// <summary>Gets or sets the image's vertical resolution.</summary>
    public double YResolution { get; set; } = 1080;

    /// <summary>Gets or sets the generated slider start X coordinate.</summary>
    public double SliderStartX { get; set; } = 256;

    /// <summary>Gets or sets the generated slider start Y coordinate.</summary>
    public double SliderStartY { get; set; } = 192;

    /// <summary>Gets or sets the image's top-left X position.</summary>
    public double ImageStartX { get; set; }

    /// <summary>Gets or sets the image's top-left Y position.</summary>
    public double ImageStartY { get; set; }

    /// <summary>Gets or sets whether the map palette supplies the track colour.</summary>
    public bool UseMapComboColors { get; set; }

    /// <summary>Gets or sets the selected map palette colour.</summary>
    public RgbaColour ComboColor { get; set; } = RgbaColour.FromRgb(0, 0, 0);

    /// <summary>Gets or sets the effective track colour used by the legacy project.</summary>
    public RgbaColour CurrentTrackColor { get; set; } = RgbaColour.White;

    /// <summary>Gets or sets the manually selected track colour.</summary>
    public RgbaColour TrackColorPickerColor { get; set; } = RgbaColour.White;

    /// <summary>Gets or sets the slider border colour.</summary>
    public RgbaColour BorderColor { get; set; } = RgbaColour.White;

    /// <summary>Gets or sets the generated slider start time in milliseconds.</summary>
    public double TimeCode { get; set; }

    /// <summary>Gets or sets the generated duration in milliseconds.</summary>
    public double Duration { get; set; } = 1;

    /// <summary>Gets or sets the image file path.</summary>
    public string PictureFile { get; set; } = string.Empty;

    /// <summary>Gets or sets whether black may be represented by transparent slider space.</summary>
    public bool BlackOn { get; set; } = true;

    /// <summary>Gets or sets whether the border colour may be used.</summary>
    public bool BorderOn { get; set; } = true;

    /// <summary>Gets or sets whether the source red channel participates in matching.</summary>
    public bool RedOn { get; set; } = true;

    /// <summary>Gets or sets whether the source green channel participates in matching.</summary>
    public bool GreenOn { get; set; } = true;

    /// <summary>Gets or sets whether the source blue channel participates in matching.</summary>
    public bool BlueOn { get; set; } = true;

    /// <summary>Gets or sets whether the source alpha channel participates in matching.</summary>
    public bool AlphaOn { get; set; } = true;

    /// <summary>Gets or sets whether generated colours are written to the beatmap.</summary>
    public bool SetBeatmapColors { get; set; } = true;

    /// <summary>Gets or sets the transient selected slider used for sliderball motion.</summary>
    [JsonIgnore]
    public HitObject? SelectedSlider { get; set; }

    /// <summary>Validates settings that were previously enforced by WPF binding rules.</summary>
    public void Validate()
    {
        if (ViewportSize <= 0) throw new ArgumentException("GPU viewport size must be positive.");
        if (Quality is < 1 or > 101) throw new ArgumentException("Image quality must be from 1 through 101.");
        if (!double.IsFinite(YResolution) || YResolution <= 0) throw new ArgumentException("Y resolution must be positive.");
        if (!double.IsFinite(SliderStartX) || SliderStartX < 0 || !double.IsFinite(SliderStartY) || SliderStartY < 0)
            throw new ArgumentException("Slider position must be non-negative.");
        if (!double.IsFinite(ImageStartX) || ImageStartX < 0 || !double.IsFinite(ImageStartY) || ImageStartY < 0)
            throw new ArgumentException("Image position must be non-negative.");
        ArgumentException.ThrowIfNullOrWhiteSpace(PictureFile);
    }
}
