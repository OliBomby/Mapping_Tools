using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Tests.TestDoubles;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Application.Tests.Projects;

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
        ProjectService service = new(directories, new RecordingFilePicker(), new FakeProjectStore());
        var definition = CreateDefinition();

        // Assert
        service.GetAutoSavePath(definition).Should().Be(Path.Combine(directories.ApplicationData, "featureproject.json"));
        service.GetProjectFolder(definition).Should().Be(Path.Combine(directories.ApplicationData, "Feature Projects"));
    }

    [TestMethod]
    public void CreateNew_DefaultDefinition_UsesFeatureFactory()
    {
        // Arrange
        var service = CreateService(new RecordingFilePicker(), new FakeProjectStore());
        var definition = CreateDefinition();

        // Act
        var project = service.CreateNew(definition);

        // Assert
        project.Name.Should().Be("new project");
    }

    [TestMethod]
    public async Task AutoSaveAsync_WithAdditionalTargets_WritesPrimaryThenDistinctTargets()
    {
        // Arrange
        FakeProjectStore store = new();
        TestDirectories directories = new(Path.GetTempPath());
        ProjectService service = new(directories, new RecordingFilePicker(), store);
        var definition = CreateDefinition();
        string primary = service.GetAutoSavePath(definition);
        string collection = Path.Combine(directories.ApplicationData, "Collection", "project.json");

        // Act
        await service.AutoSaveAsync(
            definition,
            new TestProject("snapshot"),
            [collection, primary]);

        // Assert
        store.SavedPaths.ToArray().Should().Equal(Path.GetFullPath(primary), Path.GetFullPath(collection));
        store.SavedProjects.All(project => project.Name == "snapshot").Should().BeTrue();
    }

    [TestMethod]
    public async Task AutoSaveAsync_WithPreCancelledToken_WritesNothing()
    {
        // Arrange
        FakeProjectStore store = new();
        var service = CreateService(new RecordingFilePicker(), store);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        // Act
        var act3 = () => service.AutoSaveAsync(
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
        RecordingFilePicker picker = new() { SavePath = null };
        FakeProjectStore store = new();
        var service = CreateService(picker, store);

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
        RecordingFilePicker picker = new() { SavePath = selectedPath };
        FakeProjectStore store = new();
        var service = CreateService(picker, store);

        // Act
        string? path = await service.SaveAsAsync(
            CreateDefinition(),
            new TestProject("chosen"));

        // Assert
        path.Should().Be(selectedPath);
        store.SavedPaths.ToArray().Should().Equal(selectedPath);
    }

    [TestMethod]
    public async Task OpenAsync_WhenPickerCancelled_DoesNotRead()
    {
        // Arrange
        RecordingFilePicker picker = new() { OpenFiles = [] };
        FakeProjectStore store = new();
        var service = CreateService(picker, store);

        // Act
        var result =
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
        RecordingFilePicker picker = new() { OpenFiles = [selectedPath] };
        FakeProjectStore store = new()
        {
            ProjectToLoad = new TestProject("loaded"),
        };
        var service = CreateService(picker, store);

        // Act
        var result =
            await service.OpenAsync(CreateDefinition());

        // Assert
        result.Should().NotBeNull();
        result.Path.Should().Be(selectedPath);
        result.Project.Name.Should().Be("loaded");
        store.LoadedPaths.ToArray().Should().Equal(selectedPath);
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
        RecordingFilePicker picker,
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
            if (project is TestProject testProject) SavedProjects.Add(testProject);

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

}
