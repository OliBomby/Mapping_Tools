using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Mapping_Tools.Core.Tools.ComboColourStudio;

/// <summary>
/// Interface that indicates an object has colour points.
/// </summary>
public interface IHasColourPoints {
    /// <summary>
    /// The colour points.
    /// </summary>
    [NotNull]
    IReadOnlyList<IColourPoint> ColourPoints { get; }

    /// <summary>
    /// The maximum combo length for burst-type colour points.
    /// </summary>
    int MaxBurstLength { get; }
}