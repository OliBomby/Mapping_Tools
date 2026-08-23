using Mapping_Tools.Application.Platform;

namespace Mapping_Tools.Application.Projects;

/// <summary>
///     Implements project lifecycle orchestration while leaving serialization,
///     filesystem access, and native picker presentation behind injected ports.
/// </summary>
public sealed class ProjectService : IProjectService
{
    private readonly IApplicationDirectories _directories;
    private readonly IFilePicker _filePicker;
    private readonly IProjectStore _store;

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
        _directories = directories ?? throw new ArgumentNullException(nameof(directories));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _store = store ?? throw new ArgumentNullException(nameof(store));
    }

    /// <inheritdoc />
    public string GetAutoSavePath(IProjectDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Path.Combine(_directories.ApplicationData, definition.AutoSaveFileName);
    }

    /// <inheritdoc />
    public string GetAutoSavePath<TProject>(ProjectDefinition<TProject> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Path.Combine(_directories.ApplicationData, definition.AutoSaveFileName);
    }

    /// <inheritdoc />
    public string GetProjectFolder<TProject>(ProjectDefinition<TProject> definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Path.Combine(_directories.ApplicationData, definition.ProjectFolderName);
    }

    /// <inheritdoc />
    public string GetProjectFolder(IProjectDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return Path.Combine(_directories.ApplicationData, definition.ProjectFolderName);
    }

    /// <inheritdoc />
    public object CreateNew(IProjectDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        return definition.CreateProject()
               ?? throw new InvalidOperationException("The project factory returned null.");
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
        return _store.SaveAsync(path, project, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TProject> LoadAsync<TProject>(
        string path,
        CancellationToken cancellationToken = default)
    {
        return _store.LoadAsync<TProject>(path, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<object> LoadAsync(
        IProjectDefinition definition,
        string path,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();
        return await definition.LoadAsync(_store, path, cancellationToken);
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

        var pathComparer = OperatingSystem.IsWindows()
            ? StringComparer.OrdinalIgnoreCase
            : StringComparer.Ordinal;
        HashSet<string> writtenPaths = new(pathComparer);
        IEnumerable<string> candidatePaths = additionalPaths is null
            ? [GetAutoSavePath(definition)]
            : [GetAutoSavePath(definition), .. additionalPaths];
        List<string> paths = [];

        foreach (string path in candidatePaths)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(path);
            string fullPath = Path.GetFullPath(path);
            if (writtenPaths.Add(fullPath)) paths.Add(fullPath);
        }

        foreach (string path in paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _store.SaveAsync(path, project, cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task AutoSaveAsync(
        IProjectDefinition definition,
        object project,
        IEnumerable<string>? additionalPaths = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();

        foreach (string path in ResolveAutoSavePaths(definition, additionalPaths))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await definition.SaveAsync(_store, path, project, cancellationToken);
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
        _store.EnsureDirectoryExists(projectFolder);

        string? path = await _filePicker.PickSaveFileAsync(
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

        await _store.SaveAsync(path, project, cancellationToken);
        return path;
    }

    /// <inheritdoc />
    public async Task<string?> SaveAsAsync(
        IProjectDefinition definition,
        object project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(project);
        cancellationToken.ThrowIfCancellationRequested();
        string projectFolder = GetProjectFolder(definition);
        _store.EnsureDirectoryExists(projectFolder);

        string? path = await _filePicker.PickSaveFileAsync(
            new SaveFilePickerRequest
            {
                Title = "Save project",
                SuggestedStartLocation = projectFolder,
                SuggestedFileName = definition.SuggestedFileName,
                DefaultExtension = "json",
                ShowOverwritePrompt = true,
                Filters = [CommonFilePickerFilters.MappingToolsProjects],
            },
            cancellationToken);

        if (path is null) return null;

        await definition.SaveAsync(_store, path, project, cancellationToken);
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
        _store.EnsureDirectoryExists(projectFolder);

        var paths = await _filePicker.PickOpenFilesAsync(
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
        var project = await _store.LoadAsync<TProject>(path, cancellationToken);
        return new ProjectOpenResult<TProject>(path, project);
    }

    /// <inheritdoc />
    public async Task<ProjectOpenResult?> OpenAsync(
        IProjectDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        cancellationToken.ThrowIfCancellationRequested();
        string projectFolder = GetProjectFolder(definition);
        _store.EnsureDirectoryExists(projectFolder);

        var paths = await _filePicker.PickOpenFilesAsync(
            new OpenFilePickerRequest
            {
                Title = "Open project",
                SuggestedStartLocation = projectFolder,
                AllowMultiple = false,
                Filters = [CommonFilePickerFilters.MappingToolsProjects],
            },
            cancellationToken);

        string? path = paths.FirstOrDefault();
        if (path is null) return null;

        object project = await definition.LoadAsync(_store, path, cancellationToken);
        return new ProjectOpenResult(path, project);
    }

    private IEnumerable<string> ResolveAutoSavePaths(
        IProjectDefinition definition,
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
