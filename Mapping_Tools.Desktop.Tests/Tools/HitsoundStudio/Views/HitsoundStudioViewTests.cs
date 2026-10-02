using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.VisualTree;
using System.Collections.ObjectModel;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.HitsoundStuff;
using Mapping_Tools.Application.Projects.Contracts;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.Models;
using Mapping_Tools.Desktop.Controls;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tests.Tools.HitsoundStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels.Adapters;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.Views;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.HitsoundStudio.Views;

[TestClass]
public sealed class HitsoundStudioViewTests
{
    [TestMethod]
    public void LayersList_Click_ShowsSelectedLayerEditorValues()
    {
        // Arrange
        var selection = HitsoundStudioViewModelTestFactory.CreateMixedSelection();
        using var viewModel = selection.ViewModel;
        ObservableHitsoundLayer first = selection.First;
        HitsoundStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        TextBox nameEditor = host.Find<TextBox>("NameEditor");
        ComboBox sampleSetEditor = host.Find<ComboBox>("SampleSetEditor");

        // Act
        host.Click(FindLayerCell(host, first));

        // Assert
        viewModel.SelectedLayers.Should().ContainSingle().Which.Should().BeSameAs(first);
        nameEditor.Text.Should().Be("first");
        sampleSetEditor.SelectedItem.Should().Be(SampleSet.Normal);
    }

    [TestMethod]
    public void LayersList_ShiftClickWithOneLayerSelected_ShowsMixedEditorValues()
    {
        // Arrange
        var selection = HitsoundStudioViewModelTestFactory.CreateMixedSelection();
        using var viewModel = selection.ViewModel;
        ObservableHitsoundLayer first = selection.First;
        ObservableHitsoundLayer second = selection.Second;
        HitsoundStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        host.Click(FindLayerCell(host, first));
        TextBox nameEditor = host.Find<TextBox>("NameEditor");
        ComboBox sampleSetEditor = host.Find<ComboBox>("SampleSetEditor");

        // Act
        host.Click(FindLayerCell(host, second), KeyModifiers.Shift);

        // Assert
        viewModel.SelectedLayers.Should().Equal(first, second);
        nameEditor.Text.Should().BeEmpty();
        sampleSetEditor.SelectedItem.Should().BeNull();
    }

    [TestMethod]
    public void NameEditor_RealTextInputAndFocusChange_UpdatesEverySelectedLayer()
    {
        // Arrange
        var selection = HitsoundStudioViewModelTestFactory.CreateMixedSelection();
        using var viewModel = selection.ViewModel;
        ObservableHitsoundLayer first = selection.First;
        ObservableHitsoundLayer second = selection.Second;
        HitsoundStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        host.Click(FindLayerCell(host, first));
        host.Click(FindLayerCell(host, second), KeyModifiers.Shift);
        TextBox nameEditor = host.Find<TextBox>("NameEditor");

        // Act
        host.Click(nameEditor);
        host.TypeText("shared from view");
        host.Click(host.Find<TextBox>("BaseBeatmapEditor"));

        // Assert
        viewModel.SelectedLayers.Should().Equal(first, second);
        first.Name.Should().Be("shared from view");
        second.Name.Should().Be("shared from view");
    }

    [TestMethod]
    public async Task ReloadButton_WhenSourceChangesTimes_UpdatesVisibleSampleCount()
    {
        // Arrange
        HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService service = new();
        await using var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            service,
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService());
        ObservableHitsoundLayer layer = CreateLayer("reload me", "reload.wav", [100], ImportType.Hitsounds);
        viewModel.Layers.Add(layer);
        viewModel.SetSelection([layer]);
        service.ReloadAction = layers => layers[0].Times = [200, 400];
        HitsoundStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        ScrollViewer editor = host.Find<ScrollViewer>("LayerEditorScrollViewer");
        editor.Offset = new Vector(0, editor.Extent.Height);
        HeadlessViewHost.RunDispatcherJobs();

