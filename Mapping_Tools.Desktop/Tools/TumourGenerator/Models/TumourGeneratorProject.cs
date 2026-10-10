using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Core.MathUtil;

namespace Mapping_Tools.Desktop.Tools.TumourGenerator.Models;

/// <summary>Stores the persisted Tumour Generator project.</summary>
public sealed class TumourGeneratorProject : TumourGeneratorServiceOptions
{
    /// <summary>Gets or sets the slider shown in the Tumour Generator preview.</summary>
    public HitObject PreviewHitObject { get; set; } = CreatePreviewHitObject();

    /// <summary>Gets or sets whether advanced layer controls are visible.</summary>
    public bool AdvancedOptions { get; set; }

    internal static HitObject CreatePreviewHitObject()
    {
        return new HitObject(
            Vector2.Zero,
            0,
            HitObjectType.Slider,
            false,
            0,
            false,
            false,
            false,
            false,
            SampleSet.None,
            SampleSet.None,
            0,
            0,
            string.Empty)
        {
            PixelLength = 256,
            Repeat = 1,
            ControlPoints =
            [
                new PathControlPoint(Vector2.Zero, PathType.Linear),
                new PathControlPoint(new Vector2(256, 0)),
            ],
        };
    }
}
