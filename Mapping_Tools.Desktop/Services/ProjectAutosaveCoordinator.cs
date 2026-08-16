using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.Services;

/// <summary>
/// Coordinates project menus and automatic recovery for the active shell features.
/// </summary>
public sealed class ProjectAutosaveCoordinator
{
    private readonly IProjectService _projects;
    private readonly IDialogService _dialogs;
    private readonly IUserNotificationService _notifications;
    private readonly Dictionary<IShellProjectFeature, Task> _loadTasks = [];

    /// <summary>
    /// Creates the shared project lifecycle coordinator.
    /// </summary>
    /// <param name="projects">Loads, saves, and creates typed project data.</param>
    /// <param name="dialogs">Confirms destructive New project operations.</param>
    /// <param name="notifications">Publishes project lifecycle failures.</param>
    public ProjectAutosaveCoordinator(
        IProjectService projects,
        IDialogService dialogs,
        IUserNotificationService notifications)
    {
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    /// <summary>
    /// Starts restoring a feature's automatic recovery project once.
    /// </summary>
    /// <param name="feature">The feature whose state is being activated.</param>
    public void Activate(IShellProjectFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        if (_loadTasks.ContainsKey(feature))
        {
            return;
        }

        _loadTasks.Add(feature, LoadAutosaveAsync(feature));
    }

    /// <summary>
    /// Saves a feature's current state to its automatic recovery file after any
    /// pending restore has completed.
    /// </summary>
    /// <param name="feature">The feature whose state is being deactivated.</param>
    public void Deactivate(IShellProjectFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        _ = SaveAutosaveAfterLoadAsync(feature);
    }

    /// <summary>
    /// Saves the active feature through its project Save As workflow.
    /// </summary>
    /// <param name="feature">The feature whose state should be saved.</param>
    /// <param name="cancellationToken">Cancels picker result processing or persistence.</param>
    /// <returns>A task that completes after the save attempt.</returns>
    public Task SaveAsync(
        IShellProjectFeature feature,
        CancellationToken cancellationToken = default) =>
        RunAsync(
            () => SaveProjectAsync(feature, cancellationToken),
            "Save project");

    /// <summary>
    /// Opens and installs a project selected for the active feature.
    /// </summary>
    /// <param name="feature">The feature that owns the project state.</param>
    /// <param name="cancellationToken">Cancels picker result processing or persistence.</param>
    /// <returns>A task that completes after the open attempt.</returns>
    public Task OpenAsync(
        IShellProjectFeature feature,
        CancellationToken cancellationToken = default) =>
        RunAsync(
            () => OpenProjectAsync(feature, cancellationToken),
            "Open project");

    /// <summary>
    /// Confirms and installs a new default project for the active feature.
    /// </summary>
    /// <param name="feature">The feature that owns the project state.</param>
    /// <param name="cancellationToken">Cancels confirmation or project initialization.</param>
    /// <returns>A task that completes after the confirmation and initialization attempt.</returns>
    public Task NewAsync(
        IShellProjectFeature feature,
        CancellationToken cancellationToken = default) =>
        RunAsync(
            () => NewProjectAsync(feature, cancellationToken),
            "New project");

    private async Task LoadAutosaveAsync(IShellProjectFeature feature)
    {
        try
        {
            IProjectDefinition definition = feature.ProjectDefinition;
            object project = await _projects.LoadAsync(
                definition,
                _projects.GetAutoSavePath(definition),
                CancellationToken.None);
            feature.Install(project);
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception exception)
        {
            await PublishFailureAsync("Project could not be loaded", exception);
        }
    }

    private async Task SaveAutosaveAfterLoadAsync(IShellProjectFeature feature)
    {
        await AwaitLoadAsync(feature);
        try
        {
            await _projects.AutoSaveAsync(
                feature.ProjectDefinition,
                feature.Snapshot());
        }
        catch (Exception exception)
        {
            await PublishFailureAsync("Project could not be saved", exception);
        }
    }

    private async Task SaveProjectAsync(
        IShellProjectFeature feature,
        CancellationToken cancellationToken)
    {
        await AwaitLoadAsync(feature);
        await _projects.SaveAsAsync(
            feature.ProjectDefinition,
            feature.Snapshot(),
            cancellationToken);
    }

    private async Task OpenProjectAsync(
        IShellProjectFeature feature,
        CancellationToken cancellationToken)
    {
        await AwaitLoadAsync(feature);
        ProjectOpenResult? opened = await _projects.OpenAsync(
            feature.ProjectDefinition,
            cancellationToken);
        if (opened is not null)
        {
            feature.Install(opened.Project);
        }
    }

    private async Task NewProjectAsync(
        IShellProjectFeature feature,
        CancellationToken cancellationToken)
    {
        bool confirmed = await _dialogs.ShowMessageAsync(
            new MessageDialogRequest<bool>(
                "Confirm new project",
                "Are you sure you want to start a new project? All unsaved progress will be lost.",
                [
                    new DialogChoice<bool>("Yes", true, IsDefault: true),
                    new DialogChoice<bool>("No", false, IsCancel: true)
                ],
                dismissResult: false),
            cancellationToken);
        if (!confirmed)
        {
            return;
        }

        await AwaitLoadAsync(feature);
        feature.Install(_projects.CreateNew(feature.ProjectDefinition));
    }

    private async Task AwaitLoadAsync(IShellProjectFeature feature)
    {
        if (_loadTasks.TryGetValue(feature, out Task? loadTask))
        {
            await loadTask;
        }
    }

    private async Task RunAsync(Func<Task> operation, string title)
    {
        try
        {
            await operation();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            await PublishFailureAsync(title, exception);
        }
    }

    private Task PublishFailureAsync(string title, Exception exception) =>
        _notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            title,
            exception.Message,
            exception));
}
