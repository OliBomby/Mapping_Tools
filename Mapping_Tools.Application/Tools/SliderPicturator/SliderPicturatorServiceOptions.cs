using Mapping_Tools.Core.Tools.SliderPicturator.Models;

namespace Mapping_Tools.Application.Tools.SliderPicturator;

/// <summary>Represents the complete persisted Slider Picturator project.</summary>
public class SliderPicturatorServiceOptions : SliderPicturatorEngineOptions
{
    /// <summary>Gets or sets the image file path.</summary>
    public string PictureFile { get; set; } = string.Empty;
}
