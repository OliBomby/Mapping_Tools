namespace Mapping_Tools.Application.Updates.Models;

/// <summary>
///     Describes the release metadata returned by the update source.
/// </summary>
/// <param name="CurrentVersion">The version of the running application.</param>
/// <param name="LatestVersion">The newest version offered by the selected update channel.</param>
/// <param name="ReleaseTitle">The update source title, when one is available.</param>
/// <param name="ReleaseBody">The update source description, when one is available.</param>
/// <param name="AssetName">The selected package asset or local package file name.</param>
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
