using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;

namespace Mapping_Tools.Application.Tools.HitsoundStudio.Models;

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

