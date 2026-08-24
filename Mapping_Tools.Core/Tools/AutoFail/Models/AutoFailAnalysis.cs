namespace Mapping_Tools.Core.Tools.AutoFail.Models;

/// <summary>Contains one immutable auto-fail analysis result.</summary>
/// <param name="HasAutoFail">Whether at least one object unloads incorrectly.</param>
/// <param name="UnloadingObjects">The timestamps of confirmed unloading objects.</param>
/// <param name="PotentialUnloadingObjects">The timestamps of objects that may unload.</param>
/// <param name="Disruptors">The timestamps of objects that disrupt loading.</param>
public sealed record AutoFailAnalysis(
    bool HasAutoFail,
    IReadOnlyList<double> UnloadingObjects,
    IReadOnlyList<double> PotentialUnloadingObjects,
    IReadOnlyList<double> Disruptors);

