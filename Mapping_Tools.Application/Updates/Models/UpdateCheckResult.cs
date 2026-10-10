namespace Mapping_Tools.Application.Updates.Models;

/// <summary>
///     Contains the update check outcome shown by the updater UI.
/// </summary>
/// <param name="Availability">Whether a package is available, skipped, or absent.</param>
/// <param name="CurrentVersion">The running application version.</param>
/// <param name="LatestVersion">The offered version, or <see langword="null" /> when no package was found.</param>
/// <param name="ReleaseTitle">The update source title, when one is available.</param>
/// <param name="ReleaseBody">The update source description, when one is available.</param>
/// <param name="AssetName">The selected package asset or local package file name.</param>
public sealed record UpdateCheckResult(
    UpdateAvailability Availability,
    Version CurrentVersion,
    Version? LatestVersion,
    string? ReleaseTitle,
    string? ReleaseBody,
    string AssetName)
{
    /// <summary>Gets whether the caller should display the update decision UI.</summary>
    public bool CanUpdate => Availability == UpdateAvailability.Available;
}
