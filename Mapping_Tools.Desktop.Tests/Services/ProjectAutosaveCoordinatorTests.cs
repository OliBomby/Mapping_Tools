using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Desktop.Services;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Services;

[TestClass]
public sealed class ProjectAutosaveCoordinatorTests
{
    [TestMethod]
    public async Task Activate_WithExistingAutosave_InstallsLoadedProject()
    {
        // Arrange
        RecordingProjectService projects = new()
        {
            LoadedProject = new TestProject(42),
        };
        TestProjectFeature feature = new();
        var coordinator = CreateCoordinator(projects);

        // Act
        coordinator.Activate(feature);
        await feature.Installed.Task;

        // Assert
        feature.Value.Should().Be(42);
        feature.InstallCount.Should().Be(1);
    }

    [TestMethod]
    public async Task SaveOnShutdown_AfterAutosaveLoad_SavesCurrentSnapshot()
    {
        // Arrange
        RecordingProjectService projects = new();
        TestProjectFeature feature = new() { Value = 7 };
        var coordinator = CreateCoordinator(projects);
        coordinator.Activate(feature);

        // Act
        await coordinator.SaveOnShutdown(feature);

        // Assert
        projects.AutoSavedProjects.Should().ContainSingle()
            .Which.Value.Should().Be(7);
    }

    [TestMethod]
    public async Task SaveOnShutdown_PassesFeatureAdditionalAutosavePaths()
    {
        // Arrange
        RecordingProjectService projects = new();
        TestProjectFeature feature = new()
        {
            Value = 7,
            AdditionalAutoSavePaths = ["gallery\\project.json"],
        };
        var coordinator = CreateCoordinator(projects);

        // Act
        await coordinator.SaveOnShutdown(feature);

        // Assert
        projects.LastAdditionalAutoSavePaths.Should().Equal("gallery\\project.json");
    }

    [TestMethod]
    public async Task SaveOnShutdown_WhenPersistenceIsDelayed_DoesNotCompleteEarly()
    {
        // Arrange
        RecordingProjectService projects = new() { DelayAutoSave = true };
        TestProjectFeature feature = new() { Value = 7 };
        var coordinator = CreateCoordinator(projects);

        // Act
        Task saveTask = coordinator.SaveOnShutdown(feature);
        await projects.AutoSaveStarted.Task;

        // Assert
        saveTask.IsCompleted.Should().BeFalse();

        // Act
        projects.ReleaseAutoSave.TrySetResult();
        await saveTask;

        // Assert
        saveTask.IsCompletedSuccessfully.Should().BeTrue();
    }

    [TestMethod]
    public void SaveOnShutdown_AfterSuppressSave_DoesNotPersistProject()
    {
        // Arrange
        RecordingProjectService projects = new();
        TestProjectFeature feature = new() { Value = 7 };
        var coordinator = CreateCoordinator(projects);
        coordinator.Activate(feature);
        coordinator.SuppressSave();

        // Act
        coordinator.SaveOnShutdown(feature);

        // Assert
        projects.AutoSavedProjects.Should().BeEmpty();
    }

    [TestMethod]
    public async Task SaveAsync_WithActiveFeature_UsesFeatureSnapshot()
    {
        // Arrange
        RecordingProjectService projects = new();
        TestProjectFeature feature = new() { Value = 11 };
        var coordinator = CreateCoordinator(projects);

        // Act
        await coordinator.SaveAsync(feature);

        // Assert
        projects.SaveAsCount.Should().Be(1);
        projects.LastSaveAsProject!.Value.Should().Be(11);
        projects.LastSaveAsDefinition!.SuggestedFileName.Should().Be("test-project.json");
    }

    [TestMethod]
    public async Task OpenAsync_WithSelectedProject_InstallsOpenedProject()
    {
        // Arrange
        RecordingProjectService projects = new()
        {
            OpenedProject = new TestProject(23),
        };
        TestProjectFeature feature = new();
        var coordinator = CreateCoordinator(projects);

        // Act
        await coordinator.OpenAsync(feature);

        // Assert
        projects.OpenCount.Should().Be(1);
        feature.Value.Should().Be(23);
        feature.InstallCount.Should().Be(1);
    }

    [TestMethod]
    public async Task NewAsync_WhenConfirmed_InstallsDefinitionDefaults()
    {
        // Arrange
        RecordingProjectService projects = new();
        TestDialogService dialogs = new() { BooleanResult = true };
        TestProjectFeature feature = new() { Value = 99 };
        var coordinator = CreateCoordinator(projects, dialogs);

        // Act
        await coordinator.NewAsync(feature);

        // Assert
        projects.CreateNewCount.Should().Be(1);
        feature.Value.Should().Be(3);
        feature.InstallCount.Should().Be(1);
    }

