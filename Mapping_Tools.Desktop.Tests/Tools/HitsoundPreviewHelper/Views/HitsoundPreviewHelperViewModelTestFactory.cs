using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Tools.HitsoundPreviewHelper;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.HitsoundPreviewHelper.ViewModels;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Services;
using Mapping_Tools.Desktop.Tools.RhythmGuide.ViewModels;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundPreviewHelper.Views;

internal static class HitsoundPreviewHelperViewModelTestFactory
{
    internal static HitsoundPreviewHelperViewModel CreateForShell()
    {
        IUserNotificationService notifications = new UserNotificationService();
        TestCurrentBeatmapDialogService currentBeatmap = new() { Path = "current.osu" };
        ToolExecutionService execution = new(notifications, TimeProvider.System);
        TestBeatmapWorkspace workspace = new() { QuickRunPath = "current.osu" };
        NullRhythmGuideWindowService windows = new();
        RhythmGuideViewModel rhythmGuide = new(
            new RhythmGuideServiceStub(),
            execution,
            new TestFilePicker(),
            new TestFileRevealService(),
            currentBeatmap,
            workspace,
            windows,
            new TestApplicationDirectories());

        return new HitsoundPreviewHelperViewModel(
            new HitsoundPreviewServiceStub(),
            execution,
            workspace,
            currentBeatmap,
            new DesktopApplicationSettings(),
            notifications,
            windows,
            rhythmGuide,
            new TestApplicationDirectories());
    }

    private sealed class HitsoundPreviewServiceStub : IHitsoundPreviewHelperService
    {
        public Task<HitsoundPreviewHelperResult> ApplyAsync(
            IReadOnlyList<string> paths,
            HitsoundPreviewHelperServiceOptions options,
            bool quickRun = false,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new HitsoundPreviewHelperResult(paths, 0));
        }

        public Task<IReadOnlyList<Vector2>> GetSelectedZonePositionsAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Vector2>>([]);
        }
    }

    private sealed class RhythmGuideServiceStub : IRhythmGuideService
    {
        public Task<RhythmGuideResult> GenerateAsync(
            RhythmGuideServiceOptions.RhythmGuideRunOptions options,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RhythmGuideResult(options.ExportPath, 0, options.ExportMode));
        }
    }

    private sealed class NullRhythmGuideWindowService : IRhythmGuideWindowService
    {
        public void Show(RhythmGuideViewModel viewModel) { }
    }
}
