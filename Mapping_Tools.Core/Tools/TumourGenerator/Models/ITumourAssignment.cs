using Mapping_Tools.Core.Tools.TumourGenerator.Templates;

namespace Mapping_Tools.Core.Tools.TumourGenerator.Models;

/// <summary>Describes the scalar values supplied to a generated tumour.</summary>
public interface ITumourAssignment
{
    /// <summary>Gets the tumour start distance in pixels.</summary>
    double Start { get; }

    /// <summary>Gets the tumour end distance in pixels.</summary>
    double End { get; }

    /// <summary>Gets the longitudinal tumour length.</summary>
    double Length { get; }

    /// <summary>Gets the signed tumour scale.</summary>
    double Scalar { get; }

    /// <summary>Gets the tumour rotation in radians.</summary>
    double Rotation { get; }

    /// <summary>Gets the path wrapping mode.</summary>
    WrappingMode WrappingMode { get; }

    /// <summary>Gets whether the tumour is placed on the opposite side.</summary>
    bool Inverted { get; }

    /// <summary>Gets the configured tumour shape.</summary>
    ITumourTemplate GetTemplate();
}

