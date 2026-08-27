namespace Mapping_Tools.Core.Tools.TumourGenerator.Models;

/// <summary>Controls whether a tumour follows the original or wrapped slider path.</summary>
public enum WrappingMode
{
    /// <summary>Uses the straight direction between the tumour endpoints.</summary>
    Simple,

    /// <summary>Follows the local direction of the existing slider path.</summary>
    Wrap,

    /// <summary>Uses the path point's original angle without an additional offset.</summary>
    Absolute,
}

