using Mapping_Tools.Core.Tools.SliderPicturator.Models;

namespace Mapping_Tools.Application.Tools.SliderPicturator;

/// <summary>Represents the complete persisted Slider Picturator project.</summary>
public class SliderPicturatorServiceOptions : SliderPicturatorEngineOptions
{
    /// <summary>Gets or sets the image file path.</summary>
    public string PictureFile { get; set; } = string.Empty;

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
