using System.Reflection;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Tools.PatternGallery;
using Mapping_Tools.Application.Tools.PatternGallery.Contracts;
using Mapping_Tools.Application.Tools.PatternGallery.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.PatternGallery.Models;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.PatternGallery.Models;
using Mapping_Tools.Desktop.Tools.PatternGallery.ViewModels;
using Mapping_Tools.Infrastructure.Projects;

namespace Mapping_Tools.Desktop.Tests.Tools.PatternGallery.Views;

internal sealed class PatternGalleryViewTestHarness
{
    private PatternGalleryViewTestHarness(
        PatternGalleryViewModel viewModel,
        RecordingPatternGalleryService gallery,
        Func<string?> renamedFolderName)
    {
        ViewModel = viewModel;
        Gallery = gallery;
        RenamedFolderName = renamedFolderName;
    }

    internal PatternGalleryViewModel ViewModel { get; }

    internal RecordingPatternGalleryService Gallery { get; }

    internal Func<string?> RenamedFolderName { get; }

    internal static PatternGalleryViewTestHarness Create(int patternCount = 0)
    {
        RecordingPatternGalleryService gallery = new();
        string? renamedFolderName = null;
        PatternGalleryCollectionPaths paths = new("C:/collections", "C:/collections/Default", "C:/collections/Default/Pattern Files", "C:/collections/Default/project.json");
        IPatternGalleryFileService files = InterfaceProxy<IPatternGalleryFileService>.Create((method, args) =>
        {
            if (method.Name == nameof(IPatternGalleryFileService.EnsureCollection)) return null;
            if (method.Name == nameof(IPatternGalleryFileService.Resolve)) return paths;
            if (method.Name == nameof(IPatternGalleryFileService.RenameCollection))
            {
                string folder = (string)args![1]!;
                renamedFolderName = folder;
                paths = paths with
                {
                    Collection = Path.Combine(paths.Root, folder),
                    PatternFiles = Path.Combine(paths.Root, folder, "Pattern Files"),
                    ProjectFile = Path.Combine(paths.Root, folder, "project.json"),
                };
                return paths;
            }

            return InterfaceProxy<IPatternGalleryFileService>.DefaultResult(method);
        });
        IPatternGalleryArchiveService archives = InterfaceProxy<IPatternGalleryArchiveService>.Create(
            (method, _) => InterfaceProxy<IPatternGalleryArchiveService>.DefaultResult(method));
        IProjectService projects = InterfaceProxy<IProjectService>.Create((method, _) =>
            method.Name == nameof(IProjectService.GetProjectFolder)
                ? "C:/collections"
                : InterfaceProxy<IProjectService>.DefaultResult(method));
        TestBeatmapWorkspace workspace = new() { QuickRunPath = "current.osu" };
        UserNotificationService notifications = new();
        PatternGalleryViewModel viewModel = new(
            gallery,
            files,
            archives,
            new ToolExecutionService(notifications, TimeProvider.System),
            workspace,
            new TestCurrentBeatmapDialogService(),
            new TestFilePicker(),
            new TestFileRevealService(),
            projects,
            new VersionedProjectJsonSerializer(),
            new TestApplicationDirectories(),
            new TestDialogService(),
            new DesktopApplicationSettings(),
            notifications,
            new ImmediateTestDispatcher());

        PatternGalleryProject project = new()
        {
            CollectionName = "Collection",
            FileHandler = new PatternGalleryCollectionMetadata
            {
                CollectionFolderName = "Default",
                PatternFilesFolderName = "Pattern Files",
            },
        };
        for (int index = 0; index < patternCount; index++)
            project.Patterns.Add(new PatternGalleryPattern
            {
                Name = $"Pattern {index:D3}",
                FileName = $"pattern-{index:D3}.osu",
            });

        ((IShellProjectFeature<PatternGalleryProject>)viewModel).Install(project);
        return new PatternGalleryViewTestHarness(viewModel, gallery, () => renamedFolderName);
    }

