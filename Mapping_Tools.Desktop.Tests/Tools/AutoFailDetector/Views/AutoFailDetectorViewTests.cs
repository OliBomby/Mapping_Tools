using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Tools.AutoFail;
using Mapping_Tools.Core.Tools.AutoFail.Models;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tools.AutoFailDetector.ViewModels;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Desktop.Views.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.AutoFailDetector.Views;

[TestClass]
public sealed class AutoFailDetectorViewTests
{
    [TestMethod]
    public async Task RunCommand_WithAutoInsertEnabled_ProductionDialogYesChoiceAppliesFix()
    {
        // Arrange
        RecordingAutoFailService service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        AutoFailDetectorViewModel viewModel = CreateViewModel(service, workspace);
        ClassicDesktopStyleApplicationLifetime lifetime = GetLifetime();
        Window? previousMainWindow = lifetime.MainWindow;
        ShutdownMode previousShutdownMode = lifetime.ShutdownMode;
        lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        MainWindow mainWindow = new();
        lifetime.MainWindow = mainWindow;
        HeadlessViewHost mainHost = HeadlessViewHost.ShowWindow(mainWindow);

        try
        {
            viewModel.GetAutoFailFix = true;
            viewModel.AutoPlaceFix = true;

            // Act
            Task runTask = viewModel.RunCommand.ExecuteAsync(null);
            HeadlessViewHost.PumpDispatcherUntil(() => lifetime.Windows.OfType<MessageDialog>()
                .Any(dialog => dialog.IsVisible));
            MessageDialog dialog = lifetime.Windows.OfType<MessageDialog>().Single(window => window.IsVisible);
            using HeadlessViewHost dialogHost = HeadlessViewHost.Attach(dialog);
            Button yes = dialog.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Content?.ToString() == "Yes");
            string guide = dialog.GetVisualDescendants().OfType<TextBlock>()
                .Single(textBlock => textBlock.Text?.Contains("Live auto-fail fix guide.", StringComparison.Ordinal) == true)
                .Text!;
            dialogHost.Click(yes);
            HeadlessViewHost.PumpDispatcherUntil(() => runTask.IsCompleted);
            await runTask;

            // Assert
            guide.ReplaceLineEndings("\n")
                .Should().Be("Live auto-fail fix guide.\n\nDo you want to use this solution?");
            service.ApplyFixRequestCount.Should().Be(1);
        }
        finally
        {
            mainHost.Dispose();
            lifetime.MainWindow = previousMainWindow;
            lifetime.ShutdownMode = previousShutdownMode;
        }
    }

    [TestMethod]
    public async Task RunCommand_WithAutoInsertEnabled_ProductionDialogCancelChoiceLeavesFixUnapplied()
    {
        // Arrange
        RecordingAutoFailService service = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        AutoFailDetectorViewModel viewModel = CreateViewModel(service, workspace);
        ClassicDesktopStyleApplicationLifetime lifetime = GetLifetime();
        Window? previousMainWindow = lifetime.MainWindow;
        ShutdownMode previousShutdownMode = lifetime.ShutdownMode;
        lifetime.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        MainWindow mainWindow = new();
        lifetime.MainWindow = mainWindow;
        HeadlessViewHost mainHost = HeadlessViewHost.ShowWindow(mainWindow);

        try
        {
            viewModel.GetAutoFailFix = true;
            viewModel.AutoPlaceFix = true;

            // Act
            Task runTask = viewModel.RunCommand.ExecuteAsync(null);
            HeadlessViewHost.PumpDispatcherUntil(() => lifetime.Windows.OfType<MessageDialog>()
                .Any(dialog => dialog.IsVisible));
            MessageDialog dialog = lifetime.Windows.OfType<MessageDialog>().Single(window => window.IsVisible);
            using HeadlessViewHost dialogHost = HeadlessViewHost.Attach(dialog);
            Button cancel = dialog.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Content?.ToString() == "Cancel");
            dialogHost.Click(cancel);
            HeadlessViewHost.PumpDispatcherUntil(() => runTask.IsCompleted);
            await runTask;

            // Assert
            service.ApplyFixRequestCount.Should().Be(0);
            viewModel.HasRun.Should().BeTrue();
        }
        finally
        {
            mainHost.Dispose();
            lifetime.MainWindow = previousMainWindow;
            lifetime.ShutdownMode = previousShutdownMode;
        }
    }

    private static AutoFailDetectorViewModel CreateViewModel(
        RecordingAutoFailService service,
        TestBeatmapWorkspace workspace)
    {
        UserNotificationService notifications = new();
        ToolExecutionService execution = new(notifications, TimeProvider.System);
        return new AutoFailDetectorViewModel(
            service,
            execution,
            workspace,
            new DesktopApplicationSettings(),
            new DialogService(),
            new RecordingPlatformLauncher(),
            notifications);
    }

    private static ClassicDesktopStyleApplicationLifetime GetLifetime()
    {
        return Avalonia.Application.Current?.ApplicationLifetime as ClassicDesktopStyleApplicationLifetime
               ?? throw new InvalidOperationException("The headless test application has no classic desktop lifetime.");
    }

    private sealed class RecordingAutoFailService : IAutoFailService
    {
        public int ApplyFixRequestCount { get; private set; }

        public Task<AutoFailRun> AnalyzeAsync(
            AutoFailServiceOptions options,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new AutoFailRun(new AutoFailAnalysis(true, [1000], [1000, 2000], [1500]), 5000));
        }

        public IEnumerable<AutoFailFixPlan> GetFixPlans(
            AutoFailRun run,
            CancellationToken cancellationToken = default)
        {
            return [new AutoFailFixPlan([1], "Live auto-fail fix guide.")];
        }

        public Task ApplyFixAsync(
            AutoFailRun run,
            AutoFailFixPlan plan,
            bool quickRun = false,
            CancellationToken cancellationToken = default)
        {
            ApplyFixRequestCount++;
            return Task.CompletedTask;
        }
    }
}
