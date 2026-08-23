using Mapping_Tools.Application.Audio;
using Mapping_Tools.Core.Audio;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Tools.HitsoundStudio;

/// <summary>Chooses how Hitsound Studio turns packages into an exported map.</summary>
public enum HitsoundStudioExportMode
{
    /// <summary>Uses osu! custom sample indices and optional greenlines.</summary>
    Standard,

    /// <summary>Places named samples at distinct positions.</summary>
    Coinciding,

    /// <summary>Writes named samples as storyboard sound events.</summary>
    Storyboard,

    /// <summary>Writes the generated SoundFont notes as a MIDI file.</summary>
    Midi,
}

/// <summary>Chooses the encoding used for one generated sample family.</summary>
public enum HitsoundStudioSampleExportFormat
{
    /// <summary>Copy compatible sources or fall back to floating-point WAV.</summary>
    Default,

    /// <summary>32-bit floating-point WAV.</summary>
    WaveIeeeFloat,

    /// <summary>16-bit PCM WAV.</summary>
    WavePcm,

    /// <summary>Ogg Vorbis.</summary>
    OggVorbis,

    /// <summary>A single-chord MIDI file.</summary>
    MidiChords,
}

/// <summary>Contains the complete persisted Hitsound Studio project state.</summary>
public sealed class HitsoundStudioProject
{
    /// <summary>Creates defaults matching the former WPF view model.</summary>
    public HitsoundStudioProject()
    {
        BaseBeatmap = string.Empty;
        DefaultSample = new Sample { Priority = int.MaxValue };
        HitsoundLayers = [];
        ExportFolder = string.Empty;
        HitsoundDiffName = "Hitsounds";
        ExportMap = true;
        ExportSamples = true;
        HitsoundExportModeSetting = HitsoundStudioExportMode.Standard;
        HitsoundExportGameMode = GameMode.Standard;
        ZipLayersLeniency = 15;
        FirstCustomIndex = 1;
        SingleSampleExportFormat = HitsoundStudioSampleExportFormat.Default;
        MixedSampleExportFormat = HitsoundStudioSampleExportFormat.Default;
        AddCoincidingRegularHitsounds = true;
        AddGreenLineVolumeToMidi = true;
    }

    /// <summary>Gets or sets the base beatmap used for export and MIDI timing.</summary>
    public string BaseBeatmap { get; set; }

    /// <summary>Gets or sets the normal fallback sample.</summary>
    public Sample DefaultSample { get; set; }

    /// <summary>Gets or sets the output directory.</summary>
    public string ExportFolder { get; set; }

    /// <summary>Gets or sets the version name written to the exported map.</summary>
    public string HitsoundDiffName { get; set; }

    /// <summary>Gets or sets whether the map file is exported.</summary>
    public bool ExportMap { get; set; }

    /// <summary>Gets or sets whether generated samples are exported.</summary>
    public bool ExportSamples { get; set; }

    /// <summary>Gets or sets whether the destination is cleared before export.</summary>
    public bool DeleteAllInExportFirst { get; set; }

    /// <summary>Gets or sets whether a completion summary is shown.</summary>
    public bool ShowResults { get; set; }

    /// <summary>Gets or sets whether the previous schema is the standard-mode schema.</summary>
    public bool UsePreviousSampleSchema { get; set; }

    /// <summary>Gets or sets whether an old schema can receive new entries.</summary>
    public bool AllowGrowthPreviousSampleSchema { get; set; }

    /// <summary>Gets or sets whether named modes retain regular hitsound flags.</summary>
    public bool AddCoincidingRegularHitsounds { get; set; }

    /// <summary>Gets or sets whether greenline volumes become MIDI volume changes.</summary>
    public bool AddGreenLineVolumeToMidi { get; set; }

    /// <summary>Gets or sets the schema from a previous standard or named export.</summary>
    public SampleSchema? PreviousSampleSchema { get; set; }

    /// <summary>Gets or sets the selected export mode.</summary>
    public HitsoundStudioExportMode HitsoundExportModeSetting { get; set; }

    /// <summary>Gets or sets the target osu! game mode.</summary>
    public GameMode HitsoundExportGameMode { get; set; }

