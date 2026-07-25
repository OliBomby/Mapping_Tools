using Mapping_Tools.ApplicationServices.Platform;
using Mapping_Tools.ApplicationServices.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Platform.Tests;

[TestClass]
public sealed class ProjectServiceTests
{
    [TestMethod]
    public void DefinitionRejectsPathsThatEscapeApplicationData()
    {
        Assert.ThrowsException<ArgumentException>(
            () => new ProjectDefinition<string>("nested/project.json", "Projects", () => ""));
        Assert.ThrowsException<ArgumentException>(
            () => new ProjectDefinition<string>("project.json", "../Projects", () => ""));
    }

    [TestMethod]
    public void PathsPreserveLegacyApplicationDataLayout()
    {
        TestDirectories directories = new(@"C:\MappingToolsData");
        ProjectService service = new(directories, new FakeFilePicker(), new FakeProjectStore());
        ProjectDefinition<TestProject> definition = CreateDefinition();

        Assert.AreEqual(
            Path.Combine(directories.ApplicationData, "featureproject.json"),
            service.GetAutoSavePath(definition));
        Assert.AreEqual(
            Path.Combine(directories.ApplicationData, "Feature Projects"),
            service.GetProjectFolder(definition));
    }

    [TestMethod]
    public void CreateNewUsesFeatureFactoryWithoutKnowingPresentationState()
    {
        ProjectService service = CreateService(new FakeFilePicker(), new FakeProjectStore());
        ProjectDefinition<TestProject> definition = CreateDefinition();

        TestProject project = service.CreateNew(definition);

        Assert.AreEqual("new project", project.Name);
    }

    [TestMethod]
    public async Task AutoSaveWritesPrimaryThenDistinctAdditionalTargets()
    {
        FakeProjectStore store = new();
        TestDirectories directories = new(Path.GetTempPath());
        ProjectService service = new(directories, new FakeFilePicker(), store);
        ProjectDefinition<TestProject> definition = CreateDefinition();
        string primary = service.GetAutoSavePath(definition);
        string collection = Path.Combine(directories.ApplicationData, "Collection", "project.json");

        await service.AutoSaveAsync(
            definition,
            new TestProject("snapshot"),
            [collection, primary]);

        CollectionAssert.AreEqual(
            new[] { Path.GetFullPath(primary), Path.GetFullPath(collection) },
            store.SavedPaths.ToArray());
        Assert.IsTrue(store.SavedProjects.All(project => project.Name == "snapshot"));
    }

    [TestMethod]
    public async Task PreCancelledAutoSaveWritesNoTarget()
    {
        FakeProjectStore store = new();
        ProjectService service = CreateService(new FakeFilePicker(), store);
        using CancellationTokenSource cancellation = new();
        cancellation.Cancel();

        await Assert.ThrowsExceptionAsync<OperationCanceledException>(
            () => service.AutoSaveAsync(
                CreateDefinition(),
                new TestProject("cancelled"),
                cancellationToken: cancellation.Token));

        Assert.AreEqual(0, store.SavedPaths.Count);
    }

    [TestMethod]
    public async Task CancelledSaveAsDoesNotWrite()
    {
        FakeFilePicker picker = new() { SavePath = null };
        FakeProjectStore store = new();
        ProjectService service = CreateService(picker, store);

        string? path = await service.SaveAsAsync(
            CreateDefinition(),
            new TestProject("unsaved"),
            "example.json");

        Assert.IsNull(path);
        Assert.AreEqual(0, store.SavedPaths.Count);
        Assert.AreEqual(1, store.EnsuredDirectories.Count);
        Assert.AreEqual("example.json", picker.LastSaveRequest?.SuggestedFileName);
        Assert.AreEqual("json", picker.LastSaveRequest?.DefaultExtension);
    }

    [TestMethod]
    public async Task SaveAsWritesSelectedPath()
    {
        string selectedPath = Path.Combine(Path.GetTempPath(), "chosen.json");
        FakeFilePicker picker = new() { SavePath = selectedPath };
        FakeProjectStore store = new();
        ProjectService service = CreateService(picker, store);

        string? path = await service.SaveAsAsync(
            CreateDefinition(),
            new TestProject("chosen"));

        Assert.AreEqual(selectedPath, path);
        CollectionAssert.AreEqual(new[] { selectedPath }, store.SavedPaths.ToArray());
    }

    [TestMethod]
    public async Task CancelledOpenDoesNotRead()
    {
        FakeFilePicker picker = new() { OpenPaths = [] };
        FakeProjectStore store = new();
        ProjectService service = CreateService(picker, store);

        ProjectOpenResult<TestProject>? result =
            await service.OpenAsync(CreateDefinition());

        Assert.IsNull(result);
        Assert.AreEqual(0, store.LoadedPaths.Count);
    }

    [TestMethod]
    public async Task OpenReturnsDataWithoutInstallingItInAView()
    {
        string selectedPath = Path.Combine(Path.GetTempPath(), "opened.json");
        FakeFilePicker picker = new() { OpenPaths = [selectedPath] };
        FakeProjectStore store = new()
        {
            ProjectToLoad = new TestProject("loaded")
        };
        ProjectService service = CreateService(picker, store);

        ProjectOpenResult<TestProject>? result =
            await service.OpenAsync(CreateDefinition());

        Assert.IsNotNull(result);
        Assert.AreEqual(selectedPath, result.Path);
        Assert.AreEqual("loaded", result.Project.Name);
        CollectionAssert.AreEqual(new[] { selectedPath }, store.LoadedPaths.ToArray());
        Assert.AreEqual(false, picker.LastOpenRequest?.AllowMultiple);
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
