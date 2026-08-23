using System.Globalization;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.HitsoundStudio;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Core.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Core.Classes.HitsoundStuff;
using Mapping_Tools.Desktop.Views.Dialogs;

namespace Mapping_Tools.Desktop.Interactions;

/// <summary>Shows the Hitsound Studio import and export forms as owner-modal windows.</summary>
public sealed class HitsoundStudioDialogService : IHitsoundStudioDialogService
{
    private readonly IFilePicker _filePicker;
    private readonly Func<Window> _owner;

    /// <summary>Creates the dialog adapter.</summary>
    /// <param name="owner">Returns the active shell window.</param>
    public HitsoundStudioDialogService(Func<Window> owner, IFilePicker filePicker)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
    }

    /// <inheritdoc />
    public async Task<HitsoundStudioImportRequest?> ShowImportAsync(
        string defaultName,
        CancellationToken cancellationToken = default)
    {
        HitsoundStudioImportDialogViewModel viewModel = new(defaultName, _filePicker);
        HitsoundStudioImportDialogWindow window = new() { DataContext = viewModel };
        viewModel.Close = value => window.Close(value);
        object? result = await window.ShowDialog<object?>(_owner()).WaitAsync(cancellationToken).ConfigureAwait(false);
        return result as HitsoundStudioImportRequest;
    }

    /// <inheritdoc />
    public async Task<HitsoundStudioProject?> ShowExportAsync(
        HitsoundStudioProject project,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(project);
        HitsoundStudioExportDialogViewModel viewModel = new(project, _filePicker);
        HitsoundStudioExportDialogWindow window = new() { DataContext = viewModel };
        viewModel.Close = value => window.Close(value);
        object? result = await window.ShowDialog<object?>(_owner()).WaitAsync(cancellationToken).ConfigureAwait(false);
        return result as HitsoundStudioProject;
    }
}

/// <summary>Owns the typed fields of the layer import form.</summary>
public sealed partial class HitsoundStudioImportDialogViewModel : ObservableObject
{
    private readonly IFilePicker _filePicker;
    private IAsyncRelayCommand? _pickSampleCommand;
    private IAsyncRelayCommand? _pickSourceCommand;

    /// <summary>Creates an import form with WPF-compatible defaults.</summary>
    /// <param name="defaultName">The suggested layer name.</param>
    public HitsoundStudioImportDialogViewModel(string defaultName, IFilePicker filePicker)
    {
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        Name = defaultName;
        AcceptCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(() => Close(null));
    }

    /// <summary>Gets or sets the layer name.</summary>
    [ObservableProperty]
    public partial string Name { get; set; }

    /// <summary>Gets or sets the import kind.</summary>
    [ObservableProperty]
    public partial ImportType ImportType { get; set; } = ImportType.None;

    /// <summary>Gets or sets the sample family.</summary>
    [ObservableProperty]
    public partial SampleSet SampleSet { get; set; } = SampleSet.Normal;

    /// <summary>Gets or sets the hitsound.</summary>
    [ObservableProperty]
    public partial Hitsound Hitsound { get; set; } = Hitsound.Normal;

    /// <summary>Gets or sets the audio/SoundFont path for a simple layer.</summary>
    [ObservableProperty]
    public partial string SamplePath { get; set; } = string.Empty;

    /// <summary>Gets or sets source paths separated by newlines.</summary>
    [ObservableProperty]
    public partial string SourcePaths { get; set; } = string.Empty;

    /// <summary>Gets or sets the stack X filter text.</summary>
    [ObservableProperty]
    public partial string XText { get; set; } = "-1";

    /// <summary>Gets or sets the stack Y filter text.</summary>
    [ObservableProperty]
    public partial string YText { get; set; } = "-1";

    /// <summary>Gets or sets the MIDI offset text.</summary>
    [ObservableProperty]
    public partial string OffsetText { get; set; } = "0";

    /// <summary>Gets or sets whether source volumes create separate layers.</summary>
    [ObservableProperty]
    public partial bool DiscriminateVolumes { get; set; }

    /// <summary>Gets or sets whether identical sample data is canonicalized.</summary>
    [ObservableProperty]
    public partial bool DetectDuplicateSamples { get; set; }