    /// <summary>Gets or sets the grouping leniency in milliseconds.</summary>
    public double ZipLayersLeniency { get; set; }

    /// <summary>Gets or sets the first custom index assigned to new schemas.</summary>
    public int FirstCustomIndex { get; set; }

    /// <summary>Gets or sets the single-source export format.</summary>
    public HitsoundStudioSampleExportFormat SingleSampleExportFormat { get; set; }

    /// <summary>Gets or sets the mixed-source export format.</summary>
    public HitsoundStudioSampleExportFormat MixedSampleExportFormat { get; set; }

    /// <summary>Gets or sets editable layer data.</summary>
    public List<HitsoundLayer> HitsoundLayers { get; set; }

    /// <summary>
    ///     Creates an independent snapshot suitable for autosave, export, or a
    ///     background operation. Nested layer and schema objects are copied.
    /// </summary>
    /// <returns>A project with no shared mutable hitsound objects.</returns>
    public HitsoundStudioProject Clone()
    {
        HitsoundStudioProject copy = new()
        {
            BaseBeatmap = BaseBeatmap,
            DefaultSample = DefaultSample.Copy(),
            ExportFolder = ExportFolder,
            HitsoundDiffName = HitsoundDiffName,
            ExportMap = ExportMap,
            ExportSamples = ExportSamples,
            DeleteAllInExportFirst = DeleteAllInExportFirst,
            ShowResults = ShowResults,
            UsePreviousSampleSchema = UsePreviousSampleSchema,
            AllowGrowthPreviousSampleSchema = AllowGrowthPreviousSampleSchema,
            AddCoincidingRegularHitsounds = AddCoincidingRegularHitsounds,
            AddGreenLineVolumeToMidi = AddGreenLineVolumeToMidi,
            HitsoundExportModeSetting = HitsoundExportModeSetting,
            HitsoundExportGameMode = HitsoundExportGameMode,
            ZipLayersLeniency = ZipLayersLeniency,
            FirstCustomIndex = FirstCustomIndex,
            SingleSampleExportFormat = SingleSampleExportFormat,
            MixedSampleExportFormat = MixedSampleExportFormat,
            HitsoundLayers = HitsoundLayers.Select(CloneLayer).ToList(),
            PreviousSampleSchema = PreviousSampleSchema is null ? null : CloneSchema(PreviousSampleSchema),
        };
        return copy;
    }

    private static HitsoundLayer CloneLayer(HitsoundLayer layer)
    {
        return new HitsoundLayer(
            layer.Name,
            layer.SampleSet,
            layer.Hitsound,
            layer.Priority,
            CloneImportArgs(layer.ImportArgs),
            layer.SampleArgs.Copy(),
            layer.Times.ToList());
    }

    private static LayerImportArgs CloneImportArgs(LayerImportArgs source)
    {
        return new LayerImportArgs(source.ImportType)
        {
            Path = source.Path,
            X = source.X,
            Y = source.Y,
            SamplePath = source.SamplePath,
            Volume = source.Volume,
            DiscriminateVolumes = source.DiscriminateVolumes,
            DetectDuplicateSamples = source.DetectDuplicateSamples,
            RemoveDuplicates = source.RemoveDuplicates,
            Bank = source.Bank,
            Patch = source.Patch,
            Key = source.Key,
            Length = source.Length,
            LengthRoughness = source.LengthRoughness,
            Velocity = source.Velocity,
            VelocityRoughness = source.VelocityRoughness,
            Offset = source.Offset,
        };
    }

    private static SampleSchema CloneSchema(SampleSchema source)
    {
        SampleSchema copy = new();
        foreach ((string name, var samples) in source) copy[name] = samples.Select(sample => sample.Copy()).ToList();

        return copy;
    }
}

/// <summary>Describes one import operation requested by the Hitsound Studio dialog.</summary>
public sealed record HitsoundStudioImportRequest
{
    /// <summary>Gets or sets the import kind.</summary>
    public ImportType ImportType { get; init; }