    private static ProjectAutosaveCoordinator CreateCoordinator(
        RecordingProjectService projects,
        TestDialogService? dialogs = null)
    {
        return new ProjectAutosaveCoordinator(
            projects,
            dialogs ?? new TestDialogService { BooleanResult = true },
            new UserNotificationService());
    }

    private sealed class TestProjectFeature : IShellProjectFeature<TestProject>
    {
        private static readonly ProjectDefinition<TestProject> definition = new(
            "testproject.json",
            "Test Projects",
            static () => new TestProject(3),
            "test-project.json");

        public TaskCompletionSource<TestProject> Installed { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int Value { get; set; }

        public int InstallCount { get; private set; }

        public IReadOnlyList<string> AdditionalAutoSavePaths { get; init; } = [];

        public ProjectDefinition<TestProject> ProjectDefinition => definition;

        public TestProject Snapshot()
        {
            return new TestProject(Value);
        }

        public void Install(TestProject project)
        {
            Value = project.Value;
            InstallCount++;
            Installed.TrySetResult(project);
        }
    }

    private sealed record TestProject(int Value);

    private sealed class RecordingProjectService : IProjectService
    {
        public TestProject? LoadedProject { get; init; }

        public TestProject? OpenedProject { get; init; }

        public List<TestProject> AutoSavedProjects { get; } = [];

        public TaskCompletionSource<TestProject> LastAutoSave { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource AutoSaveStarted { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseAutoSave { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool DelayAutoSave { get; init; }

        public int SaveAsCount { get; private set; }

        public TestProject? LastSaveAsProject { get; private set; }

        public IReadOnlyList<string> LastAdditionalAutoSavePaths { get; private set; } = [];

        public ProjectDefinition<TestProject>? LastSaveAsDefinition { get; private set; }

        public int OpenCount { get; private set; }

        public int CreateNewCount { get; private set; }

        public string GetAutoSavePath<TProject>(ProjectDefinition<TProject> definition)
        {
            return definition.AutoSaveFileName;
        }

        public string GetProjectFolder<TProject>(ProjectDefinition<TProject> definition)
        {
            return definition.ProjectFolderName;
        }

        public TProject CreateNew<TProject>(ProjectDefinition<TProject> definition)
        {
            CreateNewCount++;
            return definition.CreateProject();
        }

        public Task SaveAsync<TProject>(
            string path,
            TProject project,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<TProject> LoadAsync<TProject>(
            string path,
            CancellationToken cancellationToken = default)
        {
            return LoadedProject is not TProject project
                ? Task.FromException<TProject>(new FileNotFoundException())
                : Task.FromResult(project);
        }

        public Task<TProject> LoadAutoSaveAsync<TProject>(
            ProjectDefinition<TProject> definition,
            CancellationToken cancellationToken = default)
        {
            return LoadAsync<TProject>(definition.AutoSaveFileName, cancellationToken);
        }

        public async Task AutoSaveAsync<TProject>(
            ProjectDefinition<TProject> definition,
            TProject project,
            IEnumerable<string>? additionalPaths = null,
            CancellationToken cancellationToken = default)
        {
            if (project is not TestProject typed)
                throw new InvalidOperationException(
                    "The test project service received an unexpected project type.");

            AutoSavedProjects.Add(typed);
            LastAdditionalAutoSavePaths = additionalPaths?.ToArray() ?? [];
            LastAutoSave.TrySetResult(typed);
            AutoSaveStarted.TrySetResult();
            if (DelayAutoSave) await ReleaseAutoSave.Task;
        }

        public Task<string?> SaveAsAsync<TProject>(
            ProjectDefinition<TProject> definition,
            TProject project,
            string? suggestedFileName = null,
            CancellationToken cancellationToken = default)
        {
            if (definition is not ProjectDefinition<TestProject> typedDefinition
                || project is not TestProject typed)
                return Task.FromException<string?>(
                    new InvalidOperationException("The test project service received an unexpected project type."));

            SaveAsCount++;
            LastSaveAsDefinition = typedDefinition;
            LastSaveAsProject = typed;
            return Task.FromResult<string?>("saved.json");
        }

        public Task<ProjectOpenResult<TProject>?> OpenAsync<TProject>(
            ProjectDefinition<TProject> definition,
            CancellationToken cancellationToken = default)
        {
            OpenCount++;
            return OpenedProject is not TProject project
                ? Task.FromResult<ProjectOpenResult<TProject>?>(null)
                : Task.FromResult<ProjectOpenResult<TProject>?>(
                    new ProjectOpenResult<TProject>("opened.json", project));
        }
    }
}
