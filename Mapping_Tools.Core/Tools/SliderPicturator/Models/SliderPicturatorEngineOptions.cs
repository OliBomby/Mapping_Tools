using Mapping_Tools.Core.BeatmapHelper;
using Newtonsoft.Json;

namespace Mapping_Tools.Core.Tools.SliderPicturator.Models;

/// <summary>
///     Stores the framework-neutral colours, geometry, matching, and output
///     settings used by the Slider Picturator engine.
/// </summary>
public class SliderPicturatorEngineOptions
{
    /// <summary>Gets or sets the GPU viewport-size choice.</summary>
    public long ViewportSize { get; set; } = 32768;

    /// <summary>Gets or sets the image quality from one through 101.</summary>
    public int Quality { get; set; } = 1;

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

    /// <summary>Gets or sets the effective track colour used by the engine.</summary>
    public RgbaColour CurrentTrackColor { get; set; } = RgbaColour.White;

    /// <summary>Gets or sets the slider border colour.</summary>
    public RgbaColour BorderColor { get; set; } = RgbaColour.White;

    /// <summary>Gets or sets the generated slider start time in milliseconds.</summary>
    public double TimeCode { get; set; }

    /// <summary>Gets or sets the generated duration in milliseconds.</summary>
    public double Duration { get; set; } = 1;

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

    /// <summary>Gets or sets whether the generated track colour is written as a beatmap override.</summary>
    public bool SetTrackColorOverride { get; set; } = true;

    /// <summary>
    ///     Gets or sets the colour composited below transparent source pixels.
    ///     This runtime-only value is ignored when options are persisted.
    /// </summary>
    [JsonIgnore]
    public RgbaColour BackgroundColor { get; set; } = RgbaColour.FromRgb(0, 0, 0);

    /// <summary>
    ///     Gets or sets the optional selected slider whose sliderball motion is
    ///     included in generation and segment estimation. When this object is
    ///     persisted as part of a Picturator project, its osu! hit-object line is
    ///     stored so the selection can be restored without retaining an editor reference.
    /// </summary>
    public HitObject? SelectedSlider { get; set; }
}