    /// <summary>Gets or sets the layer name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Gets or sets the layer sample family.</summary>
    public SampleSet SampleSet { get; init; } = SampleSet.Normal;

    /// <summary>Gets or sets the layer hitsound.</summary>
    public Hitsound Hitsound { get; init; } = Hitsound.Normal;

    /// <summary>Gets or sets a direct sample path.</summary>
    public string SamplePath { get; init; } = string.Empty;

    /// <summary>Gets or sets one or more source beatmap/MIDI paths.</summary>
    public IReadOnlyList<string> Paths { get; init; } = [];

    /// <summary>Gets or sets the stack X filter, or -1 for wildcard.</summary>
    public double X { get; init; } = -1;

    /// <summary>Gets or sets the stack Y filter, or -1 for wildcard.</summary>
    public double Y { get; init; } = -1;

    /// <summary>Gets or sets the MIDI time offset.</summary>
    public double Offset { get; init; }

    /// <summary>Gets or sets whether beatmap volumes make distinct layers.</summary>
    public bool DiscriminateVolumes { get; init; }

    /// <summary>Gets or sets whether identical source files are collapsed.</summary>
    public bool DetectDuplicateSamples { get; init; }

    /// <summary>Gets or sets whether duplicate events are removed.</summary>
    public bool RemoveDuplicates { get; init; }

    /// <summary>Gets or sets whether storyboard sound events are included.</summary>
    public bool IncludeStoryboard { get; init; }

    /// <summary>Gets or sets whether MIDI instruments are part of layer identity.</summary>
    public bool DiscriminateInstruments { get; init; } = true;

    /// <summary>Gets or sets whether MIDI keys are part of layer identity.</summary>
    public bool DiscriminateKeys { get; init; } = true;

    /// <summary>Gets or sets whether MIDI lengths are part of layer identity.</summary>
    public bool DiscriminateLengths { get; init; }

    /// <summary>Gets or sets MIDI length rounding roughness.</summary>
    public double LengthRoughness { get; init; } = 2;

    /// <summary>Gets or sets whether MIDI velocities are part of layer identity.</summary>
    public bool DiscriminateVelocities { get; init; }

    /// <summary>Gets or sets MIDI velocity rounding roughness.</summary>
    public double VelocityRoughness { get; init; } = 10;
}

/// <summary>Describes the result of a completed Hitsound Studio export.</summary>
public sealed class HitsoundStudioExportResult
{
    /// <summary>Creates an export result.</summary>
    /// <param name="mapPath">The written map path, if a map was requested.</param>
    /// <param name="sampleCount">The number of generated sample files.</param>
    /// <param name="layerCount">The number of input layers.</param>
    /// <param name="eventCount">The number of exported events or MIDI notes.</param>
    /// <param name="schema">The schema produced by this run.</param>
    /// <param name="detailedSummary">The legacy Show Results text for this run.</param>
    public HitsoundStudioExportResult(
        string? mapPath,
        int sampleCount,
        int layerCount,
        int eventCount,
        SampleSchema schema,
        string detailedSummary)
    {
        MapPath = mapPath;
        SampleCount = sampleCount;
        LayerCount = layerCount;
        EventCount = eventCount;
        Schema = schema;
        DetailedSummary = detailedSummary;
    }

    /// <summary>Gets the written map path.</summary>
    public string? MapPath { get; }

    /// <summary>Gets the number of written sample files.</summary>
    public int SampleCount { get; }

    /// <summary>Gets the number of input layers.</summary>
    public int LayerCount { get; }

    /// <summary>Gets the number of generated events or notes.</summary>
    public int EventCount { get; }

    /// <summary>Gets the schema produced by the run.</summary>
    public SampleSchema Schema { get; }

    /// <summary>Gets the legacy detailed completion text.</summary>
    public string DetailedSummary { get; }
}

