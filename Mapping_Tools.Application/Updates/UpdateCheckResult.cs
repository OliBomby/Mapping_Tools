using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Application.Updates;

/// <summary>
///     Contains the update check outcome shown by the updater UI.
/// </summary>
/// <param name="Availability">Whether a package is available, skipped, or absent.</param>
/// <param name="CurrentVersion">The running application version.</param>
/// <param name="LatestVersion">The offered version, or <see langword="null" /> when no package was found.</param>
/// <param name="ReleaseTitle">The release title returned by GitHub.</param>
/// <param name="ReleaseBody">The release description returned by GitHub.</param>
/// <param name="AssetName">The architecture-specific package asset selected for this process.</param>
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

