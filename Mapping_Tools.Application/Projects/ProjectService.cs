using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;

namespace Mapping_Tools.Application.Projects;

/// <summary>
///     Implements typed project lifecycle orchestration while leaving
///     serialization, filesystem access, and native picker presentation behind
///     injected ports.
/// </summary>
public sealed class ProjectService : IProjectService
{
    private const string autoSaveDirectoryName = "Autosaves";
    private const string projectsDirectoryName = "Projects";
    private readonly IApplicationDirectories directories;
    private readonly IFilePicker filePicker;
    private readonly IProjectStore store;

    /// <summary>
    ///     Creates a project coordinator for the current application-data layout.
    /// </summary>
    /// <param name="directories">Provides the root used by autosaves and feature project folders.</param>
    /// <param name="filePicker">Presents native Open and Save As dialogs.</param>
    /// <param name="store">Performs typed, atomic project reads and writes.</param>
    public ProjectService(
        IApplicationDirectories directories,
        IFilePicker filePicker,
        IProjectStore store)
    {
        this.directories = directories ?? throw new ArgumentNullException(nameof(directories));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <inheritdoc />
    public string GetAutoSavePath<TProject>(ProjectDefinition<TProject> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Path.Combine(
            directories.ApplicationData,
            autoSaveDirectoryName,
            definition.AutoSaveFileName);
    }

    /// <inheritdoc />
    public async Task<TProject> LoadAutoSaveAsync<TProject>(
        ProjectDefinition<TProject> definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        try
        {
            return await store.LoadAsync<TProject>(
                definition.ConfigSchema,
                GetAutoSavePath(definition),
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (
            exception is FileNotFoundException or DirectoryNotFoundException)
        {
            return await store.LoadAsync<TProject>(
                definition.ConfigSchema,
                Path.Combine(directories.ApplicationData, definition.AutoSaveFileName),
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public string GetProjectFolder<TProject>(ProjectDefinition<TProject> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Path.Combine(
            directories.ApplicationData,
            projectsDirectoryName,
            definition.ProjectFolderName);
    }

    /// <inheritdoc />
    public TProject CreateNew<TProject>(ProjectDefinition<TProject> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition.CreateProject()
               ?? throw new InvalidOperationException(
                   $"The project factory for {typeof(TProject).FullName} returned null.");
    }

    /// <inheritdoc />
    public Task SaveAsync<TProject>(
        string path,
        TProject project,
        CancellationToken cancellationToken = default)
    {
        return store.SaveAsync(path, project, cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveAsync<TProject>(
        ToolConfigSchema schema,
        string path,
        TProject project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return store.SaveAsync(schema, path, project, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TProject> LoadAsync<TProject>(
        string path,
        CancellationToken cancellationToken = default)
    {
        return store.LoadAsync<TProject>(path, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TProject> LoadAsync<TProject>(
        ToolConfigSchema schema,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(schema);
        return store.LoadAsync<TProject>(schema, path, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AutoSaveAsync<TProject>(
        ProjectDefinition<TProject> definition,
        TProject project,
        IEnumerable<string>? additionalPaths = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (string path in ResolveAutoSavePaths(definition, additionalPaths))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await store.SaveAsync(
                definition.ConfigSchema,
                path,
                project,
                cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<string?> SaveAsAsync<TProject>(
        ProjectDefinition<TProject> definition,
        TProject project,
        string? suggestedFileName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();
        string projectFolder = GetProjectFolder(definition);
        store.EnsureDirectoryExists(projectFolder);

        string? path = await filePicker.PickSaveFileAsync(
            new SaveFilePickerRequest
            {
                Title = "Save project",
                SuggestedStartLocation = projectFolder,
                SuggestedFileName = suggestedFileName,
                DefaultExtension = "json",
                ShowOverwritePrompt = true,
                Filters = [CommonFilePickerFilters.MappingToolsProjects],
            },
            cancellationToken);

        if (path is null) return null;

        await store.SaveAsync(
            definition.ConfigSchema,
            path,
            project,
            cancellationToken);
        return path;
    }

    /// <inheritdoc />
    public async Task<ProjectOpenResult<TProject>?> OpenAsync<TProject>(
        ProjectDefinition<TProject> definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        cancellationToken.ThrowIfCancellationRequested();
        string projectFolder = GetProjectFolder(definition);
        store.EnsureDirectoryExists(projectFolder);

        var paths = await filePicker.PickOpenFilesAsync(
            new OpenFilePickerRequest
            {
                Title = "Open project",
                SuggestedStartLocation = projectFolder,
                AllowMultiple = false,
                Filters = [CommonFilePickerFilters.MappingToolsProjects],
            },
            cancellationToken);

        if (paths.Count == 0) return null;

        string path = paths[0];
        TProject project = await store.LoadAsync<TProject>(
            definition.ConfigSchema,
            path,
            cancellationToken);
        return new ProjectOpenResult<TProject>(path, project);
    }

    private IEnumerable<string> ResolveAutoSavePaths<TProject>(
        ProjectDefinition<TProject> definition,
        IEnumerable<string>? additionalPaths)
    {
        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        HashSet<string> writtenPaths = new(pathComparer);
        IEnumerable<string> candidatePaths = additionalPaths is null
            ? [GetAutoSavePath(definition)]
            : [GetAutoSavePath(definition), .. additionalPaths];

        foreach (string path in candidatePaths)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            string fullPath = Path.GetFullPath(path);
            if (writtenPaths.Add(fullPath)) yield return fullPath;
        }
    }
}
