using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Application.Updates;

/// <summary>
///     Describes the release metadata returned by the update source.
/// </summary>
/// <param name="CurrentVersion">The version of the running application.</param>
/// <param name="LatestVersion">The newest version offered by the selected update channel.</param>
/// <param name="ReleaseTitle">The GitHub release title, when the release payload contains one.</param>
/// <param name="ReleaseBody">The GitHub release description, when the release payload contains one.</param>
/// <param name="AssetName">The architecture-specific package asset selected for this process.</param>
public sealed record UpdatePackageInfo(
    Version CurrentVersion,
    Version? LatestVersion,
    string? ReleaseTitle,
    string? ReleaseBody,
    string AssetName)
{
    /// <summary>
    ///     Gets whether the source returned a package newer than the running version.
    /// </summary>
    public bool CanUpdate => LatestVersion is not null && LatestVersion > CurrentVersion;
}

