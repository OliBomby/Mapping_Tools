using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Execution.UserNotification.Models;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Interactions.Dialogs;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.Services;

/// <summary>
///     Coordinates project menus and automatic recovery for the active shell features.
/// </summary>
public sealed class ProjectAutosaveCoordinator
{
    private readonly IDialogService dialogs;
    private readonly Dictionary<IShellProjectFeature, Task> loadTasks = [];
    private readonly IUserNotificationService notifications;
    private readonly IProjectService projects;

    /// <summary>
    ///     Creates the shared project lifecycle coordinator.
    /// </summary>
    /// <param name="projects">Loads, saves, and creates typed project data.</param>
    /// <param name="dialogs">Confirms destructive New project operations.</param>
    /// <param name="notifications">Publishes project lifecycle failures.</param>
    public ProjectAutosaveCoordinator(
        IProjectService projects,
        IDialogService dialogs,
        IUserNotificationService notifications)
    {
        this.projects = projects ?? throw new ArgumentNullException(nameof(projects));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
    }

    /// <summary>
    ///     Starts restoring a feature's automatic recovery project once.
    /// </summary>
    /// <param name="feature">The feature whose state is being activated.</param>
    public void Activate(IShellProjectFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        if (loadTasks.ContainsKey(feature)) return;

        loadTasks.Add(feature, LoadAutosaveAsync(feature));
    }

    /// <summary>
    ///     Saves a feature's current state to its automatic recovery file after any
    ///     pending restore has completed.
    /// </summary>
    /// <param name="feature">The feature whose state is being deactivated.</param>
    public void Deactivate(IShellProjectFeature feature)
    {
        ArgumentNullException.ThrowIfNull(feature);
        _ = SaveAutosaveAfterLoadAsync(feature);
    }

    /// <summary>
    ///     Saves the active feature through its project Save As workflow.
    /// </summary>
    /// <param name="feature">The feature whose state should be saved.</param>
    /// <param name="cancellationToken">Cancels picker result processing or persistence.</param>
    /// <returns>A task that completes after the save attempt.</returns>
    public Task SaveAsync(
        IShellProjectFeature feature,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            () => SaveProjectAsync(feature, cancellationToken),
            "Save project");
    }

    /// <summary>
    ///     Opens and installs a project selected for the active feature.
    /// </summary>
    /// <param name="feature">The feature that owns the project state.</param>
    /// <param name="cancellationToken">Cancels picker result processing or persistence.</param>
    /// <returns>A task that completes after the open attempt.</returns>
    public Task OpenAsync(
        IShellProjectFeature feature,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            () => OpenProjectAsync(feature, cancellationToken),
            "Open project");
    }

    /// <summary>
    ///     Confirms and installs a new default project for the active feature.
    /// </summary>
    /// <param name="feature">The feature that owns the project state.</param>
    /// <param name="cancellationToken">Cancels confirmation or project initialization.</param>
    /// <returns>A task that completes after the confirmation and initialization attempt.</returns>
    public Task NewAsync(
        IShellProjectFeature feature,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            () => NewProjectAsync(feature, cancellationToken),
            "New project");
    }

    private async Task LoadAutosaveAsync(IShellProjectFeature feature)
    {
        try
        {
            var definition = feature.ProjectDefinition;
            object project = await projects.LoadAsync(
                definition,
                projects.GetAutoSavePath(definition),
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
            await projects.AutoSaveAsync(
                feature.ProjectDefinition,
                feature.Snapshot(),
                feature.AdditionalAutoSavePaths);
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
        await projects.SaveAsAsync(
            feature.ProjectDefinition,
            feature.Snapshot(),
            cancellationToken);
    }

    private async Task OpenProjectAsync(
        IShellProjectFeature feature,
        CancellationToken cancellationToken)
    {
        await AwaitLoadAsync(feature);
        var opened = await projects.OpenAsync(
            feature.ProjectDefinition,
            cancellationToken);
        if (opened is not null) feature.Install(opened.Project);
    }

    private async Task NewProjectAsync(
        IShellProjectFeature feature,
        CancellationToken cancellationToken)
    {
        bool confirmed = await dialogs.ShowMessageAsync(
            new MessageDialogRequest<bool>(
                "Confirm new project",
                "Are you sure you want to start a new project? All unsaved progress will be lost.",
                [
                    new DialogChoice<bool>("Yes", true, true),
                    new DialogChoice<bool>("No", false, IsCancel: true),
                ],
                false),
            cancellationToken);
        if (!confirmed) return;

        await AwaitLoadAsync(feature);
        feature.Install(projects.CreateNew(feature.ProjectDefinition));
    }

    private async Task AwaitLoadAsync(IShellProjectFeature feature)
    {
        if (loadTasks.TryGetValue(feature, out var loadTask)) await loadTask;
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

    private Task PublishFailureAsync(string title, Exception exception)
    {
        return notifications.PublishAsync(new UserNotification(
            UserNotificationSeverity.Error,
            title,
            exception.Message,
            exception));
    }
}