        // Act
        host.Click(host.Find<Button>("ReloadButton"));
        Task? reloadTask = viewModel.ReloadCommand.ExecutionTask;
        await viewModel.ReloadCommand.ExecutionTask!;
        HeadlessViewHost.RunDispatcherJobs();

        // Assert
        reloadTask.Should().NotBeNull();
        layer.Times.Should().Equal(200, 400);
        GetCellText(host, layer, 1).Should().Contain("2");
    }

    [TestMethod]
    public void InstallProject_WithLayers_ShowsSelectedLayerEditor()
    {
        // Arrange
        using var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(),
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService());
        HitsoundStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        HitsoundStudioProject project = new();
        project.HitsoundLayers.Add(new HitsoundLayer(
            "installed", SampleSet.Normal, Hitsound.Normal,
            new SampleGeneratingArgs("installed.wav"), new LayerImportArgs())
        {
            Times = [300],
        });

        // Act
        ((IShellProjectFeature<HitsoundStudioProject>)viewModel).Install(project);
        HeadlessViewHost.RunDispatcherJobs();

        // Assert
        viewModel.HasLayers.Should().BeTrue();
        viewModel.Layers.Should().ContainSingle().Which.Name.Should().Be("installed");
        viewModel.SelectedLayers.Should().ContainSingle().Which.Name.Should().Be("installed");
        host.Find<StackPanel>("LayerEditor").IsVisible.Should().BeTrue();
        host.Find<GridSplitter>("LayerEditorSplitter").IsVisible.Should().BeTrue();
    }

    [TestMethod]
    public void InstallProject_WithoutLayers_HidesLayerEditor()
    {
        // Arrange
        using var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(),
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService());
        viewModel.Layers.Add(CreateLayer("existing", "existing.wav", [100]));
        HitsoundStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        HitsoundStudioProject project = new();

        // Act
        ((IShellProjectFeature<HitsoundStudioProject>)viewModel).Install(project);
        HeadlessViewHost.RunDispatcherJobs();

        // Assert
        viewModel.HasLayers.Should().BeFalse();
        viewModel.Layers.Should().BeEmpty();
        host.Find<StackPanel>("LayerEditor").IsVisible.Should().BeFalse();
        host.Find<GridSplitter>("LayerEditorSplitter").IsVisible.Should().BeFalse();
    }

    [TestMethod]
    public async Task AddButton_AcceptingImport_ShowsImportedLayerEditor()
    {
        // Arrange
        HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService service = new()
        {
            ImportResult = [new HitsoundLayer(
                "imported", SampleSet.Normal, Hitsound.Normal,
                new SampleGeneratingArgs("imported.wav"), new LayerImportArgs())
            {
                Times = [400],
            }],
        };
        Window? owner = null;
        await using var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            service,
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService(),
            owner: () => owner!);
        HitsoundStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        owner = host.Window;
        IClassicDesktopStyleApplicationLifetime lifetime =
            (IClassicDesktopStyleApplicationLifetime)global::Avalonia.Application.Current!.ApplicationLifetime!;
        Button addButton = view.GetVisualDescendants().OfType<Button>()
            .Single(button => ReferenceEquals(button.Command, viewModel.AddCommand));

        // Act
        host.Click(addButton);
        HeadlessViewHost.PumpDispatcherUntil(() => lifetime.Windows
            .OfType<HitsoundStudioImportDialog>().Any(dialog => dialog.IsVisible));
        HitsoundStudioImportDialog importDialog = lifetime.Windows
            .OfType<HitsoundStudioImportDialog>().Single(dialog => dialog.IsVisible);
        using HeadlessViewHost importHost = HeadlessViewHost.Attach(importDialog);
        Button[] dialogButtons = importDialog.GetVisualDescendants().OfType<Button>().ToArray();
        Button acceptButton = dialogButtons.Single(button => string.Equals(
            button.Content?.ToString(), "ACCEPT", StringComparison.OrdinalIgnoreCase));
        importHost.Click(acceptButton);
        await viewModel.AddCommand.ExecutionTask!;
        HeadlessViewHost.RunDispatcherJobs();

        // Assert
        service.ImportResult.Should().ContainSingle();
        viewModel.Layers.Should().ContainSingle().Which.Name.Should().Be("imported");
        viewModel.SelectedLayers.Should().ContainSingle().Which.Name.Should().Be("imported");
        viewModel.HasLayers.Should().BeTrue();
        importDialog.IsVisible.Should().BeFalse();
        host.Find<StackPanel>("LayerEditor").IsVisible.Should().BeTrue();
        host.Find<GridSplitter>("LayerEditorSplitter").IsVisible.Should().BeTrue();
    }

    [TestMethod]
    public void LayersList_WhenEmpty_HelpRemainsAccessibleAfterResize()
    {
        // Arrange
        using var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(),
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService());
        HitsoundStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        view.Width = 640;
        HeadlessViewHost.RunDispatcherJobs();
        Button helpButton = FindHelpButton(view);

        // Act
        host.Click(helpButton);

        // Assert
        host.Find<StackPanel>("LayerEditor").IsVisible.Should().BeFalse();
        host.Find<GridSplitter>("LayerEditorSplitter").IsVisible.Should().BeFalse();
        helpButton.IsEffectivelyVisible.Should().BeTrue();
        helpButton.Bounds.Width.Should().BeGreaterThan(0);
        ((Flyout)helpButton.Flyout!).IsOpen.Should().BeTrue();
        viewModel.EditorColumnWidth.Value.Should().Be(0);
    }

    [TestMethod]
    public async Task RemoveButton_WhenLastLayerIsSelected_CollapsesEditorAndSplitter()
    {
        // Arrange
        TestDialogService dialogs = new() { BooleanResult = true };
        await using var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(),
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService(),
            dialogs: dialogs);
        ObservableHitsoundLayer onlyLayer = CreateLayer("only", "only.wav", [100]);
        viewModel.Layers.Add(onlyLayer);
        HitsoundStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        host.Click(FindLayerCell(host, onlyLayer));

        // Act
        host.Click(host.Find<Button>("RemoveButton"));
        await HeadlessViewHost.DrainAsync(() => viewModel.Layers.Count == 0);

        // Assert
        viewModel.HasLayers.Should().BeFalse();
        host.Find<StackPanel>("LayerEditor").IsVisible.Should().BeFalse();
        host.Find<GridSplitter>("LayerEditorSplitter").IsVisible.Should().BeFalse();
        viewModel.EditorColumnWidth.Value.Should().Be(0);
    }

    [TestMethod]
    public void RaiseAndLowerButtons_RepeatedMoves_KeepSelectedLayerAndRespectBoundaries()
    {
        // Arrange
        using var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(),
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService());
        ObservableHitsoundLayer first = CreateLayer("first", "first.wav", [100]);
        ObservableHitsoundLayer middle = CreateLayer("middle", "middle.wav", [200]);
        ObservableHitsoundLayer last = CreateLayer("last", "last.wav", [300]);
        viewModel.Layers.Add(first);
        viewModel.Layers.Add(middle);
        viewModel.Layers.Add(last);
        HitsoundStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);

        // Act
        host.Click(FindLayerCell(host, middle));
        host.Click(host.Find<Button>("RaiseButton"));
        host.Click(host.Find<Button>("RaiseButton"));
        host.Click(host.Find<Button>("RaiseButton"));
        host.Click(host.Find<Button>("LowerButton"));
        host.Click(host.Find<Button>("LowerButton"));

        // Assert
        viewModel.Layers.Should().Equal(first, last, middle);
        viewModel.SelectedLayers.Should().ContainSingle().Which.Should().BeSameAs(middle);
    }

    [TestMethod]
    public async Task RemoveButton_WithSelectedLayer_RemovesOnlySelectionAndSelectsAdjacentLayer()
    {
        // Arrange
        TestDialogService dialogs = new() { BooleanResult = true };
        await using var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(),
            new HitsoundStudioViewModelTestFactory.RecordingAudioGenerator(),
            new HitsoundStudioViewModelTestFactory.RecordingPlaybackService(),
            dialogs: dialogs);
        ObservableHitsoundLayer first = CreateLayer("first", "first.wav", [100]);
        ObservableHitsoundLayer second = CreateLayer("second", "second.wav", [200]);
        viewModel.Layers.Add(first);
        viewModel.Layers.Add(second);
        HitsoundStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        host.Click(FindLayerCell(host, first));

        // Act
        host.Click(host.Find<Button>("RemoveButton"));
        await HeadlessViewHost.DrainAsync(() => viewModel.Layers.Count == 1);

        // Assert
        viewModel.Layers.Should().ContainSingle().Which.Should().BeSameAs(second);
        viewModel.SelectedLayers.Should().ContainSingle().Which.Should().BeSameAs(second);
    }

    [TestMethod]
    public async Task LayersList_DoubleClickSelectedRow_PreviewsDoubleClickedLayerOnce()
    {
        // Arrange
        HitsoundStudioViewModelTestFactory.RecordingAudioGenerator generator = new(blockFirstGeneration: false);
        HitsoundStudioViewModelTestFactory.RecordingPlaybackService playback = new();
        await using var viewModel = HitsoundStudioViewModelTestFactory.CreateViewModel(
            new HitsoundStudioViewModelTestFactory.RecordingHitsoundStudioService(), generator, playback);
        ObservableHitsoundLayer other = CreateLayer("other", "other.wav", [100]);
        ObservableHitsoundLayer target = CreateLayer("target", "target.wav", [200]);
        viewModel.Layers.Add(other);
        viewModel.Layers.Add(target);
        HitsoundStudioView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        host.Click(FindLayerCell(host, other));

        // Act
        host.DoubleClick(FindLayerCell(host, target));
        await HeadlessViewHost.DrainAsync(() => playback.Sessions.Count > 0);
        await viewModel.PreviewCommand.ExecutionTask!;

        // Assert
        viewModel.SelectedLayers.Should().ContainSingle().Which.Should().BeSameAs(target);
        generator.Requests.Should().ContainSingle().Which.Sample.Path.Should().Be("target.wav");
        playback.Sessions.Should().ContainSingle();
    }

    private static Button FindHelpButton(HitsoundStudioView view)
    {
        ToolViewHeader header = view.GetVisualDescendants().OfType<ToolViewHeader>().Single();
        return header.GetVisualDescendants().OfType<Button>()
                   .First(button => AutomationProperties.GetName(button) == "Tool information");
    }

    private static string[] GetCellText(HeadlessViewHost host, ObservableHitsoundLayer layer, int column)
    {
        Control cell = GetLayerCells(host, layer)[column];
        return cell.GetVisualDescendants().OfType<TextBlock>().Select(textBlock => textBlock.Text ?? string.Empty).ToArray();
    }

    private static Control[] GetLayerCells(HeadlessViewHost host, ObservableHitsoundLayer layer)
    {
        Control row = host.Find<MaterialGridListView>("LayersList").GetVisualDescendants().OfType<Control>()
            .First(control => control.GetType().Name == "TableViewRow" && ReferenceEquals(control.DataContext, layer));
        return row.GetVisualDescendants().OfType<Control>()
            .Where(control => control.GetType().Name == "TableViewCell")
            .OrderBy(control => control.Bounds.X)
            .ToArray();
    }

    private static Control FindLayerCell(HeadlessViewHost host, ObservableHitsoundLayer layer)
    {
        return GetLayerCells(host, layer)[0];
    }

    private static ObservableHitsoundLayer CreateLayer(
        string name,
        string samplePath,
        List<double> times,
        ImportType importType = ImportType.None)
    {
        ObservableHitsoundLayer layer = new(new HitsoundLayer(name, SampleSet.Normal, Hitsound.Normal,
            new SampleGeneratingArgs(samplePath), new LayerImportArgs(importType)));
        layer.Times = times;
        return layer;
    }
}
