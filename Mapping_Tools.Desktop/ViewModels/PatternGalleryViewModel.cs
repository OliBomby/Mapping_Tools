using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Material.Icons;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Interactions.Converters;
using Mapping_Tools.Application.PatternGallery;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.Classes.BeatmapHelper;
using Mapping_Tools.Core.Classes.BeatmapHelper.BeatDivisors;
using Mapping_Tools.Core.Tools.PatternGallery;
using Mapping_Tools.Desktop.Interactions;
using Mapping_Tools.Desktop.Shell;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
/// Owns Pattern Gallery collection state, thumbnail loading, typed imports,
/// ZIP persistence, placement options, project recovery, and QuickRun.
/// </summary>
public sealed partial class PatternGalleryViewModel : SingleRunToolViewModel,
    IShellProjectFeature,
    IShellExtraProjectMenuFeature,
    IShellFeatureActivation,
    IQuickRun
{
    internal const string OperationId = "pattern-gallery";

    private readonly IPatternGalleryService _gallery;
    private readonly IPatternGalleryFileService _files;
    private readonly IPatternGalleryArchiveService _archives;
    private readonly IBeatmapWorkspace _workspace;
    private readonly ICurrentBeatmapLocator _currentBeatmap;
    private readonly IFilePicker _filePicker;
    private readonly IFileRevealService _reveal;
    private readonly IProjectService _projects;
    private readonly IProjectSerializer _serializer;
    private readonly IApplicationDirectories _directories;
    private readonly IDialogService _dialogs;
    private readonly ApplicationSettings _settings;
    private readonly IPatternGalleryInputDialog _inputDialog;
    private readonly ProjectDefinition<PatternGalleryProject> _definition = new(
        "patterngalleryproject.json",
        "Pattern Gallery Projects",
        () => new PatternGalleryProject(),
        "pattern-gallery-project.json");
    private readonly Dictionary<PatternGalleryPattern, PatternGalleryItemViewModel> _items = [];
    private PatternGalleryCollectionPaths? _paths;
    private CancellationTokenSource? _thumbnailCancellation;

    private IEnumerable<PatternGalleryPattern> SelectedPatterns =>
        _items.Values
            .Where(item => item.IsSelected)
            .Select(item => item.Pattern);

    /// <summary>Gets or sets the editable project model.</summary>
    [ObservableProperty]
    public partial PatternGalleryProject Project { get; set; } = new();

    /// <summary>Gets or sets the user-visible collection name.</summary>
    [ObservableProperty]
    public partial string CollectionName { get; set; } = "My Pattern Collection";

    /// <summary>Gets the visible pattern groups after filtering and sorting.</summary>
    [ObservableProperty]
    public partial IReadOnlyList<PatternGalleryGroupViewModel> Groups { get; private set; } = [];

    /// <summary>Gets or sets the case-insensitive name filter.</summary>
    [ObservableProperty]
    public partial string SearchFilter { get; set; } = string.Empty;

    /// <summary>Gets or sets the property used to order patterns.</summary>
    [ObservableProperty]
    public partial string SortProperty { get; set; } = "Creation time";

    /// <summary>Gets or sets the sort direction, where zero is ascending.</summary>
    [ObservableProperty]
    public partial int SortDirection { get; set; }

    /// <summary>Gets the sort properties preserved from the WPF gallery.</summary>
    public IReadOnlyList<string> SortableProperties { get; } =
        ["Name", "Creation time", "Last used time", "Usage count", "Object count", "Duration", "Beat length"];

    /// <summary>Gets the available export-time modes.</summary>
    public IReadOnlyList<ExportTimeMode> ExportTimeModes { get; } = Enum.GetValues<ExportTimeMode>();

    /// <summary>Gets the available pattern overwrite modes.</summary>
    public IReadOnlyList<PatternOverwriteMode> PatternOverwriteModes { get; } = Enum.GetValues<PatternOverwriteMode>();

    /// <summary>Gets the available timing overwrite modes.</summary>
    public IReadOnlyList<TimingOverwriteMode> TimingOverwriteModes { get; } = Enum.GetValues<TimingOverwriteMode>();

    /// <summary>Gets or sets the export-time mode.</summary>
    [ObservableProperty]
    public partial ExportTimeMode ExportTimeMode { get; set; } = ExportTimeMode.Current;

    /// <summary>Gets or sets the custom export time in milliseconds.</summary>
    [ObservableProperty]
    public partial double CustomExportTime { get; set; }

    /// <summary>Gets whether the custom-time field should be shown.</summary>
    public bool CustomExportTimeVisible => ExportTimeMode == ExportTimeMode.Custom;

    /// <summary>Gets or sets the extraction and overwrite padding in milliseconds.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, double.MaxValue, ErrorMessage = "Padding must be zero or greater.")]
    public partial double Padding { get; set; } = 5;

    /// <summary>Gets or sets the minimum partition gap in beats.</summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Range(0, double.MaxValue, ErrorMessage = "Parting distance must be zero or greater.")]
    public partial double PartingDistance { get; set; } = 4;

    /// <summary>Gets or sets the target-object overwrite mode.</summary>
    [ObservableProperty]
    public partial PatternOverwriteMode PatternOverwriteMode { get; set; } = PatternOverwriteMode.PartitionedOverwrite;

    /// <summary>Gets or sets the timing overwrite mode.</summary>
    [ObservableProperty]
    public partial TimingOverwriteMode TimingOverwriteMode { get; set; } = TimingOverwriteMode.OriginalTimingOnly;

    /// <summary>Gets or sets whether pattern hitsounds are copied.</summary>
    [ObservableProperty]
    public partial bool IncludeHitsounds { get; set; }

    /// <summary>Gets or sets whether pattern kiai state is copied.</summary>
    [ObservableProperty]
    public partial bool IncludeKiai { get; set; }

    /// <summary>Gets or sets whether positions are scaled to target Circle Size.</summary>
    [ObservableProperty]
    public partial bool ScaleToNewCircleSize { get; set; }

    /// <summary>Gets or sets whether pattern timing is scaled to the target.</summary>
    [ObservableProperty]
    public partial bool ScaleToNewTiming { get; set; } = true;

    /// <summary>Gets or sets whether objects are snapped to target timing.</summary>
    [ObservableProperty]
    public partial bool SnapToNewTiming { get; set; } = true;

    /// <summary>Gets or sets the beat divisors used for resnapping.</summary>
    [ObservableProperty]
    public partial IBeatDivisor[] BeatDivisors { get; set; } = RationalBeatDivisor.GetDefaultBeatDivisors();

    /// <summary>Gets or sets whether global slider velocity is compensated.</summary>
    [ObservableProperty]
    public partial bool FixGlobalSv { get; set; } = true;

    /// <summary>Gets or sets whether BPM-dependent slider velocity is compensated.</summary>
    [ObservableProperty]
    public partial bool FixBpmSv { get; set; }

    /// <summary>Gets or sets whether combo-colour skips are repaired.</summary>
    [ObservableProperty]
    public partial bool FixColourHax { get; set; } = true;

    /// <summary>Gets or sets whether stack offsets are made explicit.</summary>
    [ObservableProperty]
    public partial bool FixStackLeniency { get; set; }

    /// <summary>Gets or sets whether slider tick rate is compensated.</summary>
    [ObservableProperty]
    public partial bool FixTickRate { get; set; }

    /// <summary>Gets or sets the optional spatial scale multiplier.</summary>
    [ObservableProperty]
    public partial double CustomScale { get; set; } = 1;

    /// <summary>Gets or sets clockwise spatial rotation in degrees.</summary>
    [ObservableProperty]
    public partial double CustomRotate { get; set; }

    /// <summary>Gets the latest import, placement, or persistence summary.</summary>
    [ObservableProperty]
    public partial string ResultSummary { get; private set; } = string.Empty;

    /// <summary>Creates the Pattern Gallery presentation model.</summary>
    /// <param name="gallery">Runs framework-neutral Pattern Gallery use cases.</param>
    /// <param name="files">Resolves and writes collection files.</param>
    /// <param name="archives">Reads and creates collection ZIP files.</param>
    /// <param name="execution">Coordinates cancellable tool runs.</param>
    /// <param name="workspace">Supplies ordinary-run beatmap selection.</param>
    /// <param name="currentBeatmap">Finds the beatmap open in osu!.</param>
    /// <param name="filePicker">Presents native file and save dialogs.</param>
    /// <param name="reveal">Reveals files in the platform file manager.</param>
    /// <param name="projects">Loads and saves explicit collection JSON.</param>
    /// <param name="serializer">Serializes legacy-compatible project JSON.</param>
    /// <param name="directories">Provides the application-data collection root.</param>
    /// <param name="dialogs">Presents typed confirmations and value fields.</param>
    /// <param name="settings">Provides the shared QuickRun preference.</param>
    /// <param name="inputDialog">Presents Pattern Gallery's multi-field forms.</param>
    public PatternGalleryViewModel(
        IPatternGalleryService gallery,
        IPatternGalleryFileService files,
        IPatternGalleryArchiveService archives,
        IToolExecutionService execution,
        IBeatmapWorkspace workspace,
        ICurrentBeatmapLocator currentBeatmap,
        IFilePicker filePicker,
        IFileRevealService reveal,
        IProjectService projects,
        IProjectSerializer serializer,
        IApplicationDirectories directories,
        IDialogService dialogs,
        ApplicationSettings settings,
        IPatternGalleryInputDialog inputDialog)
        : base(execution, OperationId)
    {
        _gallery = gallery ?? throw new ArgumentNullException(nameof(gallery));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _archives = archives ?? throw new ArgumentNullException(nameof(archives));
        _workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        _currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        _reveal = reveal ?? throw new ArgumentNullException(nameof(reveal));
        _projects = projects ?? throw new ArgumentNullException(nameof(projects));
        _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        _directories = directories ?? throw new ArgumentNullException(nameof(directories));
        _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _inputDialog = inputDialog ?? throw new ArgumentNullException(nameof(inputDialog));
        ConfigureProject();
        RebuildGroups();
    }

    /// <inheritdoc/>
    string IQuickRun.OperationId => OperationId;

    /// <inheritdoc/>
    IProjectDefinition IShellProjectFeature.ProjectDefinition => _definition;

    IReadOnlyList<ShellProjectMenuItem> IShellExtraProjectMenuFeature.ExtraProjectMenuItems =>
    [
        new("_Rename collection", "Rename this collection and the collection's directory in the Pattern Files directory.", RenameCollectionCommand, MaterialIconKind.Edit),
        new("_Import collection", "Import a collection zip file to the projects folder.", ImportCollectionCommand, MaterialIconKind.Import),
        new("_Export collection", "Export this collection to the Exports folder. The exported file can later be imported with the import menu.", ExportCollectionCommand, MaterialIconKind.Export),
        new("_Restore collection", "Restore the collection from the pattern files directory. This will remove any patterns that have missing files, and add any patterns that have not been indexed. Make sure to back-up your collection before restoring it.", RestoreCollectionCommand, MaterialIconKind.Restore)
    ];

    /// <inheritdoc/>
    IReadOnlyList<string> IShellProjectFeature.AdditionalAutoSavePaths =>
        _paths is not null ? [_paths.ProjectFile] : [];

    /// <inheritdoc/>
    object IShellProjectFeature.Snapshot() => Snapshot(includeSelection: false);

    /// <inheritdoc/>
    void IShellProjectFeature.Install(object project)
    {
        if (project is not PatternGalleryProject typed)
        {
            throw new InvalidDataException("Pattern Gallery project is incomplete.");
        }

        CancelThumbnailRefresh();
        Project = typed;
        _items.Clear();
        ConfigureProject();
        RebuildGroups();
        StartThumbnailRefresh();
    }

    /// <inheritdoc/>
    public void Activate() => StartThumbnailRefresh();

    /// <inheritdoc/>
    public void Deactivate()
    {
        CancelThumbnailRefresh();
    }

    /// <summary>Adds a pattern from raw osu! hit-object and timing-point text.</summary>
    [RelayCommand]
    private async Task AddCodeAsync()
    {
        PatternGalleryCodeInput? input = await _inputDialog.ShowCodeAsync($"Pattern {Project.Patterns.Count + 1}");
        if (input is null)
        {
            return;
        }

        try
        {
            PatternGalleryPattern pattern = await _gallery.ImportCodeAsync(
                input.Name,
                input.HitObjects,
                input.TimingPoints,
                input.GlobalSv,
                input.GameMode,
                Project,
                Paths,
                CancellationToken.None);
            Project.Patterns.Add(pattern);
            ResultSummary = $"Imported {pattern.Name}.";
            RebuildGroups();
            StartThumbnailRefresh();
        }
        catch (Exception exception)
        {
            ResultSummary = exception.Message;
        }
    }

    /// <summary>Chooses and imports a pattern beatmap file.</summary>
    [RelayCommand]
    private async Task AddFileAsync()
    {
        IReadOnlyList<string> selected = await _filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Import pattern file",
            AllowMultiple = false,
            Filters = [CommonFilePickerFilters.Beatmaps]
        });
        string? path = selected.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        PatternGalleryFileInput? input = await _inputDialog.ShowFileAsync(
            $"Pattern {Project.Patterns.Count + 1}", path);
        if (input is null)
        {
            return;
        }

        try
        {
            PatternGalleryPattern pattern = await _gallery.ImportFileAsync(
                input.FilePath,
                input.Name,
                input.Filter,
                input.StartTime,
                input.EndTime,
                Paths,
                CancellationToken.None);
            Project.Patterns.Add(pattern);
            ResultSummary = $"Imported {pattern.Name}.";
            RebuildGroups();
            StartThumbnailRefresh();
        }
        catch (Exception exception)
        {
            ResultSummary = exception.Message;
        }
    }

    /// <summary>Imports the currently selected objects from the live editor.</summary>
    [RelayCommand]
    private async Task AddSelectedAsync()
    {
        string? sourcePath = await _currentBeatmap.FindCurrentBeatmapAsync();
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            ResultSummary = "Open a beatmap in osu! before importing selected objects.";
            return;
        }

        ValueDialogResult<string> name = await _dialogs.ShowValueAsync(new ValueDialogRequest<string>(
            "Import selected objects",
            "Pattern name",
            $"Pattern {Project.Patterns.Count + 1}",
            new StringConverter()));
        if (!name.Accepted || string.IsNullOrWhiteSpace(name.Value))
        {
            return;
        }

        try
        {
            PatternGalleryPattern pattern = await _gallery.ImportSelectedAsync(
                sourcePath,
                name.Value,
                Paths,
                CancellationToken.None);
            Project.Patterns.Add(pattern);
            ResultSummary = $"Imported {pattern.Name}.";
            RebuildGroups();
            StartThumbnailRefresh();
        }
        catch (Exception exception)
        {
            ResultSummary = exception.Message;
        }
    }

    /// <summary>Deletes selected patterns after a typed confirmation.</summary>
    [RelayCommand]
    private Task RemoveAsync() => RemoveSelectedAsync(skipConfirmation: false);

    /// <summary>
    /// Deletes selected patterns, optionally honoring the legacy Shift shortcut
    /// that bypasses the confirmation dialog.
    /// </summary>
    /// <param name="skipConfirmation">Whether to omit the confirmation step.</param>
    /// <returns>A task that completes after physical files and metadata are removed.</returns>
    public async Task RemoveSelectedAsync(bool skipConfirmation)
    {
        PatternGalleryPattern[] selected = SelectedPatterns.ToArray();
        if (selected.Length == 0)
        {
            return;
        }

        string message = selected.Length == 1
            ? $"Are you sure you want to delete \"{selected[0].Name}\"?"
            : $"Are you sure you want to delete \"{selected[0].Name}\" and {selected.Length - 1} others?";
        if (!skipConfirmation)
        {
            bool confirmed = await _dialogs.ShowMessageAsync(new MessageDialogRequest<bool>(
                "Confirm deletion",
                message,
                [
                    new DialogChoice<bool>("Yes", true, IsDefault: true),
                    new DialogChoice<bool>("No", false, IsCancel: true)
                ],
                false));
            if (!confirmed)
            {
                return;
            }
        }

        try
        {
            CancelThumbnailRefresh();
            await _gallery.DeleteAsync(selected, Paths);
            foreach (PatternGalleryPattern pattern in selected)
            {
                Project.Patterns.Remove(pattern);
            }

            ResultSummary = $"Deleted {selected.Length} pattern{(selected.Length == 1 ? string.Empty : "s")}.";
            RebuildGroups();
        }
        catch (Exception exception)
        {
            ResultSummary = exception.Message;
        }
    }

    /// <summary>Reveals the selected pattern files in the file manager.</summary>
    [RelayCommand]
    private async Task OpenExplorerSelectedAsync()
    {
        foreach (PatternGalleryPattern pattern in SelectedPatterns)
        {
            await _reveal.RevealAsync(_files.GetPatternPath(Paths, pattern.FileName));
        }
    }

    /// <summary>Displays and optionally renames the first selected pattern.</summary>
    [RelayCommand]
    private async Task ShowDetailsAsync()
    {
        PatternGalleryPattern? pattern = SelectedPatterns.FirstOrDefault();
        if (pattern is null)
        {
            return;
        }

        string? name = await _inputDialog.ShowDetailsAsync(pattern);
        if (!string.IsNullOrWhiteSpace(name))
        {
            pattern.Name = name;
            _items.GetValueOrDefault(pattern)?.RefreshMetadata();
            RebuildGroups();
        }
    }

    /// <summary>Assigns selected patterns to an existing or empty group.</summary>
    /// <param name="group">The persisted group name; null and empty mean None.</param>
    [RelayCommand]
    public void AssignGroup(string? group)
    {
        foreach (PatternGalleryPattern pattern in SelectedPatterns)
        {
            pattern.Group = group ?? string.Empty;
        }

        RebuildGroups();
    }

    /// <summary>Prompts for a new group name and assigns selected patterns to it.</summary>
    [RelayCommand]
    private async Task NewGroupAsync()
    {
        ValueDialogResult<string> result = await _dialogs.ShowValueAsync(new ValueDialogRequest<string>(
            "New pattern group",
            "Group name",
            $"Group {Project.Patterns.Select(pattern => pattern.Group).Distinct().Count()}",
            new StringConverter()));
        if (result.Accepted && !string.IsNullOrWhiteSpace(result.Value))
        {
            AssignGroup(result.Value);
        }
    }

    /// <summary>Renames the group containing the first selected pattern.</summary>
    [RelayCommand]
    private async Task RenameGroupAsync()
    {
        PatternGalleryPattern? selected = SelectedPatterns.FirstOrDefault();
        if (selected is null)
        {
            return;
        }

        string currentGroup = selected.Group;
        ValueDialogResult<string> result = await _dialogs.ShowValueAsync(new ValueDialogRequest<string>(
            "Rename pattern group",
            "Group name",
            string.IsNullOrWhiteSpace(currentGroup) ? "None" : currentGroup,
            new StringConverter()));
        if (!result.Accepted || string.IsNullOrWhiteSpace(result.Value))
        {
            return;
        }

        foreach (PatternGalleryPattern pattern in Project.Patterns.Where(item => item.Group == currentGroup))
        {
            pattern.Group = result.Value;
        }

        RebuildGroups();
    }

    /// <summary>Renames the collection's display and physical folder names.</summary>
    [RelayCommand]
    private async Task RenameCollectionAsync()
    {
        ValueDialogResult<string> display = await _dialogs.ShowValueAsync(new ValueDialogRequest<string>(
            "Rename collection",
            "Collection name",
            CollectionName,
            new StringConverter()));
        if (!display.Accepted || string.IsNullOrWhiteSpace(display.Value))
        {
            return;
        }

        ValueDialogResult<string> folder = await _dialogs.ShowValueAsync(new ValueDialogRequest<string>(
            "Rename collection folder",
            "Folder name",
            Project.FileHandler.CollectionFolderName,
            new StringConverter()));
        if (!folder.Accepted || string.IsNullOrWhiteSpace(folder.Value))
        {
            return;
        }

        try
        {
            if (!string.Equals(folder.Value, Project.FileHandler.CollectionFolderName, StringComparison.Ordinal))
            {
                CancelThumbnailRefresh();
                _paths = _files.RenameCollection(Paths, folder.Value);
                Project.FileHandler.CollectionFolderName = folder.Value;
                Project.FileHandler.BasePath = CollectionBasePath;
            }

            CollectionName = display.Value;
            Project.CollectionName = display.Value;
            ResultSummary = "Renamed collection.";
        }
        catch (Exception exception)
        {
            ResultSummary = exception.Message;
        }
    }

    /// <summary>Exports the current collection as a compatible ZIP archive.</summary>
    [RelayCommand]
    private async Task ExportCollectionAsync()
    {
        string archivePath = Path.Combine(_directories.Exports, CollectionName + ".zip");

        try
        {
            PatternGalleryProject snapshot = Snapshot(includeSelection: false);
            List<PatternGalleryArchiveFile> files = snapshot.Patterns
                .Select(pattern => new PatternGalleryArchiveFile(
                    pattern.FileName,
                    _files.ReadPatternBytes(_files.GetPatternPath(Paths, pattern.FileName))))
                .ToList();
            await _archives.ExportAsync(
                archivePath,
                snapshot.FileHandler.CollectionFolderName,
                CollectionName + ".json",
                _serializer.Serialize(snapshot),
                files);
            await _reveal.RevealAsync(archivePath);
            ResultSummary = "Exported Pattern Gallery collection.";
        }
        catch (Exception exception)
        {
            ResultSummary = exception.Message;
        }
    }

    /// <summary>Imports a Pattern Gallery ZIP as a new or merged collection.</summary>
    [RelayCommand]
    private async Task ImportCollectionAsync()
    {
        IReadOnlyList<string> selected = await _filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Import Pattern Gallery collection",
            AllowMultiple = false,
            Filters = [new FilePickerFilter("ZIP archive", ["*.zip"], ["application/zip"])]
        });
        string? archivePath = selected.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(archivePath))
        {
            return;
        }

        try
        {
            PatternGalleryArchive archive = await _archives.ReadAsync(archivePath);
            bool merge = await _dialogs.ShowMessageAsync(new MessageDialogRequest<bool>(
                "Import Pattern Gallery collection",
                "Merge the imported patterns into the current collection?",
                [
                    new DialogChoice<bool>("Merge", true, IsDefault: true),
                    new DialogChoice<bool>("New collection", false, IsCancel: true)
                ],
                false));

            PatternGalleryProject imported = _serializer.Deserialize<PatternGalleryProject>(archive.ProjectJson);
            imported.FileHandler.CollectionFolderName = archive.CollectionFolderName;
            if (merge)
            {
                CancelThumbnailRefresh();
                _files.EnsureCollection(Paths);
                Dictionary<string, PatternGalleryArchiveFile> contents = archive.PatternFiles
                    .ToDictionary(file => file.FileName, StringComparer.OrdinalIgnoreCase);
                foreach (PatternGalleryPattern pattern in imported.Patterns)
                {
                    if (contents.TryGetValue(pattern.FileName, out PatternGalleryArchiveFile? file))
                    {
                        _files.WritePatternBytes(_files.GetPatternPath(Paths, pattern.FileName), file.Content);
                        Project.Patterns.Add(pattern);
                    }
                }

                RebuildGroups();
                StartThumbnailRefresh();
                ResultSummary = "Merged Pattern Gallery collection.";
                return;
            }

            PatternGalleryCollectionPaths importedPaths = _files.Resolve(CollectionBasePath, imported.FileHandler);
            if (Directory.Exists(importedPaths.Collection))
            {
                throw new IOException($"Collection folder '{imported.FileHandler.CollectionFolderName}' already exists.");
            }

            CancelThumbnailRefresh();
            await _archives.ExtractAsync(archivePath, CollectionBasePath);
            bool load = await _dialogs.ShowMessageAsync(new MessageDialogRequest<bool>(
                "Load imported collection",
                $"Load '{imported.CollectionName}' as the active Pattern Gallery collection?",
                [
                    new DialogChoice<bool>("Load", true, IsDefault: true),
                    new DialogChoice<bool>("Keep current", false, IsCancel: true)
                ],
                false));
            if (load)
            {
                if (Project.Patterns.Count > 0)
                {
                    await _projects.SaveAsync(Paths.ProjectFile, Snapshot(includeSelection: false));
                }

                ((IShellProjectFeature)this).Install(imported);
            }

            ResultSummary = "Imported Pattern Gallery collection.";
        }
        catch (Exception exception)
        {
            ResultSummary = exception.Message;
        }
    }

    /// <summary>Reconciles indexed metadata with physical collection files.</summary>
    [RelayCommand]
    private async Task RestoreCollectionAsync()
    {
        bool confirmed = await _dialogs.ShowMessageAsync(new MessageDialogRequest<bool>(
            "Restore Pattern Gallery collection",
            "Remove missing patterns and add pattern files that are not indexed?",
            [
                new DialogChoice<bool>("Restore", true, IsDefault: true),
                new DialogChoice<bool>("Cancel", false, IsCancel: true)
            ],
            false));
        if (!confirmed)
        {
            return;
        }

        try
        {
            CancelThumbnailRefresh();
            PatternGalleryRestoreResult result = await _gallery.RestoreAsync(Project, Paths);
            RebuildGroups();
            StartThumbnailRefresh();
            ResultSummary = $"Restored collection: removed {result.RemovedCount}, added {result.AddedCount}.";
        }
        catch (Exception exception)
        {
            ResultSummary = exception.Message;
        }
    }

    /// <summary>Selects only the supplied gallery item, matching legacy card clicks.</summary>
    /// <param name="item">The item to select.</param>
    public void SelectOnly(PatternGalleryItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);
        foreach (PatternGalleryItemViewModel galleryItem in _items.Values)
        {
            galleryItem.IsSelected = ReferenceEquals(galleryItem, item);
        }
    }

    /// <summary>Selects or clears every indexed pattern.</summary>
    /// <param name="select">Whether all patterns should be selected.</param>
    public void SetSelectAll(bool select)
    {
        foreach (PatternGalleryItemViewModel item in _items.Values)
        {
            item.IsSelected = select;
        }
    }

    /// <summary>Selects every indexed pattern.</summary>
    [RelayCommand]
    private void SelectAll() => SetSelectAll(true);

    /// <summary>Clears selection from every indexed pattern.</summary>
    [RelayCommand]
    private void ClearSelection() => SetSelectAll(false);

    /// <summary>Gets the current physical collection paths for view commands.</summary>
    public PatternGalleryCollectionPaths Paths => _paths ?? throw new InvalidOperationException("Pattern Gallery collection paths are not initialized.");

    /// <summary>Gets the group names suitable for a context menu.</summary>
    public IReadOnlyList<string> GroupNames => Project.Patterns
        .Select(pattern => pattern.Group)
        .Where(group => !string.IsNullOrWhiteSpace(group))
        .Distinct(StringComparer.Ordinal)
        .OrderBy(group => group, StringComparer.Ordinal)
        .ToArray();

    /// <inheritdoc/>
    protected override bool PrepareRun()
    {
        ValidateAllProperties();
        if (HasErrors)
        {
            ResultSummary = "Correct the invalid Pattern Gallery options before running.";
            return false;
        }

        if (!SelectedPatterns.Any())
        {
            ResultSummary = "Select at least one pattern before running Pattern Gallery.";
            return false;
        }

        if (ExportTimeMode != ExportTimeMode.Current && _workspace.SelectedPaths.Count == 0)
        {
            ResultSummary = "Select at least one target beatmap before running Pattern Gallery.";
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    protected override async Task RunCoreAsync()
    {
        if (ExportTimeMode == ExportTimeMode.Current)
        {
            string? current = await _currentBeatmap.FindCurrentBeatmapAsync();
            await RunPathsAsync(
                string.IsNullOrWhiteSpace(current) ? [] : [current],
                quick: _settings.AlwaysQuickRun,
                CancellationToken.None);
            return;
        }

        await RunPathsAsync(_workspace.SelectedPaths, quick: _settings.AlwaysQuickRun, CancellationToken.None);
    }

    /// <inheritdoc/>
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? current = await _currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        await RunWithStateAsync(() => RunPathsAsync(
            string.IsNullOrWhiteSpace(current) ? [] : [current],
            quick: true,
            cancellationToken));
    }

    private async Task RunPathsAsync(
        IReadOnlyList<string> targetPaths,
        bool quick,
        CancellationToken cancellationToken)
    {
        if (targetPaths.Count == 0)
        {
            ResultSummary = "Open or select a target beatmap before running Pattern Gallery.";
            return;
        }

        PatternGalleryProject project = Snapshot(includeSelection: false);
        PatternGalleryPattern[] patterns = SelectedPatterns.ToArray();
        ToolExecutionResult<PatternGalleryRunResult> execution = await Execution.ExecuteAsync(
            new ToolExecutionRequest<PatternGalleryRunResult>(
                OperationId,
                "Pattern Gallery",
                async context =>
                {
                    PatternGalleryRunResult result = await _gallery.ExportAsync(
                        targetPaths[0],
                        patterns,
                        project,
                        Paths,
                        quick,
                        new Progress<double>(value => context.ReportProgress(value, "Exporting patterns")),
                        context.CancellationToken);
                    return new ToolExecutionOutput<PatternGalleryRunResult>(
                        result,
                        quick ? null : result.Message,
                        reloadEditor: quick);
                }),
            CreateProgress(),
            cancellationToken);

        if (execution.Status == ToolExecutionStatus.Succeeded && execution.Value is not null)
        {
            DateTime usedAt = DateTime.Now;
            foreach (PatternGalleryPattern pattern in SelectedPatterns)
            {
                pattern.UseCount++;
                pattern.LastUsedTime = usedAt;
            }

            ResultSummary = execution.Value.Message;
        }
        else if (execution.Status == ToolExecutionStatus.Failed)
        {
            ResultSummary = execution.Exception?.Message ?? "Pattern Gallery export failed.";
        }
    }

    private PatternGalleryProject Snapshot(bool includeSelection)
    {
        _ = includeSelection;
        PatternGalleryProject snapshot = new()
        {
            CollectionName = CollectionName,
            FileHandler = new PatternGalleryCollectionMetadata
            {
                BasePath = CollectionBasePath,
                PatternFilesFolderName = Project.FileHandler.PatternFilesFolderName,
                CollectionFolderName = Project.FileHandler.CollectionFolderName
            },
            ExportTimeMode = ExportTimeMode,
            CustomExportTime = CustomExportTime,
            Padding = Padding,
            PartingDistance = PartingDistance,
            PatternOverwriteMode = PatternOverwriteMode,
            TimingOverwriteMode = TimingOverwriteMode,
            IncludeHitsounds = IncludeHitsounds,
            IncludeKiai = IncludeKiai,
            ScaleToNewCircleSize = ScaleToNewCircleSize,
            ScaleToNewTiming = ScaleToNewTiming,
            SnapToNewTiming = SnapToNewTiming,
            BeatDivisors = BeatDivisors.ToArray(),
            FixGlobalSv = FixGlobalSv,
            FixBpmSv = FixBpmSv,
            FixColourHax = FixColourHax,
            FixStackLeniency = FixStackLeniency,
            FixTickRate = FixTickRate,
            CustomScale = CustomScale,
            CustomRotate = CustomRotate
        };

        foreach (PatternGalleryPattern pattern in Project.Patterns)
        {
            snapshot.Patterns.Add(new PatternGalleryPattern
            {
                Name = pattern.Name,
                Group = pattern.Group,
                CreationTime = pattern.CreationTime,
                LastUsedTime = pattern.LastUsedTime,
                UseCount = pattern.UseCount,
                FileName = pattern.FileName,
                ObjectCount = pattern.ObjectCount,
                Duration = pattern.Duration,
                BeatLength = pattern.BeatLength
            });
        }

        return snapshot;
    }

    private void ConfigureProject()
    {
        Project.Patterns ??= [];
        Project.FileHandler ??= new PatternGalleryCollectionMetadata();
        Project.FileHandler.BasePath = CollectionBasePath;
        CollectionName = string.IsNullOrWhiteSpace(Project.CollectionName)
            ? "My Pattern Collection"
            : Project.CollectionName;
        ExportTimeMode = Project.ExportTimeMode;
        CustomExportTime = Project.CustomExportTime;
        Padding = Project.Padding;
        PartingDistance = Project.PartingDistance;
        PatternOverwriteMode = Project.PatternOverwriteMode;
        TimingOverwriteMode = Project.TimingOverwriteMode;
        IncludeHitsounds = Project.IncludeHitsounds;
        IncludeKiai = Project.IncludeKiai;
        ScaleToNewCircleSize = Project.ScaleToNewCircleSize;
        ScaleToNewTiming = Project.ScaleToNewTiming;
        SnapToNewTiming = Project.SnapToNewTiming;
        BeatDivisors = Project.BeatDivisors?.ToArray() ?? RationalBeatDivisor.GetDefaultBeatDivisors();
        FixGlobalSv = Project.FixGlobalSv;
        FixBpmSv = Project.FixBpmSv;
        FixColourHax = Project.FixColourHax;
        FixStackLeniency = Project.FixStackLeniency;
        FixTickRate = Project.FixTickRate;
        CustomScale = Project.CustomScale;
        CustomRotate = Project.CustomRotate;
        _paths = _files.Resolve(CollectionBasePath, Project.FileHandler);
        _files.EnsureCollection(_paths);
        OnPropertyChanged(nameof(CustomExportTimeVisible));
    }

    private void RebuildGroups()
    {
        IEnumerable<PatternGalleryPattern> visible = Project.Patterns.Where(pattern =>
            string.IsNullOrWhiteSpace(SearchFilter) ||
            pattern.Name.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase));
        visible = SortPatterns(visible);
        Groups = visible
            .GroupBy(pattern => pattern.Group ?? string.Empty, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new PatternGalleryGroupViewModel(
                string.IsNullOrWhiteSpace(group.Key) ? "None" : group.Key,
                group.Select(pattern => GetItem(pattern))))
            .ToArray();
        OnPropertyChanged(nameof(GroupNames));
    }

    private IEnumerable<PatternGalleryPattern> SortPatterns(IEnumerable<PatternGalleryPattern> patterns)
    {
        IOrderedEnumerable<PatternGalleryPattern> ordered = SortProperty switch
        {
            "Name" => patterns.OrderBy(pattern => pattern.Name, StringComparer.Ordinal),
            "Last used time" => patterns.OrderBy(pattern => pattern.LastUsedTime),
            "Usage count" => patterns.OrderBy(pattern => pattern.UseCount),
            "Object count" => patterns.OrderBy(pattern => pattern.ObjectCount),
            "Duration" => patterns.OrderBy(pattern => pattern.Duration),
            "Beat length" => patterns.OrderBy(pattern => pattern.BeatLength),
            _ => patterns.OrderBy(pattern => pattern.CreationTime)
        };
        return SortDirection == 0 ? ordered : ordered.Reverse();
    }

    private PatternGalleryItemViewModel GetItem(PatternGalleryPattern pattern)
    {
        if (!_items.TryGetValue(pattern, out PatternGalleryItemViewModel? item))
        {
            item = new PatternGalleryItemViewModel(pattern);
            _items.Add(pattern, item);
        }

        return item;
    }

    private void StartThumbnailRefresh()
    {
        CancelThumbnailRefresh();
        _thumbnailCancellation = new CancellationTokenSource();
        _ = RefreshThumbnailsAsync(_thumbnailCancellation.Token);
    }

    private void CancelThumbnailRefresh()
    {
        _thumbnailCancellation?.Cancel();
        _thumbnailCancellation?.Dispose();
        _thumbnailCancellation = null;
    }

    private async Task RefreshThumbnailsAsync(CancellationToken cancellationToken)
    {
        PatternGalleryProject project = Project;
        PatternGalleryCollectionPaths paths = Paths;
        foreach (PatternGalleryPattern pattern in project.Patterns.ToArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            PatternGalleryItemViewModel item = GetItem(pattern);
            try
            {
                Beatmap? beatmap = await _gallery.LoadBeatmapAsync(pattern, paths, cancellationToken);
                if (cancellationToken.IsCancellationRequested ||
                    !ReferenceEquals(Project, project) ||
                    !project.Patterns.Contains(pattern))
                {
                    return;
                }

                item.SetThumbnail(beatmap);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                if (cancellationToken.IsCancellationRequested ||
                    !ReferenceEquals(Project, project) ||
                    !project.Patterns.Contains(pattern))
                {
                    return;
                }

                item.SetThumbnail(null);
            }
        }
    }

    private string CollectionBasePath => Path.Combine(_directories.ApplicationData, "Pattern Gallery Projects");

    partial void OnSearchFilterChanged(string value) => RebuildGroups();

    partial void OnSortPropertyChanged(string value) => RebuildGroups();

    partial void OnSortDirectionChanged(int value) => RebuildGroups();

    partial void OnExportTimeModeChanged(ExportTimeMode value) => OnPropertyChanged(nameof(CustomExportTimeVisible));
}
