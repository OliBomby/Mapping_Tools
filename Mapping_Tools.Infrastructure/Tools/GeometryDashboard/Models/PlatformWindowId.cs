namespace Mapping_Tools.Infrastructure.Tools.GeometryDashboard.Models;

/// <summary>Identifies a desktop window inside Infrastructure.</summary>
/// <param name="Value">The native window identifier.</param>
public readonly record struct PlatformWindowId(long Value)
{
    /// <summary>Gets whether the identifier does not refer to a usable native window.</summary>
    public bool IsEmpty => Value == 0;
}