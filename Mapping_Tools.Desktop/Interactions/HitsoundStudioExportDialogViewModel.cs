using System.Globalization;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Platform;
using Mapping_Tools.Application.Tools.HitsoundStudio;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Desktop.Views.Dialogs;

namespace Mapping_Tools.Desktop.Interactions;

/// <summary>Owns the fields of the Hitsound Studio export dialog.</summary>
public sealed partial class HitsoundStudioExportDialogViewModel : ObservableObject
{
    private readonly IFilePicker filePicker;
    private readonly HitsoundStudioProject project;
    private IAsyncRelayCommand? pickFolderCommand;

    /// <summary>Creates export options from an independent project snapshot.</summary>
    /// <param name="project">The current feature state.</param>
    public HitsoundStudioExportDialogViewModel(HitsoundStudioProject project, IFilePicker filePicker)
    {
        this.project = project.Clone();
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
        ExportFolder = this.project.ExportFolder;
        HitsoundDiffName = this.project.HitsoundDiffName;
        ExportMap = this.project.ExportMap;
        ExportSamples = this.project.ExportSamples;
        ShowResults = this.project.ShowResults;
        DeleteAllInExportFirst = this.project.DeleteAllInExportFirst;
        UsePreviousSampleSchema = this.project.UsePreviousSampleSchema;
        AllowGrowthPreviousSampleSchema = this.project.AllowGrowthPreviousSampleSchema;
        AddCoincidingRegularHitsounds = this.project.AddCoincidingRegularHitsounds;
        AddGreenLineVolumeToMidi = this.project.AddGreenLineVolumeToMidi;
        HitsoundExportModeSetting = this.project.HitsoundExportModeSetting;
        HitsoundExportGameMode = this.project.HitsoundExportGameMode;
        ZipLayersLeniency = this.project.ZipLayersLeniency;
        FirstCustomIndex = this.project.FirstCustomIndex;
        SingleSampleExportFormat = this.project.SingleSampleExportFormat;
        MixedSampleExportFormat = this.project.MixedSampleExportFormat;
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
    public IAsyncRelayCommand PickFolderCommand => pickFolderCommand ??= new AsyncRelayCommand(PickFolderAsync);

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
        var paths = await filePicker.PickFoldersAsync(new OpenFolderPickerRequest
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

        project.ExportFolder = ExportFolder;
        project.HitsoundDiffName = HitsoundDiffName;
        project.ExportMap = ExportMap;
        project.ExportSamples = ExportSamples;
        project.ShowResults = ShowResults;
        project.DeleteAllInExportFirst = DeleteAllInExportFirst;
        project.UsePreviousSampleSchema = UsePreviousSampleSchema;
        project.AllowGrowthPreviousSampleSchema = AllowGrowthPreviousSampleSchema;
        project.AddCoincidingRegularHitsounds = AddCoincidingRegularHitsounds;
        project.AddGreenLineVolumeToMidi = AddGreenLineVolumeToMidi;
        project.HitsoundExportModeSetting = HitsoundExportModeSetting;
        project.HitsoundExportGameMode = HitsoundExportGameMode;
        project.ZipLayersLeniency = ZipLayersLeniency;
        project.FirstCustomIndex = FirstCustomIndex;
        project.SingleSampleExportFormat = SingleSampleExportFormat;
        project.MixedSampleExportFormat = MixedSampleExportFormat;
        Close(project);
    }
}
