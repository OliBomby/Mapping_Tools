using System.Text.Json;
using Mapping_Tools.Application.BeatmapEditing;
using Mapping_Tools.Application.BeatmapEditing.Contracts;
using Mapping_Tools.Application.BeatmapEditing.Models;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Infrastructure.Files;
using Mapping_Tools.Infrastructure.Projects;

namespace Mapping_Tools.Application.Tests.Execution;

public abstract class TransformationFixtureTestBase
{
    private static readonly IProjectSerializer projectJson = new LegacyProjectJsonSerializer();

    protected static FixtureContext CreateFixture(string toolName, string fixtureName)
    {
        return new FixtureContext(toolName, fixtureName);
    }

    protected sealed record FixtureExecutionResult(
        IReadOnlyList<string>? OutputPaths = null,
        string? JsonOutput = null,
        string? OutputDirectory = null,
        IReadOnlyList<double>? Progress = null)
    {
        public bool WasExecuted => OutputPaths is not null || JsonOutput is not null || OutputDirectory is not null;
    }

    protected sealed class FixtureContext : IDisposable
    {
        private readonly FixtureWorkspace workspace;

        public FixtureContext(string toolName, string fixtureName)
        {
            workspace = new(Path.Combine(AppContext.BaseDirectory, "Fixtures"));
            WorkspaceRoot = workspace.Root;
            FixtureRoot = Path.Combine(WorkspaceRoot, "Transformations", toolName, fixtureName);

            string recordPath = Path.Combine(FixtureRoot, "metadata.json");
            using JsonDocument record = JsonDocument.Parse(File.ReadAllText(recordPath));
            Record = record.RootElement.Clone();

            string optionsPath = ResolveFixturePath(StringProperty(Record, "options"));
            using JsonDocument options = JsonDocument.Parse(File.ReadAllText(optionsPath));
            Options = options.RootElement.Clone();

            ExpectedOutputPath = ResolveFixturePath(StringProperty(Record, "expectedOutput"));
            SeedInput = ResolveFixturePath(StringProperty(Record, "seedInput"));
            SecondaryInput = OptionalStringProperty(Record, "secondaryInput") is { } secondary
                ? ResolveFixturePath(secondary)
                : null;

            Options.ValueKind.Should().Be(JsonValueKind.Object);
            File.ReadAllBytes(SeedInput).Should().NotBeEmpty();
            if (SecondaryInput is not null) File.ReadAllBytes(SecondaryInput).Should().NotBeEmpty();
            File.ReadAllText(Path.Combine(FixtureRoot, "report.md"))
                .Should().NotBeNullOrWhiteSpace();
            File.ReadAllText(ExpectedOutputPath).Should().NotBeNullOrWhiteSpace();

            Gateway = new FileBackedEditingGateway();
        }

        public string WorkspaceRoot { get; }

        public string FixtureRoot { get; }

        public JsonElement Record { get; }

        public JsonElement Options { get; }

        public string ExpectedOutputPath { get; }

        public string SeedInput { get; }

        public string? SecondaryInput { get; }

        public FileBackedEditingGateway Gateway { get; }

        public string TargetPath => OptionalPath("Target")
                                     ?? OptionalPath("BaseBeatmap")
                                     ?? OptionalPath("ExportPath")
                                     ?? SeedInput;

        public void AssertAccepted(string feature)
        {
            Record.GetProperty("feature").GetString().Should().Be(feature);
            Record.GetProperty("status").GetString().Should().Be("accepted");
        }

        public void AssertTextOutput(FixtureExecutionResult actual)
        {
            actual.WasExecuted.Should().BeTrue();
            actual.OutputPaths.Should().ContainSingle();
            AssertTextOutputEquivalent(ExpectedOutputPath, actual.OutputPaths.Single());
        }

        public string? OptionalPath(string property)
        {
            return OptionalStringProperty(Options, property) is { } value
                ? ResolveFixturePath(value)
                : null;
        }

        public string RequiredPath(string property)
        {
            return ResolveFixturePath(StringProperty(Options, property));
        }

        public double NumberProperty(string property)
        {
            return Options.GetProperty(property).GetDouble();
        }

        public int IntProperty(string property)
        {
            return Options.GetProperty(property).GetInt32();
        }

        public T ReadProject<T>()
        {
            string projectPath = Path.Combine(FixtureRoot, "project.json");
            string json = File.ReadAllText(projectPath).Replace(
                "{fixtureRoot}",
                WorkspaceRoot.Replace('\\', '/'),
                StringComparison.Ordinal);
            return projectJson.Deserialize<T>(json);
        }

        public string ResolveFixturePath(string relativePath)
        {
            return ResolvePath(FixtureRoot, relativePath);
        }

        public void Dispose()
        {
            workspace.Dispose();
        }

        private static string ResolvePath(string root, string relativePath)
        {
            string path = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
            (File.Exists(path) || Directory.Exists(path)).Should().BeTrue(
                $"Fixture path does not exist: {relativePath}");
            return path;
        }

        private static string StringProperty(JsonElement element, string property)
        {
            return element.GetProperty(property).GetString()
                   ?? throw new InvalidDataException($"Fixture property {property} is null.");
        }

        private static string? OptionalStringProperty(JsonElement element, string property)
        {
            return element.TryGetProperty(property, out var value) && value.ValueKind != JsonValueKind.Null
                ? value.GetString()
                : null;
        }
    }

    protected sealed class FixtureWorkspace : IDisposable
    {
        public FixtureWorkspace(string sourceRoot)
        {
            Root = Path.Combine(
                Path.GetTempPath(),
                "mapping-tools-transformations-" + Guid.NewGuid().ToString("N"));
            foreach (string sourcePath in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
            {
                string destinationPath = Path.Combine(Root, Path.GetRelativePath(sourceRoot, sourcePath));
                Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                File.Copy(sourcePath, destinationPath);
            }
        }

        public string Root { get; }

        public void Dispose()
        {
            if (Directory.Exists(Root)) Directory.Delete(Root, true);
        }
    }

    protected sealed class FileBackedEditingGateway : IBeatmapEditingGateway
    {
        private readonly PhysicalBeatmapsetFileSystem files = new();

        public Task<BeatmapEditingSession> OpenBeatmapAsync(
            string path,
            LiveBeatmapPreference livePreference = LiveBeatmapPreference.PreferLive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new BeatmapEditingSession(path, files, BeatmapEditingSource.Disk));
        }

        public Task<StoryboardEditingSession> OpenStoryboardAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new StoryboardEditingSession(path, files));
        }

        public Task SaveAsync(
            EditingSession value,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            value.SaveFile();
            return Task.CompletedTask;
        }

        public Task SaveAsync(
            BeatmapEditingSession session,
            bool reloadEditor = false,
            CancellationToken cancellationToken = default)
        {
            return SaveAsync((EditingSession)session, reloadEditor, cancellationToken);
        }
    }

    protected static void AssertTextOutputEquivalent(string expectedPath, string actualPath)
    {
        File.Exists(actualPath).Should().BeTrue($"Tool did not write output: {actualPath}");
        string actual = File.ReadAllText(actualPath);
        string expected = File.ReadAllText(expectedPath);
        actual.Should().Be(expected);
    }

}
