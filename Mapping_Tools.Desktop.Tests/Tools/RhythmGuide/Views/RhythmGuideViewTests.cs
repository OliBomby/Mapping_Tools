using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Tools.RhythmGuide;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Services;
using Mapping_Tools.Desktop.Tools.RhythmGuide.Views;
using Mapping_Tools.Desktop.Tools.RhythmGuide.ViewModels;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Desktop.Views.Dialogs;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.RhythmGuide.Views;

[TestClass]
public sealed class RhythmGuideViewTests
{
    [TestMethod]
    public async Task UseCurrentSourceButton_WhenLookupFails_ShowsLocalizedActionableErrorInProductionDialog()
    {
        // Arrange
        RecordingCurrentBeatmapLocator locator = new()
        {
            Failure = new InvalidOperationException("The editor state is unavailable."),
        };
        UserNotificationService notifications = new();
        CurrentBeatmapDialogService currentBeatmap = new(
            locator,
            new DialogService(),
            new PhysicalBeatmapsetFileSystem(),
            notifications);
        ToolExecutionService execution = new(notifications, TimeProvider.System);
        RhythmGuideViewModel viewModel = new(
            new RecordingRhythmGuideService(),
            execution,
            new TestFilePicker(),
            new TestFileRevealService(),
            currentBeatmap,
            new TestBeatmapWorkspace(),
            new StubRhythmGuideWindowService(),
            new TestApplicationDirectories());
        RhythmGuideView view = new() { DataContext = viewModel };
        ClassicDesktopStyleApplicationLifetime lifetime =
            Avalonia.Application.Current?.ApplicationLifetime as ClassicDesktopStyleApplicationLifetime
            ?? throw new InvalidOperationException("The headless test application has no classic desktop lifetime.");
        Window? previousMainWindow = lifetime.MainWindow;
        ShutdownMode previousShutdownMode = lifetime.ShutdownMode;
        lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        MainWindow window = new();
        lifetime.MainWindow = window;
        HeadlessViewHost mainHost = HeadlessViewHost.ShowWindow(window);
        using HeadlessViewHost viewHost = HeadlessViewHost.Show(view);

        try
        {
            Button useCurrentSource = viewHost.Window.GetVisualDescendants().OfType<Button>()
                .Where(button => button.Classes.Contains("path-picker"))
                .OrderBy(button => button.Bounds.X)
                .First();

            // Act
            viewHost.Click(useCurrentSource);
            Task lookupTask = viewModel.UseCurrentSourceCommand.ExecutionTask!;
            HeadlessViewHost.PumpDispatcherUntil(() => lifetime.Windows.OfType<MessageDialog>()
                .Any(dialog => dialog.IsVisible));
            MessageDialog dialog = lifetime.Windows.OfType<MessageDialog>().Single(candidate => candidate.IsVisible);
            using HeadlessViewHost dialogHost = HeadlessViewHost.Attach(dialog);
            string message = dialog.GetVisualDescendants().OfType<TextBlock>()
                .Single(textBlock => textBlock.Text == ApplicationStrings.Exception_LiveEditorUnavailable)
                .Text!;
            dialogHost.Click(dialog.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Content?.ToString() == "OK"));
            HeadlessViewHost.PumpDispatcherUntil(() => lookupTask.IsCompleted);
            await lookupTask;

            // Assert
            message.Should().Be(ApplicationStrings.Exception_LiveEditorUnavailable);
            dialog.Title.Should().Be(DesktopStrings.Shell_CurrentBeatmapUnavailable);
            locator.FindCount.Should().Be(1);
            viewModel.SourcePaths.Should().BeEmpty();
        }
        finally
        {
            mainHost.Dispose();
            lifetime.MainWindow = previousMainWindow;
            lifetime.ShutdownMode = previousShutdownMode;
        }
    }

    private sealed class RecordingRhythmGuideService : IRhythmGuideService
    {
        public Task<RhythmGuideResult> GenerateAsync(
            RhythmGuideServiceOptions.RhythmGuideRunOptions options,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new RhythmGuideResult(options.ExportPath, 0, options.ExportMode));
        }
    }

    private sealed class StubRhythmGuideWindowService : IRhythmGuideWindowService
    {
        public void Show(RhythmGuideViewModel viewModel)
        {
        }
    }
}
