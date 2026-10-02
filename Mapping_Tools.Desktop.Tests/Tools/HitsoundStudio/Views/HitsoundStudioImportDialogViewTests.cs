using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.VisualTree;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Desktop.Services.Dialogs;
using Mapping_Tools.Application.Tools.HitsoundStudio.Models;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.Views;
using Mapping_Tools.Desktop.Views;
using Mapping_Tools.Desktop.Views.Dialogs;
using Mapping_Tools.Infrastructure.Files;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundStudio.Views;

[TestClass]
public sealed class HitsoundStudioImportDialogViewTests
{
    [DataTestMethod]
    [DataRow(0, ImportType.None)]
    [DataRow(1, ImportType.Stack)]
    [DataRow(2, ImportType.Hitsounds)]
    [DataRow(3, ImportType.MIDI)]
    [DataRow(4, ImportType.Storyboard)]
    public void ImportType_SelectedTab_TracksVisibleLegacyImportTab(int tabIndex, ImportType expectedType)
    {
        // Arrange
        HitsoundStudioImportDialogViewModel viewModel = CreateViewModel();
        HitsoundStudioImportDialog dialog = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(dialog);
        string header = tabIndex switch
        {
            0 => "Simple layer",
            1 => "Import layer stack",
            2 => "Import hitsounds",
            3 => "Import MIDI",
            4 => "Import storyboard",
            _ => throw new ArgumentOutOfRangeException(nameof(tabIndex)),
        };

        // Act
        host.Click(host.Window.GetVisualDescendants().OfType<TextBlock>()
            .Single(textBlock => textBlock.Text == header));

        // Assert
        viewModel.SelectedTabIndex.Should().Be(tabIndex);
        viewModel.ImportType.Should().Be(expectedType);
        FindVisibleImportControl(dialog, tabIndex).IsEffectivelyVisible.Should().BeTrue();
    }

    [TestMethod]
    public void AcceptCommand_RequiredSourceMissing_LeavesDialogOpen()
    {
        // Arrange
        HitsoundStudioImportDialogViewModel viewModel = CreateViewModel();
        viewModel.ImportType = ImportType.Hitsounds;
        int closeCount = 0;
        viewModel.Close = _ => closeCount++;
        HitsoundStudioImportDialog dialog = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(dialog);
        Button accept = FindButton(host, "ACCEPT");
        TextBox source = FindVisibleTextBox(dialog, "The beatmap to import hitsounds from.");

        // Act
        host.Click(accept);

        // Assert
        closeCount.Should().Be(0);
        source.Text.Should().BeEmpty();
    }

    [TestMethod]
    public void AcceptCommand_ValidMidiImport_ReturnsTypedFieldsAndTrimmedSource()
    {
        // Arrange
        HitsoundStudioImportDialogViewModel viewModel = CreateViewModel();
        viewModel.ImportType = ImportType.MIDI;
        object? result = null;
        viewModel.Close = value => result = value;
        HitsoundStudioImportDialog dialog = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(dialog);
        Button accept = FindButton(host, "ACCEPT");
        TextBox name = FindVisibleTextBox(dialog, "The name prefix for imported MIDI layers.");
        TextBox midiPath = FindVisibleTextBox(dialog, "The MIDI file to import.");
        TextBox offset = FindVisibleTextBox(dialog, "The start time offset of the MIDI in milliseconds.");
        CheckBox discriminateInstruments = FindVisibleCheckBox(dialog, "Make separate hitsound layers for different instruments.");
        CheckBox discriminateLengths = FindVisibleCheckBox(dialog, "Make separate hitsound layers for different note lengths.");

        // Act
        TypeText(host, name, "MIDI layer");
        TypeText(host, midiPath, "  C:/Maps/source.mid  ");
        TypeText(host, offset, "125");
        host.Click(discriminateInstruments, new Point(8, discriminateInstruments.Bounds.Height / 2));
        host.Click(discriminateLengths, new Point(8, discriminateLengths.Bounds.Height / 2));
        TextBox roughness = dialog.GetVisualDescendants().OfType<TextBox>()
            .Single(textBox => textBox.IsEffectivelyVisible && textBox.Text == "2");
        TypeText(host, roughness, "3.5");
        host.Click(accept);

        // Assert
        var request = result.Should().BeOfType<HitsoundStudioImportRequest>().Subject;
        request.ImportType.Should().Be(ImportType.MIDI);
        request.Name.Should().Be("MIDI layer");
        request.Paths.Should().Equal("C:/Maps/source.mid");
        request.Offset.Should().Be(125);
        request.DiscriminateInstruments.Should().BeFalse();
        request.DiscriminateLengths.Should().BeTrue();
        request.LengthRoughness.Should().Be(3.5);
    }