    /// <summary>Gets or sets whether duplicate event times are removed.</summary>
    [ObservableProperty]
    public partial bool RemoveDuplicates { get; set; }

    /// <summary>Gets or sets whether hitsound import includes storyboard sounds.</summary>
    [ObservableProperty]
    public partial bool IncludeStoryboard { get; set; }

    /// <summary>Gets or sets whether MIDI instruments are part of layer identity.</summary>
    [ObservableProperty]
    public partial bool DiscriminateInstruments { get; set; } = true;

    /// <summary>Gets or sets whether MIDI keys are part of layer identity.</summary>
    [ObservableProperty]
    public partial bool DiscriminateKeys { get; set; } = true;

    /// <summary>Gets or sets whether MIDI lengths are part of layer identity.</summary>
    [ObservableProperty]
    public partial bool DiscriminateLengths { get; set; }

    /// <summary>Gets or sets whether MIDI velocities are part of layer identity.</summary>
    [ObservableProperty]
    public partial bool DiscriminateVelocities { get; set; }

    /// <summary>Gets or sets the MIDI length roughness.</summary>
    [ObservableProperty]
    public partial string LengthRoughnessText { get; set; } = "2";

    /// <summary>Gets or sets the MIDI velocity roughness.</summary>
    [ObservableProperty]
    public partial string VelocityRoughnessText { get; set; } = "10";

    /// <summary>Gets the validation message.</summary>
    [ObservableProperty]
    public partial string Error { get; private set; } = string.Empty;

    /// <summary>Gets all import modes.</summary>
    public IReadOnlyList<ImportType> ImportTypes { get; } = Enum.GetValues<ImportType>();

    /// <summary>Gets all sample sets.</summary>
    public IReadOnlyList<SampleSet> SampleSets { get; } =
        Enum.GetValues<SampleSet>().Where(sampleSet => sampleSet != SampleSet.None).ToArray();

    /// <summary>Gets all hitsounds.</summary>
    public IReadOnlyList<Hitsound> Hitsounds { get; } = Enum.GetValues<Hitsound>();

    /// <summary>Gets whether direct sample fields apply to the selected import kind.</summary>
    public bool IsSimpleImport => ImportType == ImportType.None;

    /// <summary>Gets whether direct sample fields apply to a simple or stack import.</summary>
    public bool IsSimpleOrStackImport => ImportType is ImportType.None or ImportType.Stack;

    /// <summary>Gets whether stack-coordinate fields apply to the selected import kind.</summary>
    public bool IsStackImport => ImportType == ImportType.Stack;

    /// <summary>Gets whether beatmap sample fields apply to the selected import kind.</summary>
    public bool IsSampleImport => ImportType is ImportType.Hitsounds or ImportType.Storyboard;

    /// <summary>Gets whether hitsound-file-specific fields apply to the selected import kind.</summary>
    public bool IsHitsoundsImport => ImportType == ImportType.Hitsounds;

    /// <summary>Gets whether MIDI fields apply to the selected import kind.</summary>
    public bool IsMidiImport => ImportType == ImportType.MIDI;

    /// <summary>Gets whether MIDI length rounding applies to the selected import.</summary>
    public bool IsLengthSettingsVisible => IsMidiImport && DiscriminateLengths;

    /// <summary>Gets whether MIDI velocity rounding applies to the selected import.</summary>
    public bool IsVelocitySettingsVisible => IsMidiImport && DiscriminateVelocities;

    /// <summary>Gets whether a source path is required for the selected import kind.</summary>
    public bool HasImportSource => ImportType != ImportType.None;

    /// <summary>Gets the accept command.</summary>
    public IRelayCommand AcceptCommand { get; }

    /// <summary>Gets the cancel command.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Gets the source picker command.</summary>
    public IAsyncRelayCommand PickSourceCommand => _pickSourceCommand ??= new AsyncRelayCommand(PickSourceAsync);

    /// <summary>Gets the sample picker command.</summary>
    public IAsyncRelayCommand PickSampleCommand => _pickSampleCommand ??= new AsyncRelayCommand(PickSampleAsync);