    internal sealed class RecordingPatternGalleryService : IPatternGalleryService
    {
        internal int ExportCount { get; private set; }

        internal IReadOnlyList<PatternGalleryPattern> ExportedPatterns { get; private set; } = [];

        internal string? TargetPath { get; private set; }

        internal bool QuickRun { get; private set; }

        public Task<Beatmap> LoadBeatmapAsync(
            PatternGalleryPattern pattern,
            PatternGalleryCollectionPaths projectPaths,
            CancellationToken cancellationToken = default)
        {
            return Task.FromException<Beatmap>(new InvalidDataException("No thumbnail file is available in this test."));
        }

        public Task<PatternGalleryPattern> ImportCodeAsync(
            string name,
            string hitObjectText,
            string timingPointText,
            double globalSv,
            GameMode gameMode,
            PatternGalleryServiceOptions project,
            PatternGalleryCollectionPaths projectPaths,
            CancellationToken cancellationToken = default,
            PatternGalleryFileEdit? fileEdit = null)
        {
            return Task.FromException<PatternGalleryPattern>(new NotSupportedException());
        }

        public Task<PatternGalleryPattern> ImportFileAsync(
            string sourcePath,
            string name,
            string? filter,
            double startTime,
            double endTime,
            PatternGalleryCollectionPaths projectPaths,
            CancellationToken cancellationToken = default,
            PatternGalleryFileEdit? fileEdit = null)
        {
            return Task.FromException<PatternGalleryPattern>(new NotSupportedException());
        }

        public Task<PatternGalleryPattern> ImportSelectedAsync(
            string sourcePath,
            string name,
            PatternGalleryCollectionPaths projectPaths,
            CancellationToken cancellationToken = default,
            PatternGalleryFileEdit? fileEdit = null)
        {
            return Task.FromException<PatternGalleryPattern>(new NotSupportedException());
        }

        public Task<PatternGalleryRunResult> ExportAsync(
            string targetPath,
            IReadOnlyList<PatternGalleryPattern> patterns,
            PatternGalleryServiceOptions project,
            PatternGalleryCollectionPaths projectPaths,
            bool quickRun,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            ExportCount++;
            ExportedPatterns = patterns.ToArray();
            TargetPath = targetPath;
            QuickRun = quickRun;
            return Task.FromResult(new PatternGalleryRunResult(patterns.Count, "Exported."));
        }

        public Task DeleteAsync(
            IReadOnlyList<PatternGalleryPattern> patterns,
            PatternGalleryCollectionPaths projectPaths,
            CancellationToken cancellationToken = default,
            PatternGalleryFileEdit? fileEdit = null)
        {
            return Task.CompletedTask;
        }

        public void MergeCollection(
            PatternGalleryServiceOptions project,
            PatternGalleryServiceOptions imported,
            IReadOnlyList<PatternGalleryArchiveFile> patternFiles,
            PatternGalleryCollectionPaths projectPaths,
            PatternGalleryFileEdit? fileEdit = null)
        {
        }

        public Task<PatternGalleryRestoreResult> RestoreAsync(
            PatternGalleryServiceOptions project,
            PatternGalleryCollectionPaths projectPaths,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new PatternGalleryRestoreResult(0, 0));
        }
    }
}

internal class InterfaceProxy<T> : DispatchProxy where T : class
{
    private Func<MethodInfo, object?[]?, object?> handler = (_, _) => null;

    internal static T Create(Func<MethodInfo, object?[]?, object?> handler)
    {
        T instance = Create<T, InterfaceProxy<T>>();
        ((InterfaceProxy<T>)(object)instance).handler = handler;
        return instance;
    }

    internal static object DefaultResult(MethodInfo method)
    {
        throw new NotSupportedException($"The test proxy does not support {typeof(T).Name}.{method.Name}.");
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        return handler(targetMethod ?? throw new InvalidOperationException("A proxy invocation did not include its method."), args);
    }
}
