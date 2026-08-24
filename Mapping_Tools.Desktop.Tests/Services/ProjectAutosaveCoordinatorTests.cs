using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Projects;
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
    public async Task Deactivate_AfterAutosaveLoad_SavesCurrentSnapshot()
    {
        // Arrange
        RecordingProjectService projects = new();
        TestProjectFeature feature = new() { Value = 7 };
        var coordinator = CreateCoordinator(projects);
        coordinator.Activate(feature);

        // Act
        coordinator.Deactivate(feature);
        await projects.LastAutoSave.Task;

        // Assert
        projects.AutoSavedProjects.Should().ContainSingle()
            .Which.Value.Should().Be(7);
    }

    [TestMethod]
    public async Task Deactivate_PassesFeatureAdditionalAutosavePaths()
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
        coordinator.Deactivate(feature);
        await projects.LastAutoSave.Task;

        // Assert
        projects.LastAdditionalAutoSavePaths.Should().Equal("gallery\\project.json");
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

    private sealed class TestProjectFeature : IShellProjectFeature
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

        public IProjectDefinition ProjectDefinition => definition;

        public object Snapshot()
        {
            return new TestProject(Value);
        }

        public void Install(object project)
        {
            var typed = (TestProject)project;
            Value = typed.Value;
            InstallCount++;
            Installed.TrySetResult(typed);
        }
    }

    private sealed record TestProject(int Value);

    private sealed class RecordingProjectService : IProjectService
    {
        public TestProject? LoadedProject { get; init; }

        public TestProject? OpenedProject { get; init; }

        public List<TestProject> AutoSavedProjects { get; } = [];

        public TaskCompletionSource<object> LastAutoSave { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int SaveAsCount { get; private set; }

        public TestProject? LastSaveAsProject { get; private set; }

        public IReadOnlyList<string> LastAdditionalAutoSavePaths { get; private set; } = [];

        public IProjectDefinition? LastSaveAsDefinition { get; private set; }

        public int OpenCount { get; private set; }

        public int CreateNewCount { get; private set; }

        public string GetAutoSavePath(IProjectDefinition definition)
        {
            return definition.AutoSaveFileName;
        }

        public string GetAutoSavePath<TProject>(ProjectDefinition<TProject> definition)
        {
            return definition.AutoSaveFileName;
        }

        public string GetProjectFolder(IProjectDefinition definition)
        {
            return definition.ProjectFolderName;
        }

        public string GetProjectFolder<TProject>(ProjectDefinition<TProject> definition)
        {
            return definition.ProjectFolderName;
        }

        public object CreateNew(IProjectDefinition definition)
        {
            CreateNewCount++;
            return new TestProject(3);
        }

        public TProject CreateNew<TProject>(ProjectDefinition<TProject> definition)
        {
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
            return Task.FromException<TProject>(new FileNotFoundException());
        }

        public Task<object> LoadAsync(
            IProjectDefinition definition,
            string path,
            CancellationToken cancellationToken = default)
        {
            return LoadedProject is null
                ? Task.FromException<object>(new FileNotFoundException())
                : Task.FromResult<object>(LoadedProject);
        }

        public Task AutoSaveAsync<TProject>(
            ProjectDefinition<TProject> definition,
            TProject project,
            IEnumerable<string>? additionalPaths = null,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task AutoSaveAsync(
            IProjectDefinition definition,
            object project,
            IEnumerable<string>? additionalPaths = null,
            CancellationToken cancellationToken = default)
        {
            AutoSavedProjects.Add((TestProject)project);
            LastAdditionalAutoSavePaths = additionalPaths?.ToArray() ?? [];
            LastAutoSave.TrySetResult(project);
            return Task.CompletedTask;
        }

        public Task<string?> SaveAsAsync<TProject>(
            ProjectDefinition<TProject> definition,
            TProject project,
            string? suggestedFileName = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<string?>(null);
        }

        public Task<string?> SaveAsAsync(
            IProjectDefinition definition,
            object project,
            CancellationToken cancellationToken = default)
        {
            SaveAsCount++;
            LastSaveAsDefinition = definition;
            LastSaveAsProject = (TestProject)project;
            return Task.FromResult<string?>("saved.json");
        }

        public Task<ProjectOpenResult<TProject>?> OpenAsync<TProject>(
            ProjectDefinition<TProject> definition,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ProjectOpenResult<TProject>?>(null);
        }

        public Task<ProjectOpenResult?> OpenAsync(
            IProjectDefinition definition,
            CancellationToken cancellationToken = default)
        {
            OpenCount++;
            return OpenedProject is null
                ? Task.FromResult<ProjectOpenResult?>(null)
                : Task.FromResult<ProjectOpenResult?>(
                    new ProjectOpenResult("opened.json", OpenedProject));
        }
    }
}
