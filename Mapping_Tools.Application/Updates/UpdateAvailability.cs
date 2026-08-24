using Mapping_Tools.Application.Settings;

namespace Mapping_Tools.Application.Updates;

/// <summary>
///     Identifies the user-visible result of an update check.
/// </summary>
public enum UpdateAvailability
{
    /// <summary>No newer package was found.</summary>
    None,

    /// <summary>A newer package was suppressed by the persisted skip setting.</summary>
    Skipped,

    /// <summary>A newer package is ready to be offered to the user.</summary>
    Available,
}