    [TestMethod]
    public void CancelCommand_RealCancelButton_ClosesWithoutRequest()
    {
        // Arrange
        HitsoundStudioImportDialogViewModel viewModel = CreateViewModel();
        object? result = "unchanged";
        viewModel.Close = value => result = value;
        HitsoundStudioImportDialog dialog = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(dialog);
        Button cancel = FindButton(host, "CANCEL");

        // Act
        host.Click(cancel);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public void SelectedMapPrefill_StackTab_ShowsPathWithoutFetchingCurrentMap()
    {
        // Arrange
        TestCurrentBeatmapDialogService currentBeatmap = new();
        TestBeatmapWorkspace workspace = new();
        workspace.SetSelection(["selected.osu"]);
        HitsoundStudioImportDialogViewModel viewModel = new(
            "Layer", currentBeatmap, workspace, new TestFilePicker());
        viewModel.ImportType = ImportType.Stack;
        HitsoundStudioImportDialog dialog = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(dialog);

        // Act
        TextBox prefilledPath = dialog.GetVisualDescendants().OfType<TextBox>()
            .Single(textBox => textBox.Text == "selected.osu" && textBox.IsEffectivelyVisible);

        // Assert
        prefilledPath.Text.Should().Be("selected.osu");
        prefilledPath.IsEffectivelyVisible.Should().BeTrue();
        currentBeatmap.FetchCount.Should().Be(0);
    }

    [TestMethod]
    public async Task BrowseButton_StackTab_UsesMapPickerLocationAndBeatmapFilter()
    {
        // Arrange
        TestFilePicker picker = new() { OpenFiles = ["target.osu"] };
        TestBeatmapWorkspace workspace = new() { BeatmapPickerStartLocation = @"C:\Maps" };
        HitsoundStudioImportDialogViewModel viewModel = new(
            "Layer", new TestCurrentBeatmapDialogService(), workspace, picker)
        {
            ImportType = ImportType.Stack,
        };
        HitsoundStudioImportDialog dialog = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(dialog);
        Button browse = dialog.GetVisualDescendants().OfType<Button>().Single(button =>
            ToolTip.GetTip(button)?.ToString() == "Select a beatmap with File Explorer.");

        // Act
        host.Click(browse);
        await viewModel.PickSourceCommand.ExecutionTask!;

        // Assert
        viewModel.BeatmapPath.Should().Be("target.osu");
        dialog.GetVisualDescendants().OfType<TextBox>()
            .Single(textBox => textBox.Text == "target.osu" && textBox.IsEffectivelyVisible)
            .IsEffectivelyVisible.Should().BeTrue();
        picker.LastOpenRequest!.SuggestedStartLocation.Should().Be(@"C:\Maps");
        picker.LastOpenRequest.Filters.Should().ContainSingle(filter =>
            filter.Patterns.Any(pattern => pattern == "*.osu"));
    }

    [TestMethod]
    public async Task LoadSourceButton_WhenLookupFails_ShowsOriginalErrorInProductionDialog()
    {
        // Arrange
        RecordingCurrentBeatmapLocator locator = new()
        {
            Failure = new InvalidOperationException("The editor state is unavailable."),
        };
        CurrentBeatmapDialogService currentBeatmap = new(
            locator,
            new DialogService(),
            new PhysicalBeatmapsetFileSystem(),
            new UserNotificationService());
        HitsoundStudioImportDialogViewModel viewModel = new(
            "Layer", currentBeatmap, new TestBeatmapWorkspace(), new TestFilePicker());
        viewModel.ImportType = ImportType.Stack;
        HitsoundStudioImportDialog dialog = new() { DataContext = viewModel };
        MainWindow mainWindow = new();
        IClassicDesktopStyleApplicationLifetime lifetime =
            global::Avalonia.Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime
            ?? throw new InvalidOperationException("The headless test application has no classic desktop lifetime.");
        Window? previousMainWindow = lifetime.MainWindow;
        lifetime.MainWindow = mainWindow;
        HeadlessViewHost mainHost = HeadlessViewHost.ShowWindow(mainWindow);
        using HeadlessViewHost importHost = HeadlessViewHost.ShowWindow(dialog);
        Button loadSource = dialog.GetVisualDescendants().OfType<Button>().Single(button =>
            ToolTip.GetTip(button)?.ToString() == "Fetch the selected beatmap from your osu! client.");

        try
        {
            // Act
            importHost.Click(loadSource);
            Task loadTask = viewModel.LoadSourceCommand.ExecutionTask!;
            HeadlessViewHost.PumpDispatcherUntil(() => lifetime.Windows.OfType<MessageDialog>()
                .Any(messageDialog => messageDialog.IsVisible));
            MessageDialog message = lifetime.Windows.OfType<MessageDialog>().Single(candidate => candidate.IsVisible);
            using HeadlessViewHost messageHost = HeadlessViewHost.Attach(message);
            string displayedError = message.GetVisualDescendants().OfType<TextBlock>()
                .Single(textBlock => textBlock.Text == "The editor state is unavailable.").Text!;
            messageHost.Click(message.GetVisualDescendants().OfType<Button>()
                .Single(button => button.Content?.ToString() == "OK"));
            HeadlessViewHost.PumpDispatcherUntil(() => loadTask.IsCompleted);
            await loadTask;

            // Assert
            displayedError.Should().Be("The editor state is unavailable.");
            message.Title.Should().Be("Current beatmap unavailable");
            locator.FindCount.Should().Be(1);
            viewModel.BeatmapPath.Should().BeEmpty();
        }
        finally
        {
            mainHost.Dispose();
            lifetime.MainWindow = previousMainWindow;
        }
    }

    private static HitsoundStudioImportDialogViewModel CreateViewModel()
    {
        return new HitsoundStudioImportDialogViewModel(
            "Layer",
            new TestCurrentBeatmapDialogService(),
            new TestBeatmapWorkspace(),
            new TestFilePicker());
    }

    private static Button FindButton(HeadlessViewHost host, string content)
    {
        Button[] buttons = host.Window.GetVisualDescendants().OfType<Button>().ToArray();
        return buttons.SingleOrDefault(button => string.Equals(
                   button.Content?.ToString(), content, StringComparison.OrdinalIgnoreCase))
               ?? throw new InvalidOperationException(
                   $"Could not find '{content}'. Buttons: {string.Join(", ", buttons.Select(button => button.Content?.ToString() ?? string.Join(" ", button.GetVisualDescendants().OfType<TextBlock>().Select(text => text.Text))))}");
    }

    private static Control FindVisibleImportControl(HitsoundStudioImportDialog dialog, int tabIndex)
    {
        return tabIndex switch
        {
            0 => FindVisibleTextBox(dialog, "Audio file or SoundFont used by the new layer."),
            1 => FindVisibleTextBox(dialog, "The X coordinate of the stack in the source beatmap which dictates all the times when the sound has to be played."),
            2 => FindVisibleCheckBox(dialog, "Canonicalize identical audio files in the source mapset."),
            3 => FindVisibleTextBox(dialog, "The MIDI file to import."),
            4 => FindVisibleCheckBox(dialog, "Remove duplicate values in the imported times."),
            _ => throw new ArgumentOutOfRangeException(nameof(tabIndex)),
        };
    }

    private static TextBox FindVisibleTextBox(HitsoundStudioImportDialog dialog, string tip)
    {
        return dialog.GetVisualDescendants().OfType<TextBox>().Single(textBox =>
            textBox.IsEffectivelyVisible && string.Equals(ToolTip.GetTip(textBox)?.ToString(), tip, StringComparison.Ordinal));
    }

    private static CheckBox FindVisibleCheckBox(HitsoundStudioImportDialog dialog, string tip)
    {
        return dialog.GetVisualDescendants().OfType<CheckBox>().Single(checkBox =>
            checkBox.IsEffectivelyVisible && string.Equals(ToolTip.GetTip(checkBox)?.ToString(), tip, StringComparison.Ordinal));
    }

    private static void TypeText(HeadlessViewHost host, TextBox textBox, string value)
    {
        host.Click(textBox);
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText(value);
    }
}
