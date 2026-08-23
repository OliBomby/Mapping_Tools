using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Audio;
using Mapping_Tools.Application.Execution;
using Mapping_Tools.Application.HitsoundStudio;
using Mapping_Tools.Application.Interactions;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Projects;
using Mapping_Tools.Application.Settings;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Workspace;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels.Adapters;
using Material.Icons;

namespace Mapping_Tools.Desktop.ViewModels;

/// <summary>
///     Owns Hitsound Studio's editable layer list, project snapshot, source
///     interactions, preview lifetime, and export execution state.
/// </summary>
public sealed partial class HitsoundStudioViewModel : SingleRunToolViewModel,
    IQuickRun,
    IShellProjectFeature,
    IShellExtraProjectMenuFeature,
    IAsyncDisposable,
    IDisposable
{
    private readonly ICurrentBeatmapLocator currentBeatmap;
    private readonly ProjectDefinition<HitsoundStudioProject> definition;
    private readonly IHitsoundStudioDialogService dialogs;
    private readonly IFilePicker filePicker;
    private readonly IHitsoundStudioFileSystem files;
    private readonly IDialogService messageDialogs;
    private readonly IProjectStore projectStore;

    private readonly IHitsoundStudioService service;
    private readonly ApplicationSettings settings;
    private readonly IBeatmapWorkspace workspace;
    private IAudioPlaybackSession? previewSession;
    private bool syncingEditor;

    /// <summary>
    ///     Creates the Hitsound Studio presentation model and binds its persisted
    ///     state to the legacy autosave filename.
    /// </summary>
    /// <param name="service">Runs imports, preview, validation, and export.</param>
    /// <param name="dialogs">Shows feature-specific Avalonia forms.</param>
    /// <param name="messageDialogs">Shows typed confirmations and diagnostics.</param>
    /// <param name="execution">Coordinates keyed cancellation and completion.</param>
    /// <param name="currentBeatmap">Finds the beatmap open in osu!.</param>
    /// <param name="workspace">Provides ordinary selected beatmaps.</param>
    /// <param name="filePicker">Presents source and folder pickers.</param>
    /// <param name="files">Checks the export directory before the legacy create prompt.</param>
    /// <param name="projectStore">Loads standalone sample schemas.</param>
    /// <param name="settings">Supplies QuickRun preferences.</param>
    /// <param name="directories">Provides the default application export directory.</param>
    public HitsoundStudioViewModel(
        IHitsoundStudioService service,
        IHitsoundStudioDialogService dialogs,
        IDialogService messageDialogs,
        IToolExecutionService execution,
        ICurrentBeatmapLocator currentBeatmap,
        IBeatmapWorkspace workspace,
        IFilePicker filePicker,
        IHitsoundStudioFileSystem files,
        IProjectStore projectStore,
        ApplicationSettings settings,
        IApplicationDirectories directories)
        : base(execution, MappingToolDefinitions.HitsoundStudio)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
        this.dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
        this.messageDialogs = messageDialogs ?? throw new ArgumentNullException(nameof(messageDialogs));
        this.currentBeatmap = currentBeatmap ?? throw new ArgumentNullException(nameof(currentBeatmap));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        this.files = files ?? throw new ArgumentNullException(nameof(files));
        this.projectStore = projectStore ?? throw new ArgumentNullException(nameof(projectStore));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        directories = directories ?? throw new ArgumentNullException(nameof(directories));
        ExportFolder = directories.Exports;
        definition = new ProjectDefinition<HitsoundStudioProject>(
            "hsstudioproject.json",
            "Hitsound Studio Projects",
            () => new HitsoundStudioProject { ExportFolder = directories.Exports });
    }

    /// <summary>Gets or sets the beatmap used as the export baseline.</summary>
    [ObservableProperty]
    public partial string BaseBeatmap { get; set; } = string.Empty;

    /// <summary>Gets or sets the default normal sample.</summary>
    [ObservableProperty]
    public partial ObservableSample DefaultSample { get; set; } =
        new(new Sample { Priority = int.MaxValue });

    /// <summary>Gets or sets the default sample volume as a percentage.</summary>
    [ObservableProperty]
    public partial string DefaultSampleVolume { get; set; } = "100";

    /// <summary>Gets or sets the export directory.</summary>
    [ObservableProperty]
    public partial string ExportFolder { get; set; } = string.Empty;

    /// <summary>Gets or sets the editable layer collection.</summary>
    [ObservableProperty]
    public partial ObservableCollection<ObservableHitsoundLayer> Layers { get; set; } = [];

    /// <summary>Gets or sets the currently focused layer after a list selection.</summary>
    [ObservableProperty]
    public partial ObservableHitsoundLayer? SelectedLayer { get; set; }

    /// <summary>Gets or sets the invariant comma-separated timestamps of the focused layer.</summary>
    [ObservableProperty]
    public partial string EditTimes { get; set; } = string.Empty;

    /// <summary>Gets or sets whether the potentially long timestamp list is shown.</summary>
    [ObservableProperty]
    public partial bool ShowTimes { get; set; }

    /// <summary>Gets or sets the selected layers' shared display name.</summary>
    [ObservableProperty]
    public partial string EditName { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected layers' shared sample family.</summary>
    [ObservableProperty]
    public partial SampleSet EditSampleSet { get; set; } = SampleSet.Normal;

    /// <summary>Gets or sets the selected layers' shared hitsound.</summary>
    [ObservableProperty]
    public partial Hitsound EditHitsound { get; set; } = Hitsound.Normal;

    /// <summary>Gets or sets the selected layers' shared source path.</summary>
    [ObservableProperty]
    public partial string EditSamplePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected source volume as a percentage.</summary>
    [ObservableProperty]
    public partial string EditSampleVolume { get; set; } = "100";

    /// <summary>Gets or sets the selected source panning.</summary>
    [ObservableProperty]
    public partial string EditSamplePanning { get; set; } = "0";

    /// <summary>Gets or sets the selected source pitch shift.</summary>
    [ObservableProperty]
    public partial string EditSamplePitchShift { get; set; } = "0";

    /// <summary>Gets or sets the selected SoundFont bank.</summary>
    [ObservableProperty]
    public partial string EditSampleBank { get; set; } = "-1";

    /// <summary>Gets or sets the selected SoundFont patch.</summary>
    [ObservableProperty]
    public partial string EditSamplePatch { get; set; } = "-1";

    /// <summary>Gets or sets the selected SoundFont instrument.</summary>
    [ObservableProperty]
    public partial string EditSampleInstrument { get; set; } = "-1";

    /// <summary>Gets or sets the selected MIDI key.</summary>
    [ObservableProperty]
    public partial string EditSampleKey { get; set; } = "-1";

    /// <summary>Gets or sets the selected SoundFont note length.</summary>
    [ObservableProperty]
    public partial string EditSampleLength { get; set; } = "-1";

    /// <summary>Gets or sets the selected MIDI velocity.</summary>
    [ObservableProperty]
    public partial string EditSampleVelocity { get; set; } = "127";

    /// <summary>Gets or sets the selected import kind.</summary>
    [ObservableProperty]
    public partial ImportType EditImportType { get; set; } = ImportType.None;

    /// <summary>Gets or sets the selected import source path.</summary>
    [ObservableProperty]
    public partial string EditImportPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the selected stack X coordinate.</summary>
    [ObservableProperty]
    public partial string EditImportX { get; set; } = "-1";

    /// <summary>Gets or sets the selected stack Y coordinate.</summary>
    [ObservableProperty]
    public partial string EditImportY { get; set; } = "-1";

    /// <summary>Gets or sets the selected imported sample path.</summary>
    [ObservableProperty]
    public partial string EditImportSamplePath { get; set; } = string.Empty;

    /// <summary>Gets or sets whether imported volume creates distinct layers.</summary>
    [ObservableProperty]
    public partial bool EditImportDiscriminateVolumes { get; set; }

    /// <summary>Gets or sets whether duplicate sample files are canonicalized.</summary>
    [ObservableProperty]
    public partial bool EditImportDetectDuplicates { get; set; }

    /// <summary>Gets or sets whether duplicate import times are removed.</summary>
    [ObservableProperty]
    public partial bool EditImportRemoveDuplicates { get; set; }

    /// <summary>Gets or sets the selected SoundFont import bank filter.</summary>
    [ObservableProperty]
    public partial string EditImportBank { get; set; } = "-1";

    /// <summary>Gets or sets the selected SoundFont import patch filter.</summary>
    [ObservableProperty]
    public partial string EditImportPatch { get; set; } = "-1";

    /// <summary>Gets or sets the selected SoundFont import key filter.</summary>
    [ObservableProperty]
    public partial string EditImportKey { get; set; } = "-1";

    /// <summary>Gets or sets the selected MIDI length filter.</summary>
    [ObservableProperty]
    public partial string EditImportLength { get; set; } = "-1";

    /// <summary>Gets or sets the selected MIDI length rounding roughness.</summary>
    [ObservableProperty]
    public partial string EditImportLengthRoughness { get; set; } = "1";

    /// <summary>Gets or sets the selected MIDI velocity filter.</summary>
    [ObservableProperty]
    public partial string EditImportVelocity { get; set; } = "-1";

    /// <summary>Gets or sets the selected MIDI velocity rounding roughness.</summary>
    [ObservableProperty]
    public partial string EditImportVelocityRoughness { get; set; } = "1";

    /// <summary>Gets or sets the selected MIDI start offset.</summary>
    [ObservableProperty]
    public partial string EditImportOffset { get; set; } = "0";

    /// <summary>Gets the layer selection supplied by the Avalonia list.</summary>
    public ObservableCollection<ObservableHitsoundLayer> SelectedLayers { get; } = [];

    /// <summary>Gets whether the layer editor has a selected layer to edit.</summary>
    public bool HasSelectedLayer => SelectedLayer is not null;

    /// <summary>Gets whether the layer editor has any layer to edit.</summary>
    public bool HasLayers => Layers.Count > 0;

    /// <summary>Gets whether the selected layer can be reloaded from a source.</summary>
    public bool HasImport => SelectedLayers.Any(layer => layer.ImportArgs.ImportType != ImportType.None);

    /// <summary>Gets whether stack-specific import fields apply to the selection.</summary>
    public bool IsStackImport => SelectedLayers.Any(layer => layer.ImportArgs.ImportType == ImportType.Stack);

    /// <summary>Gets whether hitsound-import fields apply to the selection.</summary>
    public bool IsHitsoundsImport => SelectedLayers.Any(layer => layer.ImportArgs.ImportType == ImportType.Hitsounds);

    /// <summary>Gets whether storyboard-import fields apply to the selection.</summary>
    public bool IsStoryboardImport => SelectedLayers.Any(layer => layer.ImportArgs.ImportType == ImportType.Storyboard);

    /// <summary>Gets whether imported sample fields apply to the selection.</summary>
    public bool IsSampleImport => IsHitsoundsImport || IsStoryboardImport;

    /// <summary>Gets whether MIDI-import fields apply to the selection.</summary>
    public bool IsMidiImport => SelectedLayers.Any(layer => layer.ImportArgs.ImportType == ImportType.MIDI);

    /// <summary>Gets whether SoundFont fields apply to the selection.</summary>
    public bool IsSoundFontSample => SelectedLayers.Any(layer =>
        layer.SampleArgs.UsesSoundFont || string.IsNullOrEmpty(layer.SampleArgs.GetExtension()));

    /// <summary>Gets or sets a concise status message for validation and completion.</summary>
    [ObservableProperty]
    public partial string ResultSummary { get; private set; } = "Add or import a hitsound layer.";

    /// <summary>Gets every supported layer import kind.</summary>
    public IReadOnlyList<ImportType> ImportTypes { get; } = Enum.GetValues<ImportType>();

    /// <summary>Gets every supported osu! sample family.</summary>
    public IReadOnlyList<SampleSet> SampleSets { get; } =
        Enum.GetValues<SampleSet>().Where(sampleSet => sampleSet != SampleSet.None).ToArray();

    /// <summary>Gets the default-sample choices, including the osu! automatic option.</summary>
    public IReadOnlyList<SampleSet> DefaultSampleSets { get; } = Enum.GetValues<SampleSet>();

    /// <summary>Gets every supported hitsound layer.</summary>
    public IReadOnlyList<Hitsound> Hitsounds { get; } = Enum.GetValues<Hitsound>();

    /// <summary>Gets or sets whether the next export uses the previous schema.</summary>
    [ObservableProperty]
    public partial bool UsePreviousSampleSchema { get; set; }

    /// <summary>Gets or sets the currently loaded schema.</summary>
    [ObservableProperty]
    public partial SampleSchema? PreviousSampleSchema { get; set; }

    /// <summary>Gets or sets whether a previous schema may grow.</summary>
    [ObservableProperty]
    public partial bool AllowGrowthPreviousSampleSchema { get; set; }

    /// <summary>Gets or sets the export mode used by the dialog.</summary>
    [ObservableProperty]
    public partial HitsoundStudioExportMode HitsoundExportModeSetting { get; set; }
        = HitsoundStudioExportMode.Standard;

    /// <summary>Gets or sets the output osu! game mode.</summary>
    [ObservableProperty]
    public partial GameMode HitsoundExportGameMode { get; set; } = GameMode.Standard;

    /// <summary>Gets or sets the map version name.</summary>
    [ObservableProperty]
    public partial string HitsoundDiffName { get; set; } = "Hitsounds";

    /// <summary>Gets or sets layer timestamp grouping leniency.</summary>
    [ObservableProperty]
    public partial double ZipLayersLeniency { get; set; } = 15;

    /// <summary>Gets or sets the first new custom index.</summary>
    [ObservableProperty]
    public partial int FirstCustomIndex { get; set; } = 1;

    /// <summary>Gets or sets whether maps and samples are written.</summary>
    [ObservableProperty]
    public partial bool ExportMap { get; set; } = true;

    /// <summary>Gets or sets whether generated samples are written.</summary>
    [ObservableProperty]
    public partial bool ExportSamples { get; set; } = true;

    /// <summary>Gets or sets whether the detailed result summary is shown.</summary>
    [ObservableProperty]
    public partial bool ShowResults { get; set; }

    /// <summary>Gets or sets whether the output directory is cleared.</summary>
    [ObservableProperty]
    public partial bool DeleteAllInExportFirst { get; set; }

    /// <summary>Gets or sets whether named modes retain regular hitsounds.</summary>
    [ObservableProperty]
    public partial bool AddCoincidingRegularHitsounds { get; set; } = true;

    /// <summary>Gets or sets whether MIDI receives greenline volume events.</summary>
    [ObservableProperty]
    public partial bool AddGreenLineVolumeToMidi { get; set; } = true;

    /// <summary>Gets or sets the single-source format.</summary>
    [ObservableProperty]
    public partial HitsoundStudioSampleExportFormat SingleSampleExportFormat { get; set; }
        = HitsoundStudioSampleExportFormat.Default;

    /// <summary>Gets or sets the mixed-source format.</summary>
    [ObservableProperty]
    public partial HitsoundStudioSampleExportFormat MixedSampleExportFormat { get; set; }
        = HitsoundStudioSampleExportFormat.Default;

    /// <summary>Gets all export modes for a combo box.</summary>
    public IReadOnlyList<HitsoundStudioExportMode> ExportModes { get; } = Enum.GetValues<HitsoundStudioExportMode>();

    /// <summary>Gets all game modes for a combo box.</summary>
    public IReadOnlyList<GameMode> GameModes { get; } = Enum.GetValues<GameMode>();

    /// <summary>Gets all sample export formats for a combo box.</summary>
    public IReadOnlyList<HitsoundStudioSampleExportFormat> SampleExportFormats { get; } =
        Enum.GetValues<HitsoundStudioSampleExportFormat>();

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await StopPreviewAsync().ConfigureAwait(false);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <inheritdoc />
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(path)) BaseBeatmap = path;
        await RunWithStateAsync(() => RunExportAsync([path ?? string.Empty], cancellationToken));
    }

    IReadOnlyList<ShellProjectMenuItem> IShellExtraProjectMenuFeature.ExtraProjectMenuItems =>
    [
        new("_Load sample schema", "Load sample schema from a project file.", LoadSampleSchemaCommand, MaterialIconKind.FileMusic),
        new("_Bulk assign samples",
            "Bulk assign samples to selected hitsound layers. The file name is expected to be in the following shape: [bank]_[patch]_[key]_[length]_[velocity].[extension]. Leave a value empty to imply any value. Example: 0_39__127.wav",
            BulkAssignSamplesCommand, MaterialIconKind.MusicBoxMultiple),
    ];

    IProjectDefinition IShellProjectFeature.ProjectDefinition => definition;

    object IShellProjectFeature.Snapshot()
    {
        return ToProject().Clone();
    }

    void IShellProjectFeature.Install(object project)
    {
        if (project is not HitsoundStudioProject typed) throw new InvalidDataException("Invalid Hitsound Studio project.");
        InstallProject(typed);
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        var paths = workspace.SelectedPaths;
        if (settings.AlwaysQuickRun)
        {
            string? current = await currentBeatmap.FindCurrentBeatmapAsync();
            paths = string.IsNullOrWhiteSpace(current) ? [] : [current];
        }

        await RunExportAsync(paths, CancellationToken.None);
    }

    /// <inheritdoc />
    protected override bool PrepareRun()
    {
        if (Layers.Count == 0)
        {
            ResultSummary = "There are no hitsound layers.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(BaseBeatmap))
        {
            ResultSummary = "Choose a base beatmap before exporting.";
            return false;
        }

        return true;
    }

    /// <summary>Opens the complete layer import form and appends its layers.</summary>
    [RelayCommand]
    private async Task AddAsync()
    {
        try
        {
            var request = await dialogs.ShowImportAsync(
                $"Layer {Layers.Count + 1}");
            if (request is null) return;
            var imported = await service.ImportAsync(request);
            foreach (var layer in imported)
            {
                ObservableHitsoundLayer adapter = new(layer) { Priority = Layers.Count };
                Layers.Add(adapter);
            }

            SetSelection(Layers.Skip(Math.Max(0, Layers.Count - imported.Count)));
            OnPropertyChanged(nameof(HasLayers));
            ResultSummary = $"Imported {imported.Count} layer{(imported.Count == 1 ? string.Empty : "s")}.";
        }
        catch (OperationCanceledException)
        {
            ResultSummary = "Import canceled.";
        }
        catch (Exception exception)
        {
            ResultSummary = $"Import failed: {exception.Message}";
        }
    }

    /// <summary>Removes the selected layers and recalculates their priorities.</summary>
    [RelayCommand]
    private async Task RemoveAsync()
    {
        if (SelectedLayers.Count == 0) return;
        bool confirmed = await messageDialogs.ShowMessageAsync(new MessageDialogRequest<bool>(
            "Confirm deletion",
            SelectedLayers.Count == 1
                ? "Are you sure you want to delete the selected layer?"
                : $"Are you sure you want to delete the {SelectedLayers.Count} selected layers?",
            [
                new DialogChoice<bool>("Yes", true, true),
                new DialogChoice<bool>("No", false, IsCancel: true),
            ],
            false));
        if (!confirmed) return;

        int firstSelectedIndex = SelectedLayers
            .Select(Layers.IndexOf)
            .Where(index => index >= 0)
            .DefaultIfEmpty(0)
            .Min();
        foreach (var layer in SelectedLayers.ToArray()) Layers.Remove(layer);
        RecalculatePriorities();
        SetSelection(Layers.Skip(Math.Max(0, Math.Min(firstSelectedIndex - 1, Layers.Count - 1))).Take(1));
        OnPropertyChanged(nameof(HasLayers));
    }

    /// <summary>Reloads selected layers from their persisted import source.</summary>
    [RelayCommand]
    private async Task ReloadAsync()
    {
        if (SelectedLayers.Count == 0)
        {
            ResultSummary = "Select at least one imported layer.";
            return;
        }

        try
        {
            await service.ReloadAsync(SelectedLayers.Select(layer => layer.Model).ToArray());
            ResultSummary = "Reloaded selected layers.";
        }
        catch (OperationCanceledException)
        {
            ResultSummary = "Reload canceled.";
        }
        catch (Exception exception)
        {
            ResultSummary = $"Reload failed: {exception.Message}";
        }
    }

    /// <summary>Previews the selected layer and disposes the previous session first.</summary>
    [RelayCommand]
    private async Task PreviewAsync()
    {
        if (SelectedLayer is null)
        {
            ResultSummary = "Select a layer to preview.";
            return;
        }

        await StopPreviewAsync();
        try
        {
            previewSession = await service.PreviewAsync(SelectedLayer.SampleArgs.Snapshot());
            ResultSummary = "Playing selected layer.";
        }
        catch (FileNotFoundException)
        {
            ResultSummary = "Could not find the specified sample.";
        }
        catch (DirectoryNotFoundException)
        {
            ResultSummary = "Could not find the specified sample's directory.";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ResultSummary = $"Could not load the specified sample: {exception.Message}";
        }
    }

    /// <summary>Stops and disposes the current audio preview session.</summary>
    [RelayCommand]
    private Task StopPreviewAsync()
    {
        var session = Interlocked.Exchange(ref previewSession, null);
        return session is null ? Task.CompletedTask : session.StopAsync().AsTask();
    }

    /// <summary>Validates every unique layer source through the shared audio generator.</summary>
    [RelayCommand]
    private async Task ValidateSamplesAsync()
    {
        try
        {
            var failures = await service.ValidateSamplesAsync(
                Layers.Select(layer => layer.SampleArgs.Snapshot()).ToArray());
            ResultSummary = failures.Count == 0
                ? "All sample sources are valid."
                : $"{failures.Count} sample source{(failures.Count == 1 ? " is" : "s are")} invalid.";
        }
        catch (OperationCanceledException)
        {
            ResultSummary = "Sample validation canceled.";
        }
        catch (Exception exception)
        {
            ResultSummary = $"Sample validation failed: {exception.Message}";
        }
    }

    /// <summary>Chooses a base beatmap through the standard workspace picker.</summary>
    [RelayCommand]
    private async Task PickBaseBeatmapAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Choose base beatmap",
            AllowMultiple = false,
            Filters = [new FilePickerFilter("osu! beatmaps", [".osu"])],
        });
        if (paths.Count > 0) BaseBeatmap = paths[0];
    }

    /// <summary>Chooses the fallback sample file.</summary>
    [RelayCommand]
    private async Task PickDefaultSampleAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Choose default sample",
            AllowMultiple = false,
            Filters = [new FilePickerFilter("Audio and SoundFont files", [".wav", ".ogg", ".mp3", ".sf2"])],
        });
        if (paths.Count > 0) DefaultSample.SampleArgs.Path = paths[0];
    }

    /// <summary>Chooses the focused layer's generated sample source.</summary>
    [RelayCommand]
    private async Task PickEditSamplePathAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Choose layer sample",
            AllowMultiple = false,
            Filters = [new FilePickerFilter("Audio and SoundFont files", [".wav", ".ogg", ".mp3", ".sf2"])],
        });
        if (paths.Count > 0) EditSamplePath = paths[0];
    }

    /// <summary>Chooses the focused layer's import source.</summary>
    [RelayCommand]
    private async Task PickEditImportPathAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Choose import source",
            AllowMultiple = false,
            Filters = [new FilePickerFilter("Beatmaps, MIDI, and storyboards", [".osu", ".mid", ".midi", ".osb"])],
        });
        if (paths.Count > 0) EditImportPath = paths[0];
    }

    /// <summary>Loads the beatmap currently selected by the osu! client.</summary>
    [RelayCommand]
    private async Task LoadEditImportPathAsync()
    {
        string? path = await currentBeatmap.FindCurrentBeatmapAsync();
        if (!string.IsNullOrWhiteSpace(path)) EditImportPath = path;
    }

    /// <summary>Chooses the focused layer's imported source sample.</summary>
    [RelayCommand]
    private async Task PickEditImportSamplePathAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Choose imported sample",
            AllowMultiple = false,
            Filters = [new FilePickerFilter("Audio and SoundFont files", [".wav", ".ogg", ".mp3", ".sf2"])],
        });
        if (paths.Count > 0) EditImportSamplePath = paths[0];
    }

    /// <summary>Chooses the export folder.</summary>
    [RelayCommand]
    private async Task PickExportFolderAsync()
    {
        var paths = await filePicker.PickFoldersAsync(new OpenFolderPickerRequest
        {
            Title = "Choose Hitsound Studio export folder",
            AllowMultiple = false,
        });
        if (paths.Count > 0) ExportFolder = paths[0];
    }

    /// <summary>Loads a standalone legacy-compatible sample schema JSON document.</summary>
    [RelayCommand]
    private async Task LoadSampleSchemaAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Load sample schema",
            AllowMultiple = false,
            Filters = [new FilePickerFilter("JSON files", [".json"])],
        });
        if (paths.Count == 0) return;
        PreviousSampleSchema = await projectStore.LoadAsync<SampleSchema>(paths[0]);
        UsePreviousSampleSchema = true;
        ResultSummary = "Loaded previous sample schema.";
    }

    /// <summary>
    ///     Assigns selected SoundFont layers from filenames in the legacy
    ///     <c>[bank]_[patch]_[key]_[length]_[velocity]</c> format.
    /// </summary>
    [RelayCommand]
    private async Task BulkAssignSamplesAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Bulk assign samples",
            AllowMultiple = true,
            Filters = [new FilePickerFilter("Audio and SoundFont files", [".wav", ".ogg", ".mp3", ".sf2"])],
        });
        int assigned = 0;
        foreach (string path in paths)
        {
            string[] fields = Path.GetFileNameWithoutExtension(path).Split('_');
            if (fields.Length > 5) continue;
            int? bank = ParseOptionalInt(fields, 0);
            int? patch = ParseOptionalInt(fields, 1);
            int? key = ParseOptionalInt(fields, 2);
            int? length = ParseOptionalInt(fields, 3);
            int? velocity = ParseOptionalInt(fields, 4);
            foreach (var layer in SelectedLayers)
            {
                var sample = layer.SampleArgs;
                if (bank.HasValue && bank.Value != layer.ImportArgs.Bank
                    || patch.HasValue && patch.Value != layer.ImportArgs.Patch
                    || key.HasValue && key.Value != layer.ImportArgs.Key
                    || length.HasValue && length.Value != (int)Math.Round(layer.ImportArgs.Length)
                    || velocity.HasValue && velocity.Value != layer.ImportArgs.Velocity) continue;
                sample.Path = path;
                assigned++;
            }
        }

        ResultSummary = $"Assigned {assigned} sample{(assigned == 1 ? string.Empty : "s")}.";
    }

    partial void OnSingleSampleExportFormatChanged(HitsoundStudioSampleExportFormat value)
    {
        if (value == HitsoundStudioSampleExportFormat.MidiChords)
            MixedSampleExportFormat = value;
        else if (MixedSampleExportFormat == HitsoundStudioSampleExportFormat.MidiChords) MixedSampleExportFormat = value;
    }

    partial void OnMixedSampleExportFormatChanged(HitsoundStudioSampleExportFormat value)
    {
        if (value == HitsoundStudioSampleExportFormat.MidiChords)
            SingleSampleExportFormat = value;
        else if (SingleSampleExportFormat == HitsoundStudioSampleExportFormat.MidiChords) SingleSampleExportFormat = value;
    }

    /// <summary>Allows the view to update the focused item after an extended selection change.</summary>
    /// <param name="selected">The selected layers in list order.</param>
    public void SetSelection(IEnumerable<ObservableHitsoundLayer> selected)
    {
        SelectedLayers.Clear();
        foreach (var layer in selected) SelectedLayers.Add(layer);
        SelectedLayer = SelectedLayers.FirstOrDefault();
    }

    partial void OnSelectedLayerChanged(ObservableHitsoundLayer? value)
    {
        OnPropertyChanged(nameof(HasSelectedLayer));
        RefreshEditorVisibility();
        syncingEditor = true;
        EditTimes = value is null
            ? string.Empty
            : string.Join(", ", value.Times.Select(time => time.ToString("0.###", CultureInfo.InvariantCulture)));
        EditName = value?.Name ?? string.Empty;
        EditSampleSet = value?.SampleSet ?? SampleSet.Normal;
        EditHitsound = value?.Hitsound ?? Hitsound.Normal;
        EditSamplePath = value?.SampleArgs.Path ?? string.Empty;
        EditSampleVolume = Format(value?.SampleArgs.Volume * 100, 100);
        EditSamplePanning = Format(value?.SampleArgs.Panning, 0);
        EditSamplePitchShift = Format(value?.SampleArgs.PitchShift, 0);
        EditSampleBank = Format(value?.SampleArgs.Bank, -1);
        EditSamplePatch = Format(value?.SampleArgs.Patch, -1);
        EditSampleInstrument = Format(value?.SampleArgs.Instrument, -1);
        EditSampleKey = Format(value?.SampleArgs.Key, -1);
        EditSampleLength = Format(value?.SampleArgs.Length, -1);
        EditSampleVelocity = Format(value?.SampleArgs.Velocity, 127);
        EditImportType = value?.ImportArgs.ImportType ?? ImportType.None;
        EditImportPath = value?.ImportArgs.Path ?? string.Empty;
        EditImportX = Format(value?.ImportArgs.X, -1);
        EditImportY = Format(value?.ImportArgs.Y, -1);
        EditImportSamplePath = value?.ImportArgs.SamplePath ?? string.Empty;
        EditImportDiscriminateVolumes = value?.ImportArgs.DiscriminateVolumes ?? false;
        EditImportDetectDuplicates = value?.ImportArgs.DetectDuplicateSamples ?? false;
        EditImportRemoveDuplicates = value?.ImportArgs.RemoveDuplicates ?? false;
        EditImportBank = Format(value?.ImportArgs.Bank, -1);
        EditImportPatch = Format(value?.ImportArgs.Patch, -1);
        EditImportKey = Format(value?.ImportArgs.Key, -1);
        EditImportLength = Format(value?.ImportArgs.Length, -1);
        EditImportLengthRoughness = Format(value?.ImportArgs.LengthRoughness, 1);
        EditImportVelocity = Format(value?.ImportArgs.Velocity, -1);
        EditImportVelocityRoughness = Format(value?.ImportArgs.VelocityRoughness, 1);
        EditImportOffset = Format(value?.ImportArgs.Offset, 0);
        syncingEditor = false;
    }

    partial void OnEditTimesChanged(string value)
    {
        if (syncingEditor) return;
        string[] fields = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        List<double> times = [];
        foreach (string field in fields)
        {
            if (!double.TryParse(field, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double time)) return;
            times.Add(time);
        }

        times.Sort();

        foreach (var layer in SelectedLayers.Count > 0 ? SelectedLayers : SelectedLayer is null ? [] : [SelectedLayer]) layer.Times = times.ToList();
    }

    partial void OnEditNameChanged(string value)
    {
        if (syncingEditor) return;
        foreach (var layer in Targets()) layer.Name = value;
    }

    partial void OnEditSampleSetChanged(SampleSet value)
    {
        if (syncingEditor) return;
        foreach (var layer in Targets()) layer.SampleSet = value;
    }

    partial void OnEditHitsoundChanged(Hitsound value)
    {
        if (syncingEditor) return;
        foreach (var layer in Targets()) layer.Hitsound = value;
    }

    partial void OnEditSamplePathChanged(string value)
    {
        if (syncingEditor) return;
        foreach (var layer in Targets()) layer.SampleArgs.Path = value;
        RefreshEditorVisibility();
    }

    partial void OnEditSampleVolumeChanged(string value)
    {
        if (syncingEditor || !TryDouble(value, 100, out double parsed)) return;
        foreach (var layer in Targets()) layer.SampleArgs.Volume = parsed / 100;
    }

    partial void OnEditSamplePanningChanged(string value)
    {
        if (syncingEditor || !TryDouble(value, 0, out double parsed)) return;
        foreach (var layer in Targets()) layer.SampleArgs.Panning = parsed;
    }

    partial void OnEditSamplePitchShiftChanged(string value)
    {
        if (syncingEditor || !TryDouble(value, 0, out double parsed)) return;
        foreach (var layer in Targets()) layer.SampleArgs.PitchShift = parsed;
    }

    partial void OnEditSampleBankChanged(string value)
    {
        SetSampleInt(value, -1, (sample, parsed) => sample.Bank = parsed);
    }

    partial void OnEditSamplePatchChanged(string value)
    {
        SetSampleInt(value, -1, (sample, parsed) => sample.Patch = parsed);
    }

    partial void OnEditSampleInstrumentChanged(string value)
    {
        SetSampleInt(value, -1, (sample, parsed) => sample.Instrument = parsed);
    }

    partial void OnEditSampleKeyChanged(string value)
    {
        SetSampleInt(value, -1, (sample, parsed) => sample.Key = parsed);
    }

    partial void OnEditSampleLengthChanged(string value)
    {
        if (syncingEditor || !TryDouble(value, -1, out double parsed)) return;
        foreach (var layer in Targets()) layer.SampleArgs.Length = parsed;
    }

    partial void OnEditSampleVelocityChanged(string value)
    {
        if (syncingEditor || !TryInt(value, 127, out int parsed)) return;
        foreach (var layer in Targets()) layer.SampleArgs.Velocity = parsed;
    }

    partial void OnEditImportTypeChanged(ImportType value)
    {
        if (syncingEditor) return;
        foreach (var layer in Targets()) layer.ImportArgs.ImportType = value;
        RefreshEditorVisibility();
    }

    partial void OnEditImportPathChanged(string value)
    {
        SetImportString(value, (args, parsed) => args.Path = parsed);
    }

    partial void OnEditImportSamplePathChanged(string value)
    {
        SetImportString(value, (args, parsed) => args.SamplePath = parsed);
    }

    partial void OnEditImportXChanged(string value)
    {
        SetImportDouble(value, -1, (args, parsed) => args.X = parsed);
    }

    partial void OnEditImportYChanged(string value)
    {
        SetImportDouble(value, -1, (args, parsed) => args.Y = parsed);
    }

    partial void OnEditImportBankChanged(string value)
    {
        SetImportInt(value, -1, (args, parsed) => args.Bank = parsed);
    }

    partial void OnEditImportPatchChanged(string value)
    {
        SetImportInt(value, -1, (args, parsed) => args.Patch = parsed);
    }

    partial void OnEditImportKeyChanged(string value)
    {
        SetImportInt(value, -1, (args, parsed) => args.Key = parsed);
    }

    partial void OnEditImportLengthChanged(string value)
    {
        SetImportDouble(value, -1, (args, parsed) => args.Length = parsed);
    }

    partial void OnEditImportLengthRoughnessChanged(string value)
    {
        SetImportDouble(value, 1, (args, parsed) => args.LengthRoughness = parsed);
    }

    partial void OnEditImportVelocityChanged(string value)
    {
        SetImportInt(value, -1, (args, parsed) => args.Velocity = parsed);
    }

    partial void OnEditImportVelocityRoughnessChanged(string value)
    {
        SetImportDouble(value, 1, (args, parsed) => args.VelocityRoughness = parsed);
    }

    partial void OnEditImportOffsetChanged(string value)
    {
        SetImportDouble(value, 0, (args, parsed) => args.Offset = parsed);
    }

    partial void OnEditImportDiscriminateVolumesChanged(bool value)
    {
        SetImportBool(value, (args, parsed) => args.DiscriminateVolumes = parsed);
    }

    partial void OnEditImportDetectDuplicatesChanged(bool value)
    {
        SetImportBool(value, (args, parsed) => args.DetectDuplicateSamples = parsed);
    }

    partial void OnEditImportRemoveDuplicatesChanged(bool value)
    {
        SetImportBool(value, (args, parsed) => args.RemoveDuplicates = parsed);
    }

    private IEnumerable<ObservableHitsoundLayer> Targets()
    {
        return SelectedLayers.Count > 0 ? SelectedLayers : SelectedLayer is null ? [] : [SelectedLayer];
    }

    private void RefreshEditorVisibility()
    {
        OnPropertyChanged(nameof(HasImport));
        OnPropertyChanged(nameof(IsStackImport));
        OnPropertyChanged(nameof(IsHitsoundsImport));
        OnPropertyChanged(nameof(IsStoryboardImport));
        OnPropertyChanged(nameof(IsSampleImport));
        OnPropertyChanged(nameof(IsMidiImport));
        OnPropertyChanged(nameof(IsSoundFontSample));
    }

    private void SetSampleInt(string value, int fallback, Action<ObservableSampleGeneratingArgs, int> setter)
    {
        if (syncingEditor || !TryInt(value, fallback, out int parsed)) return;
        foreach (var layer in Targets()) setter(layer.SampleArgs, parsed);
    }

    private void SetImportString(string value, Action<LayerImportArgs, string> setter)
    {
        if (syncingEditor) return;
        foreach (var layer in Targets()) setter(layer.ImportArgs, value);
    }

    private void SetImportDouble(string value, double fallback, Action<LayerImportArgs, double> setter)
    {
        if (syncingEditor || !TryDouble(value, fallback, out double parsed)) return;
        foreach (var layer in Targets()) setter(layer.ImportArgs, parsed);
    }

    private void SetImportInt(string value, int fallback, Action<LayerImportArgs, int> setter)
    {
        if (syncingEditor || !TryInt(value, fallback, out int parsed)) return;
        foreach (var layer in Targets()) setter(layer.ImportArgs, parsed);
    }

    private void SetImportBool(bool value, Action<LayerImportArgs, bool> setter)
    {
        if (syncingEditor) return;
        foreach (var layer in Targets()) setter(layer.ImportArgs, value);
    }

    private static bool TryDouble(string value, double fallback, out double parsed)
    {
        return string.IsNullOrWhiteSpace(value)
            ? (parsed = fallback) == fallback
            : double.TryParse(value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out parsed);
    }

    private static bool TryInt(string value, int fallback, out int parsed)
    {
        return string.IsNullOrWhiteSpace(value)
            ? (parsed = fallback) == fallback
            : int.TryParse(value, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out parsed);
    }

    private static string Format<T>(T? value, T fallback) where T : struct
    {
        return Convert.ToString(value ?? fallback, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    partial void OnDefaultSampleVolumeChanged(string value)
    {
        if (TryDouble(value, 100, out double parsed)) DefaultSample.SampleArgs.Volume = Math.Abs(parsed + 1) < 1e-9 ? -0.01 : parsed / 100;
    }

    partial void OnDefaultSampleChanged(ObservableSample value)
    {
        if (value is not null) DefaultSampleVolume = FormatDefaultSampleVolume(value.SampleArgs.Volume);
    }

    private static string FormatDefaultSampleVolume(double volume)
    {
        return Math.Abs(volume + 0.01) < 1e-9
            ? "-1"
            : (volume * 100).ToString("0.###", CultureInfo.InvariantCulture);
    }

    private async Task RunExportAsync(IReadOnlyList<string> selectedPaths, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(BaseBeatmap)
            && selectedPaths.FirstOrDefault(path => path.EndsWith(".osu", StringComparison.OrdinalIgnoreCase)) is { } selected) BaseBeatmap = selected;

        var chosen = await dialogs.ShowExportAsync(ToProject());
        if (chosen is null) return;
        if (chosen.UsePreviousSampleSchema && chosen.PreviousSampleSchema is null)
        {
            await messageDialogs.ShowMessageAsync(new MessageDialogRequest<bool>(
                "Previous sample schema not found",
                "Load a previous sample schema before enabling this option.",
                [new DialogChoice<bool>("OK", true, true)],
                true));
            return;
        }

        if (!files.DirectoryExists(chosen.ExportFolder))
        {
            bool create = await messageDialogs.ShowMessageAsync(new MessageDialogRequest<bool>(
                "Export path not found",
                $"Folder at path \"{chosen.ExportFolder}\" does not exist. Create a new folder?",
                [
                    new DialogChoice<bool>("Yes", true, true),
                    new DialogChoice<bool>("No", false, IsCancel: true),
                ],
                false));
            if (!create) return;
        }

        InstallProject(chosen);
        var snapshot = chosen.Clone();
        var result = await Execution.ExecuteAsync(
            new ToolExecutionRequest<HitsoundStudioExportResult>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    var output = await service.ExportAsync(
                        snapshot,
                        new Progress<double>(value => context.ReportProgress(value, "Exporting hitsounds")),
                        context.CancellationToken);
                    return new ToolExecutionOutput<HitsoundStudioExportResult>(output, "Hitsound Studio export complete.");
                }),
            CreateProgress(),
            cancellationToken);
        if (result.Status == ToolExecutionStatus.Succeeded && result.Value is not null)
        {
            if (HitsoundExportModeSetting != HitsoundStudioExportMode.Midi) PreviousSampleSchema = result.Value.Schema;
            ResultSummary = ShowResults
                ? result.Value.DetailedSummary
                : "Hitsound Studio export complete.";
        }
    }

    private HitsoundStudioProject ToProject()
    {
        return new HitsoundStudioProject
        {
            BaseBeatmap = BaseBeatmap,
            DefaultSample = DefaultSample.Snapshot(),
            ExportFolder = ExportFolder,
            HitsoundDiffName = HitsoundDiffName,
            ExportMap = ExportMap,
            ExportSamples = ExportSamples,
            ShowResults = ShowResults,
            DeleteAllInExportFirst = DeleteAllInExportFirst,
            UsePreviousSampleSchema = UsePreviousSampleSchema,
            AllowGrowthPreviousSampleSchema = AllowGrowthPreviousSampleSchema,
            AddCoincidingRegularHitsounds = AddCoincidingRegularHitsounds,
            AddGreenLineVolumeToMidi = AddGreenLineVolumeToMidi,
            PreviousSampleSchema = PreviousSampleSchema,
            HitsoundExportModeSetting = HitsoundExportModeSetting,
            HitsoundExportGameMode = HitsoundExportGameMode,
            ZipLayersLeniency = ZipLayersLeniency,
            FirstCustomIndex = FirstCustomIndex,
            SingleSampleExportFormat = SingleSampleExportFormat,
            MixedSampleExportFormat = MixedSampleExportFormat,
            HitsoundLayers = Layers.Select(layer => layer.Snapshot()).ToList(),
        };
    }

    private void InstallProject(HitsoundStudioProject project)
    {
        var copy = project.Clone();
        BaseBeatmap = copy.BaseBeatmap;
        DefaultSample = new ObservableSample(copy.DefaultSample.Copy());
        ExportFolder = copy.ExportFolder;
        HitsoundDiffName = copy.HitsoundDiffName;
        ExportMap = copy.ExportMap;
        ExportSamples = copy.ExportSamples;
        ShowResults = copy.ShowResults;
        DeleteAllInExportFirst = copy.DeleteAllInExportFirst;
        UsePreviousSampleSchema = copy.UsePreviousSampleSchema;
        AllowGrowthPreviousSampleSchema = copy.AllowGrowthPreviousSampleSchema;
        AddCoincidingRegularHitsounds = copy.AddCoincidingRegularHitsounds;
        AddGreenLineVolumeToMidi = copy.AddGreenLineVolumeToMidi;
        PreviousSampleSchema = copy.PreviousSampleSchema;
        HitsoundExportModeSetting = copy.HitsoundExportModeSetting;
        HitsoundExportGameMode = copy.HitsoundExportGameMode;
        ZipLayersLeniency = copy.ZipLayersLeniency;
        FirstCustomIndex = copy.FirstCustomIndex;
        SingleSampleExportFormat = copy.SingleSampleExportFormat;
        MixedSampleExportFormat = copy.MixedSampleExportFormat;
        Layers = new ObservableCollection<ObservableHitsoundLayer>(
            copy.HitsoundLayers.Select(layer => new ObservableHitsoundLayer(layer)));
        SetSelection(Layers.Take(1));
        OnPropertyChanged(nameof(HasLayers));
    }

    /// <summary>
    ///     Moves selected layers while preserving their relative order.
    /// </summary>
    /// <param name="direction">-1 to raise or 1 to lower.</param>
    /// <param name="repeat">Whether to apply the WPF Shift-click ten-step move.</param>
    public void MoveSelectedLayers(int direction, bool repeat = false)
    {
        if (direction is not (-1 or 1)) throw new ArgumentOutOfRangeException(nameof(direction), "Direction must be -1 or 1.");

        int repetitions = repeat ? 10 : 1;
        var indices = SelectedLayers.Select(Layers.IndexOf).Where(index => index >= 0).OrderBy(index => index).ToList();
        if (indices.Count == 0) return;
        for (int repetition = 0; repetition < repetitions; repetition++)
        {
            if (direction < 0 && indices[0] == 0) break;
            if (direction > 0 && indices[^1] == Layers.Count - 1) break;
            foreach (int index in direction < 0 ? indices : indices.AsEnumerable().Reverse()) Layers.Move(index, index + direction);

            for (int index = 0; index < indices.Count; index++) indices[index] += direction;
        }

        RecalculatePriorities();
    }

    private void RecalculatePriorities()
    {
        for (int index = 0; index < Layers.Count; index++) Layers[index].Priority = index;
    }

    private static int? ParseOptionalInt(IReadOnlyList<string> fields, int index)
    {
        if (index >= fields.Count || string.IsNullOrWhiteSpace(fields[index])) return null;
        return int.TryParse(fields[index], NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int result)
            ? result
            : null;
    }
}
