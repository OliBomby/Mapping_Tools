using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.ToolExecution.Models;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Application.Workspace.Contracts;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.ViewModels;

namespace Mapping_Tools.SamplePlugin;

/// <summary>
///     Coordinates the sample tool's persisted setting, QuickRun path, and single-run execution.
/// </summary>
public sealed partial class SampleToolViewModel : SingleRunToolViewModel,
    IQuickRun,
    IShellProjectFeature<SampleToolProject>
{
    private readonly ProjectDefinition<SampleToolProject> definition = new(
        "samplepluginproject.json",
        "Sample Plugin Projects",
        static () => new SampleToolProject(),
        "sample-plugin-project.json");
    private readonly SampleToolService sampleTool;
    private readonly IBeatmapWorkspace workspace;

    /// <summary>
    ///     Creates the sample tool presentation model.
    /// </summary>
    /// <param name="sampleTool">Applies the sample edit through the shared beatmap gateway.</param>
    /// <param name="execution">Coordinates single-run execution, cancellation, backup notifications, and reload.</param>
    /// <param name="workspace">Supplies the beatmaps selected by the Mapping Tools shell.</param>
    public SampleToolViewModel(
        SampleToolService sampleTool,
        IToolExecutionService execution,
        IBeatmapWorkspace workspace)
        : base(execution, SampleToolDefinition.Definition)
    {
        this.sampleTool = sampleTool ?? throw new ArgumentNullException(nameof(sampleTool));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
    }

    /// <summary>
    ///     Gets or sets the one tag appended to selected beatmaps' metadata.
    /// </summary>
    [ObservableProperty]
    [NotifyDataErrorInfo]
    [Required(ErrorMessage = "Enter a tag.")]
    [RegularExpression(@"^\S+$", ErrorMessage = "The tag must not contain spaces.")]
    public partial string Tag { get; set; } = "sample-plugin";

    /// <inheritdoc />
    public async Task RunQuickAsync(CancellationToken cancellationToken)
    {
        await RunWithStateAsync(() => RunPathsAsync(
            GetCurrentPaths(),
            true,
            cancellationToken));
    }

    /// <inheritdoc />
    ProjectDefinition<SampleToolProject> IShellProjectFeature<SampleToolProject>.ProjectDefinition => definition;

    /// <inheritdoc />
    SampleToolProject IShellProjectFeature<SampleToolProject>.Snapshot()
    {
        return new SampleToolProject { Tag = Tag };
    }

    /// <inheritdoc />
    void IShellProjectFeature<SampleToolProject>.Install(SampleToolProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        Tag = project.Tag;
    }

    /// <inheritdoc />
    protected override Task RunCoreAsync()
    {
        return RunPathsAsync(workspace.SelectedPaths, false, CancellationToken.None);
    }

    private async Task RunPathsAsync(
        IReadOnlyList<string> paths,
        bool quick,
        CancellationToken cancellationToken)
    {
        if (paths.Count == 0) return;

        string tag = Tag;
        await Execution.ExecuteAsync(
            new ToolExecutionRequest<int>(
                Tool.Id,
                Tool.DisplayName,
                async context =>
                {
                    Progress<double> progress = new(value =>
                        context.ReportProgress(value, "Updating beatmaps"));
                    int changedCount = await sampleTool
                        .AddTagAsync(paths, tag, progress, context.CancellationToken)
                        .ConfigureAwait(false);
                    return new ToolExecutionOutput<int>(
                        changedCount,
                        quick ? null : Summarize(changedCount, paths.Count, tag),
                        quick && changedCount > 0);
                }),
            CreateProgress(),
            cancellationToken);
    }

    private IReadOnlyList<string> GetCurrentPaths()
    {
        string? path = workspace.SelectedPaths.FirstOrDefault();
        return string.IsNullOrWhiteSpace(path) ? [] : [path];
    }

    private static string Summarize(int changedCount, int pathCount, string tag)
    {
        return changedCount == 0
            ? $"The '{tag}' tag was already present in the selected beatmap{(pathCount == 1 ? string.Empty : "s")}."
            : $"Added the '{tag}' tag to {changedCount} of {pathCount} selected beatmap{(pathCount == 1 ? string.Empty : "s")}.";
    }
}
