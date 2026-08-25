using Mapping_Tools.Application.Tools.SliderPicturator;
using Mapping_Tools.Core.BeatmapHelper;

namespace Mapping_Tools.Desktop.Models;

/// <summary>Stores Slider Picturator preview state alongside the service inputs.</summary>
public sealed class SliderPicturatorProject : SliderPicturatorServiceOptions
{
    /// <summary>Gets or sets the approximate preview segment count.</summary>
    public long SegmentCount { get; set; }

    private bool useMapComboColors;

    /// <summary>Gets or sets whether the map palette supplies the track colour.</summary>
    public bool UseMapComboColors
    {
        get => useMapComboColors;
        set
        {
            useMapComboColors = value;
            SetTrackColorOverride = !value;
        }
    }

    /// <summary>Gets or sets the selected map palette colour.</summary>
    public RgbaColour ComboColor { get; set; } = RgbaColour.FromRgb(0, 0, 0);

    /// <summary>Gets or sets the manually selected track colour.</summary>
    public RgbaColour TrackColorPickerColor { get; set; } = RgbaColour.White;
}