/// <summary>Provides the feature operations required by the Hitsound Studio presentation.</summary>
public interface IHitsoundStudioService
{
    /// <summary>Imports layers from a selected source while preserving its reload metadata.</summary>
    /// <param name="request">The source, filters, and layer settings to import.</param>
    /// <param name="cancellationToken">Stops source parsing before the next layer is created.</param>
    Task<IReadOnlyList<HitsoundLayer>> ImportAsync(
        HitsoundStudioImportRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Reimports every selected layer grouped by compatible source metadata.</summary>
    /// <param name="layers">The selected layers whose import metadata is re-run in place.</param>
    /// <param name="cancellationToken">Stops reloading between source groups.</param>
    Task<IReadOnlyList<HitsoundLayer>> ReloadAsync(
        IReadOnlyList<HitsoundLayer> layers,
        CancellationToken cancellationToken = default);

    /// <summary>Validates source paths and SoundFont notes without leaking decoder types.</summary>
    /// <param name="samples">The distinct source specifications to validate.</param>
    /// <param name="cancellationToken">Stops validation before the next source is decoded.</param>
    Task<IReadOnlyDictionary<SampleGeneratingArgs, Exception>> ValidateSamplesAsync(
        IReadOnlyList<SampleGeneratingArgs> samples,
        CancellationToken cancellationToken = default);

    /// <summary>Previews one generated source and returns its owned playback session.</summary>
    /// <param name="sample">The source and SoundFont parameters to render.</param>
    /// <param name="cancellationToken">Stops generation or playback startup.</param>
    Task<IAudioPlaybackSession> PreviewAsync(
        SampleGeneratingArgs sample,
        CancellationToken cancellationToken = default);

    /// <summary>Builds and writes the requested map/package with cooperative cancellation.</summary>
    /// <param name="project">An independent export snapshot and its output options.</param>
    /// <param name="progress">Receives monotonically increasing major-phase percentages.</param>
    /// <param name="cancellationToken">Stops generation, encoding, or writing at the next safe boundary.</param>
    Task<HitsoundStudioExportResult> ExportAsync(
        HitsoundStudioProject project,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default);
}

/// <summary>Owns the feature-specific import and export forms.</summary>
public interface IHitsoundStudioDialogService
{
    /// <summary>Shows the layer import form and returns submitted values.</summary>
    /// <param name="defaultName">The initial name shown for the new layer.</param>
    /// <param name="cancellationToken">Closes the modal operation when cancellation is requested.</param>
    Task<HitsoundStudioImportRequest?> ShowImportAsync(
        string defaultName,
        CancellationToken cancellationToken = default);

    /// <summary>Shows export options initialized from the current project snapshot.</summary>
    /// <param name="project">The current project copied into the form.</param>
    /// <param name="cancellationToken">Closes the modal operation when cancellation is requested.</param>
    Task<HitsoundStudioProject?> ShowExportAsync(
        HitsoundStudioProject project,
        CancellationToken cancellationToken = default);
}

/// <summary>Restricts filesystem mutations used by Hitsound Studio export.</summary>
public interface IHitsoundStudioFileSystem
{
    /// <summary>Gets whether a file exists.</summary>
    /// <param name="path">The path to inspect.</param>
    bool FileExists(string path);

    /// <summary>Gets whether a directory exists.</summary>
    /// <param name="path">The directory path to inspect.</param>
    bool DirectoryExists(string path);

    /// <summary>Creates a directory and its parents.</summary>
    /// <param name="path">The directory path to create.</param>
    void CreateDirectory(string path);

    /// <summary>Deletes every file directly inside a directory.</summary>
    /// <param name="path">The directory whose direct files are removed.</param>
    void DeleteFiles(string path);

    /// <summary>Copies one file and replaces an existing destination.</summary>
    /// <param name="sourcePath">The existing source file.</param>
    /// <param name="destinationPath">The destination file to replace.</param>
    void CopyFile(string sourcePath, string destinationPath);
}

/// <summary>Mixes generated neutral audio clips without exposing a desktop audio library.</summary>
/// <param name="clips">The decoded clips to mix; all clips must contain audio data.</param>
/// <param name="cancellationToken">Stops normalization or mixing before returning a clip.</param>
public interface IAudioClipMixer
{
    /// <summary>Mixes clips after resampling and channel normalization.</summary>
    Task<AudioClip> MixAsync(
        IReadOnlyList<AudioClip> clips,
        CancellationToken cancellationToken = default);
}
