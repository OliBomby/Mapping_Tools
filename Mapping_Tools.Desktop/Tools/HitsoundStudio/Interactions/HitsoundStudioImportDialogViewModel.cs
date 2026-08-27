using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Platform.FilePicker;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Desktop.Tools.HitsoundStudio.Interactions;

/// <summary>Owns the typed fields of the layer import form.</summary>
public sealed partial class HitsoundStudioImportDialogViewModel : ObservableObject
{
    private readonly IFilePicker filePicker;
    private IAsyncRelayCommand? pickSampleCommand;
    private IAsyncRelayCommand? pickSourceCommand;

    /// <summary>Creates an import form with WPF-compatible defaults.</summary>
    /// <param name="defaultName">The suggested layer name.</param>
    public HitsoundStudioImportDialogViewModel(string defaultName, IFilePicker filePicker)
    {
        this.filePicker = filePicker ?? throw new ArgumentNullException(nameof(filePicker));
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
    public IAsyncRelayCommand PickSourceCommand => pickSourceCommand ??= new AsyncRelayCommand(PickSourceAsync);

    /// <summary>Gets the sample picker command.</summary>
    public IAsyncRelayCommand PickSampleCommand => pickSampleCommand ??= new AsyncRelayCommand(PickSampleAsync);

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
        var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
        {
            Title = "Choose Hitsound Studio source",
            AllowMultiple = true,
            Filters = [new FilePickerFilter("Beatmaps, MIDI, and storyboards", [".osu", ".mid", ".midi", ".osb"])],
        }).ConfigureAwait(false);
        if (paths.Count > 0) SourcePaths = string.Join(Environment.NewLine, paths);
    }

    private async Task PickSampleAsync()
    {
        var paths = await filePicker.PickOpenFilesAsync(new OpenFilePickerRequest
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

