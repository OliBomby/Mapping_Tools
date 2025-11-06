using Mapping_Tools.Classes.Tools.TumourGenerating.Enums;
using Mapping_Tools.Classes.Tools.TumourGenerating.Options;

namespace Mapping_Tools.Classes.Tools.TumourGenerating;

public interface ITumourAssignment {
    /// <summary>
    /// The pixel length cumulative distance of the start of the tumour.
    /// </summary>
    double Start { get; }
        
    /// <summary>
    /// The pixel length cumulative distance of the end of the tumour.
    /// </summary>
    double End { get; }

    /// <summary>
    /// The longitudinal length of the tumour.
    /// </summary>
    public double Length { get; }

    /// <summary>
    /// The size scalar of the tumour.
    /// </summary>
    public double Scalar { get; }

    /// <summary>
    /// Rotational offset of the tumour in radians.
    /// </summary>
    public double Rotation { get; }

    /// <summary>
    /// The wrapping mode controls how the tumour sits on the slider.
    /// </summary>
    public WrappingMode WrappingMode { get; }

    /// <summary>
    /// Whether to invert the sidedness of the tumour.
    /// If false, up (-Y) will be on the left-hand side of the slider.
    /// If true, up (-Y) will be on the right-hand side of the slider.
    /// </summary>
    public bool Inverted { get; }

    /// <summary>
    /// The tumour shape.
    /// </summary>
    ITumourTemplate GetTemplate();
}