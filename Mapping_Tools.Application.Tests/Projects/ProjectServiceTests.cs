using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests;

[TestClass]
public sealed class ProjectServiceTests
{
    [TestMethod]
    public void Constructor_WithEscapingPaths_ThrowsArgumentException()
    {
        // Arrange
        // Act
        Action act1 = () => new ProjectDefinition<string>("nested/project.json", "Projects", () => "");

        // Assert
        act1.Should().Throw<ArgumentException>();
        Action act2 = () => new ProjectDefinition<string>("project.json", "../Projects", () => "");

        act2.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void GetPrimaryPath_DefaultDefinition_PreservesLegacyLayout()
    {
        // Arrange
        // Act
        TestDirectories directories = new(@"C:\MappingToolsData");
        ProjectService service = new(directories, new FakeFilePicker(), new FakeProjectStore());
        ProjectDefinition<TestProject> definition = CreateDefinition();

        // Assert
        service.GetAutoSavePath(definition).Should().Be(Path.Combine(directories.ApplicationData, "featureproject.json"));
        service.GetProjectFolder(definition).Should().Be(Path.Combine(directories.ApplicationData, "Feature Projects"));
    }

    [TestMethod]
    public void CreateNew_DefaultDefinition_UsesFeatureFactory()
    {
        // Arrange
        ProjectService service = CreateService(new FakeFilePicker(), new FakeProjectStore());
        ProjectDefinition<TestProject> definition = CreateDefinition();

        // Act
        TestProject project = service.CreateNew(definition);

        // Assert
        project.Name.Should().Be("new project");
    }

    [TestMethod]
    public async Task AutoSaveAsync_WithAdditionalTargets_WritesPrimaryThenDistinctTargets()
    {
        // Arrange
        FakeProjectStore store = new();
        TestDirectories directories = new(Path.GetTempPath());
        ProjectService service = new(directories, new FakeFilePicker(), store);
        ProjectDefinition<TestProject> definition = CreateDefinition();
        string primary = service.GetAutoSavePath(definition);
        string collection = Path.Combine(directories.ApplicationData, "Collection", "project.json");

        // Act
        await service.AutoSaveAsync(
            definition,
            new TestProject("snapshot"),
            [collection, primary]);

        // Assert
        store.SavedPaths.ToArray().Should().Equal(new[] { Path.GetFullPath(primary), Path.GetFullPath(collection) });
        store.SavedProjects.All(project => project.Name == "snapshot").Should().BeTrue();
    }

    [TestMethod]
    public async Task AutoSaveAsync_WithPreCancelledToken_WritesNothing()
    {
        // Arrange
        FakeProjectStore store = new();
        ProjectService service = CreateService(new FakeFilePicker(), store);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        Func<Task> act3 = () => service.AutoSaveAsync(
                CreateDefinition(),
                new TestProject("cancelled"),
                cancellationToken: cancellation.Token);

        // Assert
        await act3.Should().ThrowAsync<OperationCanceledException>();

        store.SavedPaths.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task SaveAsAsync_WhenPickerCancelled_DoesNotWrite()
    {
        // Arrange
        FakeFilePicker picker = new() { SavePath = null };
        FakeProjectStore store = new();
        ProjectService service = CreateService(picker, store);

        // Act
        string? path = await service.SaveAsAsync(
            CreateDefinition(),
            new TestProject("unsaved"),
            "example.json");

        // Assert
        path.Should().BeNull();
        store.SavedPaths.Count.Should().Be(0);
        store.EnsuredDirectories.Count.Should().Be(1);
        (picker.LastSaveRequest?.SuggestedFileName).Should().Be("example.json");
        (picker.LastSaveRequest?.DefaultExtension).Should().Be("json");
    }

    [TestMethod]
    public async Task SaveAsAsync_WithSelectedPath_WritesSelectedPath()
    {
        // Arrange
        string selectedPath = Path.Combine(Path.GetTempPath(), "chosen.json");
        FakeFilePicker picker = new() { SavePath = selectedPath };
        FakeProjectStore store = new();
        ProjectService service = CreateService(picker, store);

        // Act
        string? path = await service.SaveAsAsync(
            CreateDefinition(),
            new TestProject("chosen"));

        // Assert
        path.Should().Be(selectedPath);
        store.SavedPaths.ToArray().Should().Equal(new[] { selectedPath });
    }

    [TestMethod]
    public async Task OpenAsync_WhenPickerCancelled_DoesNotRead()
    {
        // Arrange
        FakeFilePicker picker = new() { OpenPaths = [] };
        FakeProjectStore store = new();
        ProjectService service = CreateService(picker, store);

        // Act
        ProjectOpenResult<TestProject>? result =
            await service.OpenAsync(CreateDefinition());

        // Assert
        result.Should().BeNull();
        store.LoadedPaths.Count.Should().Be(0);
    }

    [TestMethod]
    public async Task OpenAsync_WithSelectedProject_ReturnsDataWithoutPresentationState()
    {
        // Arrange
        string selectedPath = Path.Combine(Path.GetTempPath(), "opened.json");
        FakeFilePicker picker = new() { OpenPaths = [selectedPath] };
        FakeProjectStore store = new()
        {
            ProjectToLoad = new TestProject("loaded")
        };
        ProjectService service = CreateService(picker, store);

        // Act
        ProjectOpenResult<TestProject>? result =
            await service.OpenAsync(CreateDefinition());

        // Assert
        result.Should().NotBeNull();
        result.Path.Should().Be(selectedPath);
        result.Project.Name.Should().Be("loaded");
        store.LoadedPaths.ToArray().Should().Equal(new[] { selectedPath });
        (picker.LastOpenRequest?.AllowMultiple).Should().Be(false);
    }

    private static ProjectDefinition<TestProject> CreateDefinition()
    {
        return new ProjectDefinition<TestProject>(
            "featureproject.json",
            "Feature Projects",
            () => new TestProject("new project"));
    }

    private static ProjectService CreateService(
        FakeFilePicker picker,
        FakeProjectStore store)
    {
        return new ProjectService(
            new TestDirectories(Path.GetTempPath()),
            picker,
            store);
    }

    private sealed record TestProject(string Name);

    private sealed class TestDirectories(string applicationData) : IApplicationDirectories
    {
        public string LocalApplicationData => applicationData;

        public string ApplicationData => applicationData;

        public string Exports => Path.Combine(applicationData, "Exports");

        public string ConfigurationFile => Path.Combine(applicationData, "config.json");

        public void EnsureCreated()
        {
        }
    }

    private sealed class FakeProjectStore : IProjectStore
    {
        public List<string> EnsuredDirectories { get; } = [];

        public List<string> SavedPaths { get; } = [];

        public List<TestProject> SavedProjects { get; } = [];

        public List<string> LoadedPaths { get; } = [];

        public TestProject ProjectToLoad { get; init; } = new("loaded");

        public void EnsureDirectoryExists(string path)
        {
            EnsuredDirectories.Add(path);
        }

        public Task SaveAsync<TProject>(
            string path,
            TProject project,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SavedPaths.Add(path);
            if (project is TestProject testProject)
            {
                SavedProjects.Add(testProject);
            }

            return Task.CompletedTask;
        }

        public Task<TProject> LoadAsync<TProject>(
            string path,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LoadedPaths.Add(path);
            return Task.FromResult((TProject)(object)ProjectToLoad);
        }
    }

    private sealed class FakeFilePicker : IFilePicker
    {
        public bool CanOpenFiles => true;

        public bool CanSaveFiles => true;

        public bool CanPickFolders => true;

        public string? SavePath { get; init; }

        public IReadOnlyList<string> OpenPaths { get; init; } = [];

        public SaveFilePickerRequest? LastSaveRequest { get; private set; }

        public OpenFilePickerRequest? LastOpenRequest { get; private set; }

        public Task<IReadOnlyList<string>> PickOpenFilesAsync(
            OpenFilePickerRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastOpenRequest = request;
            return Task.FromResult(OpenPaths);
        }

        public Task<string?> PickSaveFileAsync(
            SaveFilePickerRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastSaveRequest = request;
            return Task.FromResult(SavePath);
        }

        public Task<IReadOnlyList<string>> PickFoldersAsync(
            OpenFolderPickerRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }
}
