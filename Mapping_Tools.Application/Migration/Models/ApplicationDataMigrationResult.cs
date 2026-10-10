namespace Mapping_Tools.Application.Migration.Models;

/// <summary>
///     Summarizes a copy-only migration from the legacy application-data layout.
/// </summary>
/// <param name="AutosavesCopied">The number of legacy autosave files copied.</param>
/// <param name="ProjectFilesCopied">The number of files copied from legacy project folders.</param>
/// <param name="ExistingFilesSkipped">The number of destination files left unchanged.</param>
public sealed record ApplicationDataMigrationResult(
    int AutosavesCopied,
    int ProjectFilesCopied,
    int ExistingFilesSkipped);
