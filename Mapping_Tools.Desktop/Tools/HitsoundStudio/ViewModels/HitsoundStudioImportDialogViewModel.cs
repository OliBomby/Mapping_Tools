using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Desktop.Services.Dialogs;

namespace Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;

/// <summary>Owns the typed fields of the layer import form.</summary>
public sealed partial class HitsoundStudioImportDialogViewModel : ObservableObject
{
    private readonly ICurrentBeatmapDialogService currentBeatmapService;
    private readonly IFilePicker filePicker;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>Creates an import form with WPF-compatible defaults.</summary>
    /// <param name="defaultName">The suggested layer name.</param>
    /// <param name="currentBeatmapService">Fetches the current beatmap and presents lookup feedback.</param>
    /// <param name="workspace">Supplies the shared default beatmap picker location.</param>
    /// <param name="filePicker">Presents the native file picker.</param>
    public HitsoundStudioImportDialogViewModel(
        string defaultName,
        ICurrentBeatmapDialogService currentBeatmapService,
        IBeatmapWorkspace workspace,
        IFilePicker filePicker)
    {
        this.currentBeatmapService = currentBeatmapService
                                     ?? throw new ArgumentNullException(nameof(currentBeatmapService));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        Name = defaultName;
        IReadOnlyList<string> selectedPaths = workspace.SelectedPaths;
        BeatmapPath = selectedPaths.FirstOrDefault() ?? string.Empty;
        AcceptCommand = new RelayCommand(Accept);
        CancelCommand = new RelayCommand(() => Close(null));
    }

    /// <summary>Gets or sets the layer name.</summary>
    [ObservableProperty]
    public partial string Name { get; set; }

    /// <summary>Gets or sets the import kind.</summary>
    [ObservableProperty]
    public partial ImportType ImportType { get; set; } = ImportType.None;

    /// <summary>Gets or sets the selected legacy import tab.</summary>
    [ObservableProperty]
    public partial int SelectedTabIndex { get; set; }

    /// <summary>Gets or sets the sample family.</summary>
    [ObservableProperty]
    public partial SampleSet SampleSet { get; set; } = SampleSet.Normal;

    /// <summary>Gets or sets the hitsound.</summary>
    [ObservableProperty]
    public partial Hitsound Hitsound { get; set; } = Hitsound.Normal;

    /// <summary>Gets or sets the audio/SoundFont path for a simple layer.</summary>
    [ObservableProperty]
    public partial string SamplePath { get; set; } = string.Empty;

    /// <summary>Gets or sets the beatmap path used by beatmap-based imports.</summary>
    [ObservableProperty]
    public partial string BeatmapPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the MIDI source path.</summary>
    [ObservableProperty]
    public partial string MidiPath { get; set; } = string.Empty;

    /// <summary>Gets or sets the stack X filter.</summary>
    [ObservableProperty]
    public partial double X { get; set; } = -1;

    /// <summary>Gets or sets the stack Y filter.</summary>
    [ObservableProperty]
    public partial double Y { get; set; } = -1;

    /// <summary>Gets or sets the MIDI offset.</summary>
    [ObservableProperty]
    public partial double Offset { get; set; }

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
    [NotifyPropertyChangedFor(nameof(IsLengthSettingsVisible))]
    public partial bool DiscriminateLengths { get; set; }

    /// <summary>Gets or sets whether MIDI velocities are part of layer identity.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsVelocitySettingsVisible))]
    public partial bool DiscriminateVelocities { get; set; }

    /// <summary>Gets or sets the MIDI length roughness.</summary>
    [ObservableProperty]
    public partial double LengthRoughness { get; set; } = 2;

    /// <summary>Gets or sets the MIDI velocity roughness.</summary>
    [ObservableProperty]
    public partial double VelocityRoughness { get; set; } = 10;

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
    public IAsyncRelayCommand PickSourceCommand => field ??= new AsyncRelayCommand(PickSourceAsync);

    /// <summary>Gets the command that fills source paths from the current osu! beatmap.</summary>
    public IAsyncRelayCommand LoadSourceCommand => field ??= new AsyncRelayCommand(LoadSourceAsync);

    /// <summary>Gets the sample picker command.</summary>
    public IAsyncRelayCommand PickSampleCommand => field ??= new AsyncRelayCommand(PickSampleAsync);

    /// <summary>Gets or sets the modal close callback.</summary>
    internal Action<object?> Close { get; set; } = _ => { };

    partial void OnImportTypeChanged(ImportType value)
    {
        int tabIndex = GetTabIndex(value);
        if (SelectedTabIndex != tabIndex) SelectedTabIndex = tabIndex;

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

    partial void OnSelectedTabIndexChanged(int value)
    {
        ImportType = value switch
        {
            1 => ImportType.Stack,
            2 => ImportType.Hitsounds,
            3 => ImportType.MIDI,
            4 => ImportType.Storyboard,
            _ => ImportType.None,
        };
    }

    private async Task PickSourceAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Choose Hitsound Studio source",
            SuggestedStartLocation = ImportType == ImportType.MIDI
                ? null
                : workspace.GetBeatmapPickerStartLocation(
                    Path.GetDirectoryName(BeatmapPath)),
            AllowMultiple = false,
            Filters = ImportType == ImportType.MIDI
                ? [new FilePickerFilter("MIDI files", ["*.mid"])]
                : [CommonFilePickerFilters.BeatmapsAndStoryboards],
        }).ConfigureAwait(false);
        if (paths.Count == 0) return;
        if (ImportType == ImportType.MIDI)
            MidiPath = paths[0];
        else
            BeatmapPath = paths[0];
    }

    internal async Task LoadSourceAsync()
    {
        string? path = await currentBeatmapService.FetchAsync();
        if (path is null) return;

        BeatmapPath = path;
    }

    private async Task PickSampleAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Choose sample",
            AllowMultiple = false,
            Filters = [CommonFilePickerFilters.SampleFiles],
        }).ConfigureAwait(false);
        if (paths.Count > 0) SamplePath = paths[0];
    }

    private void Accept()
    {
        string sourcePath = ImportType == ImportType.MIDI ? MidiPath : BeatmapPath;
        if (ImportType != ImportType.None && string.IsNullOrWhiteSpace(sourcePath))
            return;

        string[] paths = ImportType == ImportType.None || string.IsNullOrWhiteSpace(sourcePath)
            ? []
            : [sourcePath.Trim()];

        Close(new HitsoundStudioImportRequest
        {
            ImportType = ImportType,
            Name = Name,
            SampleSet = SampleSet,
            Hitsound = Hitsound,
            SamplePath = SamplePath,
            Paths = paths,
            X = X,
            Y = Y,
            Offset = Offset,
            DiscriminateVolumes = DiscriminateVolumes,
            DetectDuplicateSamples = DetectDuplicateSamples,
            RemoveDuplicates = RemoveDuplicates,
            IncludeStoryboard = IncludeStoryboard,
            DiscriminateInstruments = DiscriminateInstruments,
            DiscriminateKeys = DiscriminateKeys,
            DiscriminateLengths = DiscriminateLengths,
            DiscriminateVelocities = DiscriminateVelocities,
            LengthRoughness = LengthRoughness,
            VelocityRoughness = VelocityRoughness,
        });
    }

    private static int GetTabIndex(ImportType importType)
    {
        return importType switch
        {
            ImportType.Stack => 1,
            ImportType.Hitsounds => 2,
            ImportType.MIDI => 3,
            ImportType.Storyboard => 4,
            _ => 0,
        };
    }
}
