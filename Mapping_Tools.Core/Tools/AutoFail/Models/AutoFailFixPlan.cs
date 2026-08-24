namespace Mapping_Tools.Core.Tools.AutoFail.Models;

/// <summary>Describes one candidate distribution of padding objects and its human-readable guide.</summary>
/// <param name="Padding">The number of objects inserted around each problem area.</param>
/// <param name="Guide">The mapper-facing instructions for reproducing the repair.</param>
public sealed record AutoFailFixPlan(IReadOnlyList<int> Padding, string Guide);

