using Mapping_Tools.Application.Migration.Contracts;
using Mapping_Tools.Application.Migration.Models;
using Mapping_Tools.Application.Platform;

namespace Mapping_Tools.Infrastructure.Migration;

/// <summary>
///     Copies legacy root-level autosaves and project folders to the current layout.
/// </summary>
public sealed class ApplicationDataMigrationService : IApplicationDataMigrationService
{
    private const string autosaves_directory_name = "Autosaves";
    private const string projects_directory_name = "Projects";
    private readonly IApplicationDirectories directories;

    /// <summary>
    ///     Creates a migration service for the supplied application-data layout.
    /// </summary>
    /// <param name="directories">Provides the legacy and current application-data paths.</param>
    public ApplicationDataMigrationService(IApplicationDirectories directories)
    {
        this.directories = directories ?? throw new ArgumentNullException(nameof(directories));
    }

    /// <inheritdoc />
    public bool RequiresMigration => File.Exists(directories.ConfigurationFile)
                                     && !File.Exists(directories.PreferencesFile);

    /// <inheritdoc />
    public Task<ApplicationDataMigrationResult> CopyLegacyDataAsync(
        CancellationToken cancellationToken = default)
    {
        if (!RequiresMigration)
            return Task.FromResult(new ApplicationDataMigrationResult(0, 0, 0));

        return Task.Run(() => CopyLegacyData(cancellationToken), cancellationToken);
    }

    private ApplicationDataMigrationResult CopyLegacyData(CancellationToken cancellationToken)
    {
        int autosavesCopied = 0;
        int projectFilesCopied = 0;
        int existingFilesSkipped = 0;
        string applicationData = directories.ApplicationData;

        string autosavesDirectory = Path.Combine(applicationData, autosaves_directory_name);
        string projectsDirectory = Path.Combine(applicationData, projects_directory_name);

        foreach (string file in Directory.EnumerateFiles(applicationData, "*", SearchOption.TopDirectoryOnly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Path.GetFileName(file).EndsWith("project.json", StringComparison.OrdinalIgnoreCase)) continue;

            bool copied = CopyFileIfMissing(file, Path.Combine(autosavesDirectory, Path.GetFileName(file)));
            if (copied) autosavesCopied++;
            else existingFilesSkipped++;
        }

        foreach (string directory in Directory.EnumerateDirectories(applicationData, "*", SearchOption.TopDirectoryOnly))
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

        return new ApplicationDataMigrationResult(
            autosavesCopied,
            projectFilesCopied,
            existingFilesSkipped);
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
