using Mapping_Tools.Application.Migration.Contracts;
using Mapping_Tools.Application.Migration.Models;
using Mapping_Tools.Application.Platform;

namespace Mapping_Tools.Infrastructure.Migration;

/// <summary>
///     Copies legacy configuration, root-level autosaves, and project folders
///     from the previous client location to the current layout.
/// </summary>
public sealed class ApplicationDataMigrationService : IApplicationDataMigrationService
{
    private const string autosaves_directory_name = "Autosaves";
    private const string projects_directory_name = "Projects";
    private const string preferences_file_name = "preferences.json";
    private readonly IApplicationDirectories directories;

    /// <summary>
    ///     Creates a migration service for the supplied legacy and current data locations.
    /// </summary>
    /// <param name="directories">Provides the legacy source and current destination paths.</param>
    public ApplicationDataMigrationService(IApplicationDirectories directories)
    {
        this.directories = directories ?? throw new ArgumentNullException(nameof(directories));
    }

    /// <inheritdoc />
    public bool RequiresMigration => File.Exists(GetCurrentPreferencesFile())
                                    || (!File.Exists(directories.ConfigurationFile)
                                        && GetLegacySettingsSourceFile() is not null);

    /// <inheritdoc />
    public ApplicationDataMigrationResult? LastMigrationResult { get; private set; }

    /// <inheritdoc />
    public Task<ApplicationDataMigrationResult> CopyLegacyDataAsync(
        CancellationToken cancellationToken = default)
    {
        if (!RequiresMigration)
            return Task.FromResult(new ApplicationDataMigrationResult(0, 0, 0));

        return CopyAndRememberResultAsync(cancellationToken);
    }

    private async Task<ApplicationDataMigrationResult> CopyAndRememberResultAsync(
        CancellationToken cancellationToken)
    {
        ApplicationDataMigrationResult result = await Task.Run(
            () => CopyLegacyData(cancellationToken),
            cancellationToken).ConfigureAwait(false);
        LastMigrationResult = result;
        return result;
    }

    private ApplicationDataMigrationResult CopyLegacyData(CancellationToken cancellationToken)
    {
        int autosavesCopied = 0;
        int projectFilesCopied = 0;
        int existingFilesSkipped = 0;
        string legacyApplicationData = directories.LegacyApplicationData;
        string applicationData = directories.ApplicationData;

        string autosavesDirectory = Path.Combine(applicationData, autosaves_directory_name);
        string projectsDirectory = Path.Combine(applicationData, projects_directory_name);

        if (Directory.Exists(legacyApplicationData))
        {
            foreach (string file in Directory.EnumerateFiles(
                         legacyApplicationData,
                         "*",
                         SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!Path.GetFileName(file).EndsWith("project.json", StringComparison.OrdinalIgnoreCase)) continue;

                bool copied = CopyFileIfMissing(file, Path.Combine(autosavesDirectory, Path.GetFileName(file)));
                if (copied) autosavesCopied++;
                else existingFilesSkipped++;
            }

            foreach (string directory in Directory.EnumerateDirectories(
                         legacyApplicationData,
                         "*",
                         SearchOption.TopDirectoryOnly))
            {
                cancellationToken.ThrowIfCancellationRequested();
                string directoryName = Path.GetFileName(directory);
                if (!directoryName.EndsWith(" Projects", StringComparison.OrdinalIgnoreCase)) continue;

                CopyDirectory(
                    directory,
                    Path.Combine(projectsDirectory, directoryName),
                    cancellationToken,
                    ref projectFilesCopied,
                    ref existingFilesSkipped);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        string currentPreferencesFile = GetCurrentPreferencesFile();
        if (File.Exists(currentPreferencesFile))
        {
            File.Move(currentPreferencesFile, directories.ConfigurationFile, overwrite: true);
        }
        else if (!File.Exists(directories.ConfigurationFile)
                 && GetLegacySettingsSourceFile() is { } legacySettingsFile)
        {
            CopyFileIfMissing(legacySettingsFile, directories.ConfigurationFile);
        }

        return new ApplicationDataMigrationResult(
            autosavesCopied,
            projectFilesCopied,
            existingFilesSkipped);
    }

    private string GetCurrentPreferencesFile()
    {
        return Path.Combine(directories.ApplicationData, preferences_file_name);
    }

    private string? GetLegacySettingsSourceFile()
    {
        string[] candidates =
        [
            Path.Combine(directories.LegacyApplicationData, preferences_file_name),
            Path.Combine(directories.LegacyApplicationData, "config.json"),
        ];

        return candidates.FirstOrDefault(File.Exists);
    }

    private static void CopyDirectory(
        string sourceDirectory,
        string destinationDirectory,
        CancellationToken cancellationToken,
        ref int copied,
        ref int skipped)
    {
        Directory.CreateDirectory(destinationDirectory);

        foreach (string sourceFile in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            string destinationFile = Path.Combine(destinationDirectory, Path.GetFileName(sourceFile));
            if (CopyFileIfMissing(sourceFile, destinationFile)) copied++;
            else skipped++;
        }

        foreach (string sourceChildDirectory in Directory.EnumerateDirectories(
                     sourceDirectory,
                     "*",
                     SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            CopyDirectory(
                sourceChildDirectory,
                Path.Combine(destinationDirectory, Path.GetFileName(sourceChildDirectory)),
                cancellationToken,
                ref copied,
                ref skipped);
        }
    }

    private static bool CopyFileIfMissing(string sourceFile, string destinationFile)
    {
        if (File.Exists(destinationFile)) return false;

        string? destinationDirectory = Path.GetDirectoryName(destinationFile);
        if (destinationDirectory is not null) Directory.CreateDirectory(destinationDirectory);
        File.Copy(sourceFile, destinationFile);
        return true;
    }
}
