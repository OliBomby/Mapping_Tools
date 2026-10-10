using Mapping_Tools.Application.Migration.Models;

namespace Mapping_Tools.Application.Migration.Contracts;

/// <summary>
///     Detects and copies application data from the pre-versioned Mapping Tools layout.
/// </summary>
public interface IApplicationDataMigrationService
{
    /// <summary>
    ///     Gets whether a configuration document can be migrated into the current location.
    /// </summary>
    bool RequiresMigration { get; }

    /// <summary>
    ///     Gets the result of a migration already completed during this process, if any.
    /// </summary>
    ApplicationDataMigrationResult? LastMigrationResult { get; }

    /// <summary>
    ///     Copies legacy autosaves and project files into the current application-data layout.
    /// </summary>
    /// <param name="cancellationToken">Cancels the copy before the next file is processed.</param>
    /// <returns>A summary of copied and already-existing files.</returns>
    Task<ApplicationDataMigrationResult> CopyLegacyDataAsync(CancellationToken cancellationToken = default);
}
