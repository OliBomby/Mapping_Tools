namespace Mapping_Tools.Application.Projects.Models;

/// <summary>
///     Binds a feature's project model to its autosave filename, user-facing project
///     folder, and clean-project factory.
/// </summary>
/// <typeparam name="TProject">The complete serializable state owned by one feature.</typeparam>
public sealed class ProjectDefinition<TProject>
{
    /// <summary>
    ///     Creates the persistence metadata for one project-bearing feature.
    /// </summary>
    /// <param name="autoSaveFileName">
    ///     A filename, including extension, used beneath the current Autosaves
    ///     directory and the legacy application-data directory.
    /// </param>
    /// <param name="projectFolderName">
    ///     A single directory name beneath application data used as the initial
    ///     location for Open and Save As.
    /// </param>
    /// <param name="createProject">
    ///     Creates a fully initialized project with the same defaults as the
    ///     feature's New command.
    /// </param>
    /// <param name="suggestedFileName">
    ///     An optional filename proposed by the Save As picker.
    /// </param>
    /// <param name="configSchema">
    ///     The tool-owned configuration schema used for project persistence. When
    ///     omitted, the standalone compatibility schema is used.
    /// </param>
    /// <exception cref="ArgumentException">
    ///     A filename, folder name, or suggested filename is blank, rooted, or
    ///     contains a directory separator.
    /// </exception>
    public ProjectDefinition(
        string autoSaveFileName,
        string projectFolderName,
        Func<TProject> createProject,
        string? suggestedFileName = null,
        ToolConfigSchema? configSchema = null)
    {
        AutoSaveFileName = ValidateSinglePathSegment(
            autoSaveFileName,
            nameof(autoSaveFileName));
        ProjectFolderName = ValidateSinglePathSegment(
            projectFolderName,
            nameof(projectFolderName));
        SuggestedFileName = suggestedFileName is null
            ? null
            : ValidateSinglePathSegment(suggestedFileName, nameof(suggestedFileName));
        CreateProject = createProject
                        ?? throw new ArgumentNullException(nameof(createProject));
        ConfigSchema = configSchema ?? ToolConfigSchema.Default;
    }

    /// <summary>
    ///     Gets the factory used only after the presentation layer has confirmed
    ///     that replacing the current unsaved state is acceptable.
    /// </summary>
    public Func<TProject> CreateProject { get; }

    /// <summary>
    ///     Gets the filename used for automatic session recovery.
    /// </summary>
    public string AutoSaveFileName { get; }

    /// <summary>
    ///     Gets the application-data subdirectory offered by project file pickers.
    /// </summary>
    public string ProjectFolderName { get; }

    /// <summary>
    ///     Gets the filename proposed by the Save As picker, or <see langword="null" />
    ///     when the platform picker should choose its own default.
    /// </summary>
    public string? SuggestedFileName { get; }

    /// <summary>
    ///     Gets the tool-owned schema used when this project is serialized.
    /// </summary>
    public ToolConfigSchema ConfigSchema { get; }

    private static string ValidateSinglePathSegment(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        if (Path.IsPathRooted(value)
            || value is "." or ".."
            || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || value.IndexOfAny(
                [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar])
            >= 0)
            throw new ArgumentException(
                "The value must be a single relative path segment.",
                parameterName);

        return value;
    }
}