    /// <summary>Gets or sets the modal close callback.</summary>
    internal Action<object?> Close { get; set; } = _ => { };

    partial void OnImportTypeChanged(ImportType value)
    {
        OnPropertyChanged(nameof(IsSimpleImport));
        OnPropertyChanged(nameof(IsSimpleOrStackImport));
        OnPropertyChanged(nameof(IsStackImport));
        OnPropertyChanged(nameof(IsSampleImport));
        OnPropertyChanged(nameof(IsHitsoundsImport));
        OnPropertyChanged(nameof(IsMidiImport));
        OnPropertyChanged(nameof(IsLengthSettingsVisible));
        OnPropertyChanged(nameof(IsVelocitySettingsVisible));
        OnPropertyChanged(nameof(HasImportSource));
    }

    partial void OnDiscriminateLengthsChanged(bool value)
    {
        OnPropertyChanged(nameof(IsLengthSettingsVisible));
    }

    partial void OnDiscriminateVelocitiesChanged(bool value)
    {
        OnPropertyChanged(nameof(IsVelocitySettingsVisible));
    }

    private async Task PickSourceAsync()
    {
        var paths = await _filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Choose Hitsound Studio source",
            AllowMultiple = true,
            Filters = [new FilePickerFilter("Beatmaps, MIDI, and storyboards", [".osu", ".mid", ".midi", ".osb"])],
        }).ConfigureAwait(false);
        if (paths.Count > 0) SourcePaths = string.Join(Environment.NewLine, paths);
    }

    private async Task PickSampleAsync()
    {
        var paths = await _filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Choose sample",
            AllowMultiple = false,
            Filters = [new FilePickerFilter("Audio and SoundFont files", [".wav", ".ogg", ".mp3", ".sf2"])],
        }).ConfigureAwait(false);
        if (paths.Count > 0) SamplePath = paths[0];
    }

    private void Accept()
    {
        Error = string.Empty;
        if (!TryParse(XText, -1, out double x)
            || !TryParse(YText, -1, out double y)
            || !TryParse(OffsetText, 0, out double offset)
            || !TryParse(LengthRoughnessText, 2, out double lengthRoughness)
            || !TryParse(VelocityRoughnessText, 10, out double velocityRoughness))
        {
            Error = "Numeric import fields must contain valid invariant numbers.";
            return;
        }

        string[] paths = SourcePaths.Split(
            ['\r', '\n', '|'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (ImportType != ImportType.None && paths.Length == 0)
        {
            Error = "Choose or enter at least one source path.";
            return;
        }

        Close(new HitsoundStudioImportRequest
        {
            ImportType = ImportType,
            Name = Name,
            SampleSet = SampleSet,
            Hitsound = Hitsound,
            SamplePath = SamplePath,
            Paths = paths,
            X = x,
            Y = y,
            Offset = offset,
            DiscriminateVolumes = DiscriminateVolumes,
            DetectDuplicateSamples = DetectDuplicateSamples,
            RemoveDuplicates = RemoveDuplicates,
            IncludeStoryboard = IncludeStoryboard,
            DiscriminateInstruments = DiscriminateInstruments,
            DiscriminateKeys = DiscriminateKeys,
            DiscriminateLengths = DiscriminateLengths,
            DiscriminateVelocities = DiscriminateVelocities,
            LengthRoughness = lengthRoughness,
            VelocityRoughness = velocityRoughness,
        });
    }

    private static bool TryParse(string text, double fallback, out double value)
    {
        return string.IsNullOrWhiteSpace(text)
            ? (value = fallback) == fallback
            : double.TryParse(text, NumberStyles.Float,
                CultureInfo.InvariantCulture, out value);
    }
}

/// <summary>Owns the fields of the Hitsound Studio export dialog.</summary>
public sealed partial class HitsoundStudioExportDialogViewModel : ObservableObject
{
    private readonly IFilePicker _filePicker;
    private readonly HitsoundStudioProject _project;
    private IAsyncRelayCommand? _pickFolderCommand;

    /// <summary>Creates export options from an independent project snapshot.</summary>
    /// <param name="project">The current feature state.</param>
    public HitsoundStudioExportDialogViewModel(HitsoundStudioProject project, IFilePicker filePicker)
    {
        _project = project.Clone();
        _filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        ExportFolder = _project.ExportFolder;
        HitsoundDiffName = _project.HitsoundDiffName;
        ExportMap = _project.ExportMap;
        ExportSamples = _project.ExportSamples;
        ShowResults = _project.ShowResults;
        DeleteAllInExportFirst = _project.DeleteAllInExportFirst;
        UsePreviousSampleSchema = _project.UsePreviousSampleSchema;
        AllowGrowthPreviousSampleSchema = _project.AllowGrowthPreviousSampleSchema;
        AddCoincidingRegularHitsounds = _project.AddCoincidingRegularHitsounds;
        AddGreenLineVolumeToMidi = _project.AddGreenLineVolumeToMidi;
        HitsoundExportModeSetting = _project.HitsoundExportModeSetting;
        HitsoundExportGameMode = _project.HitsoundExportGameMode;
        ZipLayersLeniency = _project.ZipLayersLeniency;
        FirstCustomIndex = _project.FirstCustomIndex;
        SingleSampleExportFormat = _project.SingleSampleExportFormat;
        MixedSampleExportFormat = _project.MixedSampleExportFormat;
        AcceptCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(() => Close(null));
    }

    /// <summary>Gets or sets the output folder.</summary>
    [ObservableProperty]
    public partial string ExportFolder { get; set; }

    /// <summary>Gets or sets the map version name.</summary>
    [ObservableProperty]
    public partial string HitsoundDiffName { get; set; }

    /// <summary>Gets or sets whether the map is exported.</summary>
    [ObservableProperty]
    public partial bool ExportMap { get; set; }

    /// <summary>Gets or sets whether samples are exported.</summary>
    [ObservableProperty]
    public partial bool ExportSamples { get; set; }

    /// <summary>Gets or sets whether the detailed completion summary is shown.</summary>
    [ObservableProperty]
    public partial bool ShowResults { get; set; }

    /// <summary>Gets or sets whether the output is cleared.</summary>
    [ObservableProperty]
    public partial bool DeleteAllInExportFirst { get; set; }

    /// <summary>Gets or sets whether the prior schema is used.</summary>
    [ObservableProperty]
    public partial bool UsePreviousSampleSchema { get; set; }

    /// <summary>Gets or sets whether the prior schema may grow.</summary>
    [ObservableProperty]
    public partial bool AllowGrowthPreviousSampleSchema { get; set; }

    /// <summary>Gets or sets whether coinciding modes retain regular hitsounds.</summary>
    [ObservableProperty]
    public partial bool AddCoincidingRegularHitsounds { get; set; }

    /// <summary>Gets or sets whether MIDI includes greenline volume.</summary>
    [ObservableProperty]
    public partial bool AddGreenLineVolumeToMidi { get; set; }

    /// <summary>Gets or sets export mode.</summary>
    [ObservableProperty]
    public partial HitsoundStudioExportMode HitsoundExportModeSetting { get; set; }

    /// <summary>Gets whether sample-specific options apply to the selected mode.</summary>
    public bool SampleExportSettingsVisible => HitsoundExportModeSetting != HitsoundStudioExportMode.Midi;

    /// <summary>Gets whether standard-mode-only options apply to the selected mode.</summary>
    public bool StandardExtraSettingsVisible => HitsoundExportModeSetting == HitsoundStudioExportMode.Standard;

    /// <summary>Gets whether coinciding-mode-only options apply to the selected mode.</summary>
    public bool CoincidingExtraSettingsVisible => HitsoundExportModeSetting == HitsoundStudioExportMode.Coinciding;

    /// <summary>Gets whether MIDI-only options apply to the selected mode.</summary>
    public bool MidiExtraSettingsVisible => HitsoundExportModeSetting == HitsoundStudioExportMode.Midi;

    /// <summary>Gets whether the map game mode applies to the selected mode.</summary>
    public bool GameModeVisible => HitsoundExportModeSetting != HitsoundStudioExportMode.Midi;

    /// <summary>Gets or sets the output game mode.</summary>
    [ObservableProperty]
    public partial GameMode HitsoundExportGameMode { get; set; }

    /// <summary>Gets or sets time grouping leniency.</summary>
    [ObservableProperty]
    public partial double ZipLayersLeniency { get; set; }

    /// <summary>Gets or sets the first custom index.</summary>
    [ObservableProperty]
    public partial int FirstCustomIndex { get; set; }

    /// <summary>Gets or sets single-source format.</summary>
    [ObservableProperty]
    public partial HitsoundStudioSampleExportFormat SingleSampleExportFormat { get; set; }

    /// <summary>Gets or sets mixed-source format.</summary>
    [ObservableProperty]
    public partial HitsoundStudioSampleExportFormat MixedSampleExportFormat { get; set; }

    /// <summary>Gets the export modes.</summary>
    public IReadOnlyList<HitsoundStudioExportMode> ExportModes { get; } = Enum.GetValues<HitsoundStudioExportMode>();

    /// <summary>Gets the game modes.</summary>
    public IReadOnlyList<GameMode> GameModes { get; } = Enum.GetValues<GameMode>();

    /// <summary>Gets the sample formats.</summary>
    public IReadOnlyList<HitsoundStudioSampleExportFormat> SampleExportFormats { get; } = Enum.GetValues<HitsoundStudioSampleExportFormat>();

    /// <summary>Gets the validation message.</summary>
    [ObservableProperty]
    public partial string Error { get; private set; } = string.Empty;

    /// <summary>Gets the accept command.</summary>
    public IRelayCommand AcceptCommand { get; }

    /// <summary>Gets the cancel command.</summary>
    public IRelayCommand CancelCommand { get; }

    /// <summary>Gets the export-folder picker command.</summary>
    public IAsyncRelayCommand PickFolderCommand => _pickFolderCommand ??= new AsyncRelayCommand(PickFolderAsync);

    /// <summary>Gets or sets the modal close callback.</summary>
    internal Action<object?> Close { get; set; } = _ => { };

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

    private async Task PickFolderAsync()
    {
        var paths = await _filePicker.PickFoldersAsync(new OpenFolderPickerRequest
        {
            Title = "Choose Hitsound Studio export folder",
            AllowMultiple = false,
        }).ConfigureAwait(false);
        if (paths.Count > 0) ExportFolder = paths[0];
    }

    partial void OnHitsoundExportModeSettingChanged(HitsoundStudioExportMode value)
    {
        OnPropertyChanged(nameof(SampleExportSettingsVisible));
        OnPropertyChanged(nameof(GameModeVisible));
        OnPropertyChanged(nameof(StandardExtraSettingsVisible));
        OnPropertyChanged(nameof(CoincidingExtraSettingsVisible));
        OnPropertyChanged(nameof(MidiExtraSettingsVisible));
    }

    private void Accept()
    {
        Error = string.Empty;
        if (string.IsNullOrWhiteSpace(ExportFolder) || string.IsNullOrWhiteSpace(HitsoundDiffName))
        {
            Error = "An export folder and map name are required.";
            return;
        }

        _project.ExportFolder = ExportFolder;
        _project.HitsoundDiffName = HitsoundDiffName;
        _project.ExportMap = ExportMap;
        _project.ExportSamples = ExportSamples;
        _project.ShowResults = ShowResults;
        _project.DeleteAllInExportFirst = DeleteAllInExportFirst;
        _project.UsePreviousSampleSchema = UsePreviousSampleSchema;
        _project.AllowGrowthPreviousSampleSchema = AllowGrowthPreviousSampleSchema;
        _project.AddCoincidingRegularHitsounds = AddCoincidingRegularHitsounds;
        _project.AddGreenLineVolumeToMidi = AddGreenLineVolumeToMidi;
        _project.HitsoundExportModeSetting = HitsoundExportModeSetting;
        _project.HitsoundExportGameMode = HitsoundExportGameMode;
        _project.ZipLayersLeniency = ZipLayersLeniency;
        _project.FirstCustomIndex = FirstCustomIndex;
        _project.SingleSampleExportFormat = SingleSampleExportFormat;
        _project.MixedSampleExportFormat = MixedSampleExportFormat;
        Close(_project);
    }
}
