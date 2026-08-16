using Mapping_Tools.Application.Projects;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>Shares the missing-file and failure handling for feature autosaves.</summary>
internal static class ProjectAutosaveCoordinator
{
    /// <summary>
    /// Loads a feature autosave and installs it, ignoring an absent autosave.
    /// </summary>
    /// <typeparam name="TProject">The feature-specific project type.</typeparam>
    /// <param name="projects">Loads typed projects and resolves autosave paths.</param>
    /// <param name="definition">Identifies the feature's autosave document.</param>
    /// <param name="install">Installs the validated project into presentation state.</param>
    /// <param name="publishFailure">Reports an invalid or unreadable autosave.</param>
    /// <param name="completed">Runs after the load attempt, including when it fails.</param>
    /// <returns>A task that completes after loading, failure reporting, and completion handling.</returns>
    public static async Task LoadAsync<TProject>(
        IProjectService projects,
        ProjectDefinition<TProject> definition,
        Action<TProject> install,
        Func<Exception, Task> publishFailure,
        Action? completed = null)
    {
        try
        {
            TProject project = await projects.LoadAsync<TProject>(
                projects.GetAutoSavePath(definition));
            install(project);
        }
        catch (FileNotFoundException)
        {
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (Exception exception)
        {
            await publishFailure(exception);
        }
        finally
        {
            completed?.Invoke();
        }
    }

    /// <summary>Snapshots and writes a feature autosave, reporting write failures.</summary>
    /// <typeparam name="TProject">The feature-specific project type.</typeparam>
    /// <param name="projects">Writes typed projects and resolves autosave paths.</param>
    /// <param name="definition">Identifies the feature's autosave document.</param>
    /// <param name="snapshot">Creates the complete project state to persist.</param>
    /// <param name="publishFailure">Reports an autosave failure.</param>
    /// <returns>A task that completes after the autosave attempt and any failure reporting.</returns>
    public static async Task SaveAsync<TProject>(
        IProjectService projects,
        ProjectDefinition<TProject> definition,
        Func<TProject> snapshot,
        Func<Exception, Task> publishFailure)
    {
        try
        {
            await projects.AutoSaveAsync(definition, snapshot());
        }
        catch (Exception exception)
        {
            await publishFailure(exception);
        }
    }
}
