using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Tools;
using Mapping_Tools.Application.Tools.PropertyTransformer;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Core.Tools.PropertyTransformer;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tools.PropertyTransformer.Models;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.Desktop.Tools.PropertyTransformer.ViewModels;

/// <summary>
///     Owns Property Transformer form state, synchronized time fields, project persistence, and execution.
/// </summary>
public sealed partial class PropertyTransformerViewModel : SingleRunToolViewModel,
    IShellProjectFeature<PropertyTransformerProject>
{

    private readonly ProjectDefinition<PropertyTransformerProject> definition = new(
        "propertytransformerproject.json",
        "Property Transformer Projects",
        () => new PropertyTransformerProject(),
        "property-transformer-project.json",
        ToolConfigSchema.ForTool(PropertyTransformerToolDefinition.Definition.Id));

    private readonly IPropertyTransformerService propertyTransformer;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>
    ///     Creates a Property Transformer presentation model.
    /// </summary>
    /// <param name="propertyTransformer">Runs the framework-independent transformation.</param>
    /// <param name="execution">Coordinates background execution and notifications.</param>
    /// <param name="workspace">Supplies the selected beatmap and storyboard paths.</param>
    public PropertyTransformerViewModel(
        IPropertyTransformerService propertyTransformer,
        IToolExecutionService execution,
        IBeatmapWorkspace workspace)
        : base(execution, PropertyTransformerToolDefinition.Definition)
    {
        this.propertyTransformer = propertyTransformer
                                   ?? throw new ArgumentNullException(nameof(propertyTransformer));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    /// <summary>Gets or sets the timing-point offset multiplier.</summary>
    [ObservableProperty]
    public partial double TimingpointOffsetMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the timing-point offset addition in milliseconds.</summary>
    [ObservableProperty]
    public partial double TimingpointOffsetOffset { get; set; }

    /// <summary>Gets or sets the uninherited timing-point BPM multiplier.</summary>
    [ObservableProperty]
    public partial double TimingpointBpmMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the uninherited timing-point BPM addition.</summary>
    [ObservableProperty]
    public partial double TimingpointBpmOffset { get; set; }

    /// <summary>Gets or sets the inherited timing-point slider-velocity multiplier.</summary>
    [ObservableProperty]
    public partial double TimingpointSvMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the inherited timing-point slider-velocity addition.</summary>
    [ObservableProperty]
    public partial double TimingpointSvOffset { get; set; }

    /// <summary>Gets or sets the timing-point custom-index multiplier.</summary>
    [ObservableProperty]
    public partial double TimingpointIndexMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the timing-point custom-index addition.</summary>
    [ObservableProperty]
    public partial double TimingpointIndexOffset { get; set; }

    /// <summary>Gets or sets the timing-point volume multiplier.</summary>
    [ObservableProperty]
    public partial double TimingpointVolumeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the timing-point volume addition.</summary>
    [ObservableProperty]
    public partial double TimingpointVolumeOffset { get; set; }

    /// <summary>Gets or sets the hit-object time multiplier.</summary>
    [ObservableProperty]
    public partial double HitObjectTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the hit-object time addition in milliseconds.</summary>
    [ObservableProperty]
    public partial double HitObjectTimeOffset { get; set; }

    /// <summary>Gets or sets the hit-object volume multiplier.</summary>
    [ObservableProperty]
    public partial double HitObjectVolumeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the hit-object volume addition.</summary>
    [ObservableProperty]
    public partial double HitObjectVolumeOffset { get; set; }

    /// <summary>Gets or sets the bookmark time multiplier.</summary>
    [ObservableProperty]
    public partial double BookmarkTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the bookmark time addition in milliseconds.</summary>
    [ObservableProperty]
    public partial double BookmarkTimeOffset { get; set; }

    /// <summary>Gets or sets the storyboard event time multiplier.</summary>
    [ObservableProperty]
    public partial double SbEventTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the storyboard event time addition in milliseconds.</summary>
    [ObservableProperty]
    public partial double SbEventTimeOffset { get; set; }

    /// <summary>Gets or sets the storyboard sample time multiplier.</summary>
    [ObservableProperty]
    public partial double SbSampleTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the storyboard sample time addition in milliseconds.</summary>
    [ObservableProperty]
    public partial double SbSampleTimeOffset { get; set; }

    /// <summary>Gets or sets the storyboard sample volume multiplier.</summary>
    [ObservableProperty]
    public partial double SbSampleVolumeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the storyboard sample volume addition.</summary>
    [ObservableProperty]
    public partial double SbSampleVolumeOffset { get; set; }

    /// <summary>Gets or sets the break time multiplier.</summary>
    [ObservableProperty]
    public partial double BreakTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the break time addition in milliseconds.</summary>
    [ObservableProperty]
    public partial double BreakTimeOffset { get; set; }

    /// <summary>Gets or sets the video start-time multiplier.</summary>
    [ObservableProperty]
    public partial double VideoTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the video start-time addition in milliseconds.</summary>
    [ObservableProperty]
    public partial double VideoTimeOffset { get; set; }

    /// <summary>Gets or sets the preview-point time multiplier.</summary>
    [ObservableProperty]
    public partial double PreviewTimeMultiplier { get; set; } = 1;

    /// <summary>Gets or sets the preview-point time addition.</summary>
    [ObservableProperty]
    public partial double PreviewTimeOffset { get; set; }

    /// <summary>Gets or sets whether transformed values are clipped to their legacy bounds.</summary>
    [ObservableProperty]
    public partial bool ClipProperties { get; set; }

    /// <summary>Gets or sets whether value and time filters are active.</summary>
    [ObservableProperty]
    public partial bool EnableFilters { get; set; }

    /// <summary>Gets or sets the values allowed by the match filter.</summary>
    [ObservableProperty]
    public partial double[] MatchFilter { get; set; } = [];

    /// <summary>Gets or sets the values rejected by the mismatch filter.</summary>
    [ObservableProperty]
    public partial double[] UnmatchFilter { get; set; } = [];

    /// <summary>Gets or sets the inclusive lower time filter, or <c>-1</c> for none.</summary>
    [ObservableProperty]
    public partial double MinTimeFilter { get; set; } = -1;

    /// <summary>Gets or sets the inclusive upper time filter, or <c>-1</c> for none.</summary>
    [ObservableProperty]
    public partial double MaxTimeFilter { get; set; } = -1;

    /// <summary>Gets or sets whether all time-related fields are synchronized.</summary>
    [ObservableProperty]
    public partial bool SyncTimeFields { get; set; }

    ProjectDefinition<PropertyTransformerProject> IShellProjectFeature<PropertyTransformerProject>.ProjectDefinition => definition;

    PropertyTransformerProject IShellProjectFeature<PropertyTransformerProject>.Snapshot()
    {
        return Snapshot();
    }

    void IShellProjectFeature<PropertyTransformerProject>.Install(PropertyTransformerProject project)
    {
        Install(project);
    }

    /// <inheritdoc />
    protected override async Task RunCoreAsync()
    {
        PropertyTransformerProject options = Snapshot();
        await Execution.ExecuteAsync(
                new ToolExecutionRequest<PropertyTransformerResult>(
                Tool.Id,
                Tool.DisplayName,
                    async context =>
                    {
                        Progress<double> progress = new(value =>
                            context.ReportProgress(value, "Transforming documents"));
                        var result = await propertyTransformer
                            .TransformAsync(
                                workspace.SelectedPaths,
                                options,
                                progress,
                                context.CancellationToken)
                            .ConfigureAwait(false);
                        return new ToolExecutionOutput<PropertyTransformerResult>(
                            result,
                            "Done!");
                    }),
                CreateProgress())
            .ConfigureAwait(false);
    }

    partial void OnTimingpointOffsetMultiplierChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeMultipliers(value);
    }

    partial void OnTimingpointOffsetOffsetChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeOffsets(value);
    }

    partial void OnHitObjectTimeMultiplierChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeMultipliers(value);
    }

    partial void OnHitObjectTimeOffsetChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeOffsets(value);
    }

    partial void OnBookmarkTimeMultiplierChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeMultipliers(value);
    }

    partial void OnBookmarkTimeOffsetChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeOffsets(value);
    }

    partial void OnSbEventTimeMultiplierChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeMultipliers(value);
    }

    partial void OnSbEventTimeOffsetChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeOffsets(value);
    }

    partial void OnSbSampleTimeMultiplierChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeMultipliers(value);
    }

    partial void OnSbSampleTimeOffsetChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeOffsets(value);
    }

    partial void OnBreakTimeMultiplierChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeMultipliers(value);
    }

    partial void OnBreakTimeOffsetChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeOffsets(value);
    }

    partial void OnVideoTimeMultiplierChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeMultipliers(value);
    }

    partial void OnVideoTimeOffsetChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeOffsets(value);
    }

    partial void OnPreviewTimeMultiplierChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeMultipliers(value);
    }

    partial void OnPreviewTimeOffsetChanged(double value)
    {
        if (SyncTimeFields) SetAllTimeOffsets(value);
    }

    [RelayCommand]
    private void Reset()
    {
        ResetMultipliersAndOffsets();
    }

    private PropertyTransformerProject Snapshot()
    {
        return new PropertyTransformerProject
        {
            TimingpointOffsetMultiplier = TimingpointOffsetMultiplier,
            TimingpointOffsetOffset = TimingpointOffsetOffset,
            TimingpointBpmMultiplier = TimingpointBpmMultiplier,
            TimingpointBpmOffset = TimingpointBpmOffset,
            TimingpointSvMultiplier = TimingpointSvMultiplier,
            TimingpointSvOffset = TimingpointSvOffset,
            TimingpointIndexMultiplier = TimingpointIndexMultiplier,
            TimingpointIndexOffset = TimingpointIndexOffset,
            TimingpointVolumeMultiplier = TimingpointVolumeMultiplier,
            TimingpointVolumeOffset = TimingpointVolumeOffset,
            HitObjectTimeMultiplier = HitObjectTimeMultiplier,
            HitObjectTimeOffset = HitObjectTimeOffset,
            HitObjectVolumeMultiplier = HitObjectVolumeMultiplier,
            HitObjectVolumeOffset = HitObjectVolumeOffset,
            BookmarkTimeMultiplier = BookmarkTimeMultiplier,
            BookmarkTimeOffset = BookmarkTimeOffset,
            SbEventTimeMultiplier = SbEventTimeMultiplier,
            SbEventTimeOffset = SbEventTimeOffset,
            SbSampleTimeMultiplier = SbSampleTimeMultiplier,
            SbSampleTimeOffset = SbSampleTimeOffset,
            SbSampleVolumeMultiplier = SbSampleVolumeMultiplier,
            SbSampleVolumeOffset = SbSampleVolumeOffset,
            BreakTimeMultiplier = BreakTimeMultiplier,
            BreakTimeOffset = BreakTimeOffset,
            VideoTimeMultiplier = VideoTimeMultiplier,
            VideoTimeOffset = VideoTimeOffset,
            PreviewTimeMultiplier = PreviewTimeMultiplier,
            PreviewTimeOffset = PreviewTimeOffset,
            ClipProperties = ClipProperties,
            EnableFilters = EnableFilters,
            MatchFilter = MatchFilter.ToArray(),
            UnmatchFilter = UnmatchFilter.ToArray(),
            MinTimeFilter = MinTimeFilter,
            MaxTimeFilter = MaxTimeFilter,
            SyncTimeFields = SyncTimeFields,
        };
    }

    private void Install(PropertyTransformerProject project)
    {
        ArgumentNullException.ThrowIfNull(project);

        SyncTimeFields = false;
        TimingpointOffsetMultiplier = project.TimingpointOffsetMultiplier;
        TimingpointOffsetOffset = project.TimingpointOffsetOffset;
        TimingpointBpmMultiplier = project.TimingpointBpmMultiplier;
        TimingpointBpmOffset = project.TimingpointBpmOffset;
        TimingpointSvMultiplier = project.TimingpointSvMultiplier;
        TimingpointSvOffset = project.TimingpointSvOffset;
        TimingpointIndexMultiplier = project.TimingpointIndexMultiplier;
        TimingpointIndexOffset = project.TimingpointIndexOffset;
        TimingpointVolumeMultiplier = project.TimingpointVolumeMultiplier;
        TimingpointVolumeOffset = project.TimingpointVolumeOffset;
        HitObjectTimeMultiplier = project.HitObjectTimeMultiplier;
        HitObjectTimeOffset = project.HitObjectTimeOffset;
        HitObjectVolumeMultiplier = project.HitObjectVolumeMultiplier;
        HitObjectVolumeOffset = project.HitObjectVolumeOffset;
        BookmarkTimeMultiplier = project.BookmarkTimeMultiplier;
        BookmarkTimeOffset = project.BookmarkTimeOffset;
        SbEventTimeMultiplier = project.SbEventTimeMultiplier;
        SbEventTimeOffset = project.SbEventTimeOffset;
        SbSampleTimeMultiplier = project.SbSampleTimeMultiplier;
        SbSampleTimeOffset = project.SbSampleTimeOffset;
        SbSampleVolumeMultiplier = project.SbSampleVolumeMultiplier;
        SbSampleVolumeOffset = project.SbSampleVolumeOffset;
        BreakTimeMultiplier = project.BreakTimeMultiplier;
        BreakTimeOffset = project.BreakTimeOffset;
        VideoTimeMultiplier = project.VideoTimeMultiplier;
        VideoTimeOffset = project.VideoTimeOffset;
        PreviewTimeMultiplier = project.PreviewTimeMultiplier;
        PreviewTimeOffset = project.PreviewTimeOffset;
        ClipProperties = project.ClipProperties;
        EnableFilters = project.EnableFilters;
        MatchFilter = project.MatchFilter?.ToArray() ?? [];
        UnmatchFilter = project.UnmatchFilter?.ToArray() ?? [];
        MinTimeFilter = project.MinTimeFilter;
        MaxTimeFilter = project.MaxTimeFilter;
        SyncTimeFields = project.SyncTimeFields;
    }

    private void ResetMultipliersAndOffsets()
    {
        bool sync = SyncTimeFields;
        SyncTimeFields = false;
        TimingpointOffsetMultiplier = 1;
        TimingpointOffsetOffset = 0;
        TimingpointBpmMultiplier = 1;
        TimingpointBpmOffset = 0;
        TimingpointSvMultiplier = 1;
        TimingpointSvOffset = 0;
        TimingpointIndexMultiplier = 1;
        TimingpointIndexOffset = 0;
        TimingpointVolumeMultiplier = 1;
        TimingpointVolumeOffset = 0;
        HitObjectTimeMultiplier = 1;
        HitObjectTimeOffset = 0;
        HitObjectVolumeMultiplier = 1;
        HitObjectVolumeOffset = 0;
        BookmarkTimeMultiplier = 1;
        BookmarkTimeOffset = 0;
        SbEventTimeMultiplier = 1;
        SbEventTimeOffset = 0;
        SbSampleTimeMultiplier = 1;
        SbSampleTimeOffset = 0;
        SbSampleVolumeMultiplier = 1;
        SbSampleVolumeOffset = 0;
        BreakTimeMultiplier = 1;
        BreakTimeOffset = 0;
        VideoTimeMultiplier = 1;
        VideoTimeOffset = 0;
        PreviewTimeMultiplier = 1;
        PreviewTimeOffset = 0;
        SyncTimeFields = sync;
    }

    private void SetAllTimeMultipliers(double value)
    {
        TimingpointOffsetMultiplier = value;
        HitObjectTimeMultiplier = value;
        BookmarkTimeMultiplier = value;
        SbEventTimeMultiplier = value;
        SbSampleTimeMultiplier = value;
        BreakTimeMultiplier = value;
        VideoTimeMultiplier = value;
        PreviewTimeMultiplier = value;
    }

    private void SetAllTimeOffsets(double value)
    {
        TimingpointOffsetOffset = value;
        HitObjectTimeOffset = value;
        BookmarkTimeOffset = value;
        SbEventTimeOffset = value;
        SbSampleTimeOffset = value;
        BreakTimeOffset = value;
        VideoTimeOffset = value;
        PreviewTimeOffset = value;
    }
}
