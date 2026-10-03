using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.VisualTree;
using Material.Icons.Avalonia;
using Mapping_Tools.Application.Execution.ToolExecution;
using Mapping_Tools.Application.Execution.UserNotification;
using Mapping_Tools.Application.Settings.Models;
using Mapping_Tools.Application.Tools.TumourGenerator;
using Mapping_Tools.Application.Tools.TumourGenerator.Models;
using Mapping_Tools.Core.BeatmapHelper.Enums;
using Mapping_Tools.Core.Tools.TumourGenerator.Models;
using Mapping_Tools.Desktop.Models;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Desktop.Tools.TumourGenerator.Controls;
using Mapping_Tools.Desktop.Tools.TumourGenerator.Models;
using Mapping_Tools.Desktop.Tools.TumourGenerator.ViewModels;
using Mapping_Tools.Desktop.Tools.TumourGenerator.ViewModels.Adapters;
using Mapping_Tools.Desktop.Tools.TumourGenerator.Views;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.TumourGenerator.Views;

[TestClass]
public sealed class TumourGeneratorViewTests
{
    [TestMethod]
    public async Task AttachedView_ProjectSnapshotRoundTripBeforePreview_PreservesAdvancedRangeAndPreviewObject()
    {
        // Arrange
        using TumourGeneratorViewModel viewModel = Create(activate: false);
        TumourGeneratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        viewModel.CurrentLayer!.TumourStart = 35.39506172839506;
        viewModel.CurrentLayer.TumourEnd = 256;
        viewModel.CurrentLayer.UseAbsoluteRange = true;
        viewModel.AdvancedOptions = true;
        viewModel.PreviewHitObject = DecodeHitObject("32,64,100,2,0,L|200:64,1,168");
        IShellProjectFeature<TumourGeneratorProject> feature = viewModel;
        LegacyProjectJsonSerializer serializer = new();

        // Act
        TumourGeneratorProject beforePreviewSnapshot = feature.Snapshot();
        string savedProject = serializer.Serialize(beforePreviewSnapshot);
        TumourGeneratorProject restoredProject = serializer.Deserialize<TumourGeneratorProject>(savedProject);
        feature.Install(restoredProject);
        await HeadlessViewHost.DrainAsync(() =>
            host.Find<Slider>("TumourEndSlider").Maximum >= 256
            && Math.Abs(host.Find<Slider>("TumourEndSlider").Value - 256) < 0.001);

        // Assert
        viewModel.TumouredPreviewHitObject.Should().BeNull();
        host.Find<ToggleSwitch>("AdvancedOptionsToggle").IsChecked.Should().BeTrue();
        host.Find<Slider>("TumourStartSlider").Value.Should().Be(35.39506172839506);
        host.Find<Slider>("TumourEndSlider").Value.Should().Be(256);
        host.Find<Slider>("TumourEndSlider").Maximum.Should().BeGreaterThanOrEqualTo(256);
        EncodeHitObject(viewModel.PreviewHitObject).Should().Contain("32,64,100");
    }

    [TestMethod]
    public void RemoveCommand_FromAttachedViewWithOneLayer_LeavesLayerInPlace()
    {
        // Arrange
        using TumourGeneratorViewModel viewModel = Create(activate: false);
        TumourGeneratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        CommandBar commandBar = host.Find<CommandBar>("LayerCommandBar");

        // Act
        ClickCommand(host, commandBar, viewModel.RemoveCommand);

        // Assert
        viewModel.TumourLayers.Should().ContainSingle();
        viewModel.CurrentLayer.Should().BeSameAs(viewModel.TumourLayers[0]);
    }

    [TestMethod]
    public void AddCommand_FromAttachedView_AddsAndSelectsLayer()
    {
        // Arrange
        using TumourGeneratorViewModel viewModel = Create(activate: false);
        TumourGeneratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        CommandBar commandBar = host.Find<CommandBar>("LayerCommandBar");

        // Act
        ClickCommand(host, commandBar, viewModel.AddCommand);
        ObservableTumourLayer added = viewModel.CurrentLayer!;

        // Assert
        viewModel.TumourLayers.Should().HaveCount(2);
        viewModel.TumourLayers.Should().Contain(added);
        viewModel.CurrentLayer.Should().BeSameAs(added);
        host.Find<ListBox>("TumourLayerList").SelectedIndex.Should().Be(1);
    }

    [TestMethod]
    public void CopyCommand_FromAttachedView_CopiesAndSelectsLayer()
    {
        // Arrange
        using TumourGeneratorViewModel viewModel = Create(activate: false);
        viewModel.CurrentLayer!.Name = "Original";
        TumourGeneratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        CommandBar commandBar = host.Find<CommandBar>("LayerCommandBar");

        // Act
        ClickCommand(host, commandBar, viewModel.CopyCommand);
        ObservableTumourLayer copied = viewModel.CurrentLayer!;

        // Assert
        viewModel.TumourLayers.Should().HaveCount(2);
        copied.Should().NotBeSameAs(viewModel.TumourLayers[0]);
        copied.Name.Should().Be("Original (Copy)");
        viewModel.CurrentLayer.Should().BeSameAs(copied);
        host.Find<ListBox>("TumourLayerList").SelectedIndex.Should().Be(1);
    }

    [TestMethod]
    public void RemoveCommand_FromAttachedViewWithMultipleLayers_RemovesSelectedLayer()
    {
        // Arrange
        using TumourGeneratorViewModel viewModel = Create(activate: false);
        TumourGeneratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        CommandBar commandBar = host.Find<CommandBar>("LayerCommandBar");
        ClickCommand(host, commandBar, viewModel.AddCommand);
        ObservableTumourLayer selected = viewModel.CurrentLayer!;

        // Act
        ClickCommand(host, commandBar, viewModel.RemoveCommand);

        // Assert
        viewModel.TumourLayers.Should().ContainSingle();
        viewModel.TumourLayers.Should().NotContain(selected);
        viewModel.CurrentLayer.Should().BeSameAs(viewModel.TumourLayers[0]);
    }

    [TestMethod]
    public void ReorderCommands_FromAttachedView_MoveSelectedLayerAndKeepSelection()
    {
        // Arrange
        using TumourGeneratorViewModel viewModel = Create(activate: false);
        TumourGeneratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        CommandBar commandBar = host.Find<CommandBar>("LayerCommandBar");
        ClickCommand(host, commandBar, viewModel.AddCommand);
        ClickCommand(host, commandBar, viewModel.AddCommand);
        ObservableTumourLayer selected = viewModel.CurrentLayer!;
        ListBox layers = host.Find<ListBox>("TumourLayerList");
        string[] commandLabels = commandBar.VisiblePrimaryCommands.OfType<CommandBarButton>()
            .Concat(commandBar.OverflowItems.OfType<CommandBarButton>())
            .Select(button => button.Label!).ToArray();

        // Act
        ClickCommand(host, commandBar, viewModel.LowerCommand);
        int indexAfterMovingDown = viewModel.CurrentLayerIndex;
        bool selectedLayerMovedDown = ReferenceEquals(viewModel.TumourLayers[1], selected);
        ClickCommand(host, commandBar, viewModel.RaiseCommand);
        int indexAfterMovingUp = viewModel.CurrentLayerIndex;
        bool selectedLayerMovedUp = ReferenceEquals(viewModel.TumourLayers[2], selected);

        // Assert
        commandLabels.Should().Contain("Move layer down");
        indexAfterMovingDown.Should().Be(1);
        selectedLayerMovedDown.Should().BeTrue();
        indexAfterMovingUp.Should().Be(2);
        selectedLayerMovedUp.Should().BeTrue();
        viewModel.CurrentLayer.Should().BeSameAs(selected);
        layers.SelectedItem.Should().BeSameAs(selected);
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void ReorderCommand_AtLayerBoundary_LeavesOrderAndSelectionUnchanged(bool moveLastLayerUp)
    {
        // Arrange
        using TumourGeneratorViewModel viewModel = Create(activate: false);
        TumourGeneratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        CommandBar commandBar = host.Find<CommandBar>("LayerCommandBar");
        ClickCommand(host, commandBar, viewModel.AddCommand);
        ClickCommand(host, commandBar, viewModel.AddCommand);
        int boundaryIndex = moveLastLayerUp ? viewModel.TumourLayers.Count - 1 : 0;
        viewModel.CurrentLayerIndex = boundaryIndex;
        ObservableTumourLayer selected = viewModel.CurrentLayer!;
        ObservableTumourLayer[] originalOrder = viewModel.TumourLayers.ToArray();
        ListBox layers = host.Find<ListBox>("TumourLayerList");
        string expectedCommandLabel = moveLastLayerUp ? "Move layer up" : "Move layer down";
        System.Windows.Input.ICommand command = moveLastLayerUp ? viewModel.RaiseCommand : viewModel.LowerCommand;

        // Act
        ClickCommand(host, commandBar, command);
        ObservableTumourLayer[] orderAfterCommand = viewModel.TumourLayers.ToArray();
        int indexAfterCommand = viewModel.CurrentLayerIndex;

        // Assert
        commandBar.VisiblePrimaryCommands.OfType<CommandBarButton>()
            .Concat(commandBar.OverflowItems.OfType<CommandBarButton>())
            .Select(button => button.Label).Should().Contain(expectedCommandLabel);
        orderAfterCommand.Should().Equal(originalOrder);
        indexAfterCommand.Should().Be(boundaryIndex);
        viewModel.CurrentLayer.Should().BeSameAs(selected);
        layers.SelectedItem.Should().BeSameAs(selected);
    }

    [TestMethod]
    public void LayerCommands_FromAttachedView_QuickReorderDoesNotOverwriteLaterPointerSelection()
    {
        // Arrange
        using TumourGeneratorViewModel viewModel = Create(activate: false);
        TumourGeneratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        CommandBar commandBar = host.Find<CommandBar>("LayerCommandBar");
        ClickCommand(host, commandBar, viewModel.AddCommand);
        ClickCommand(host, commandBar, viewModel.AddCommand);
        ObservableTumourLayer previouslySelected = viewModel.CurrentLayer!;
        ObservableTumourLayer laterSelection = viewModel.TumourLayers[0];
        ListBox layers = host.Find<ListBox>("TumourLayerList");

        // Act
        ClickCommand(host, commandBar, viewModel.LowerCommand);
        int indexAfterLower = viewModel.CurrentLayerIndex;
        bool selectedMovedDown = ReferenceEquals(viewModel.TumourLayers[1], previouslySelected);
        layers.ScrollIntoView(laterSelection);
        HeadlessViewHost.RunDispatcherJobs();
        ListBoxItem laterRow = layers.GetVisualDescendants().OfType<ListBoxItem>()
            .Single(item => ReferenceEquals(item.DataContext, laterSelection));
        MaterialIcon laterRowIcon = laterRow.GetVisualDescendants().OfType<MaterialIcon>().Single();
        bool laterRowIconVisible = laterRowIcon.IsEffectivelyVisible;
        double laterRowIconWidth = laterRowIcon.Bounds.Width;
        host.Click(laterRowIcon);
        HeadlessViewHost.RunDispatcherJobs();

        // Assert
        indexAfterLower.Should().Be(1);
        selectedMovedDown.Should().BeTrue();
        laterRowIconVisible.Should().BeTrue();
        laterRowIconWidth.Should().BeGreaterThan(0);
        viewModel.CurrentLayer.Should().BeSameAs(laterSelection);
        viewModel.CurrentLayerIndex.Should().Be(0);
        layers.SelectedItem.Should().BeSameAs(laterSelection);
    }

    [TestMethod]
    public void ValueAndGraphModes_FromAttachedView_EditAndPreserveIndependentParameterValues()
    {
        // Arrange
        using TumourGeneratorViewModel viewModel = Create(activate: false);
        viewModel.CurrentLayer!.TumourLength = TumourLayer.GetGraphState(24);
        viewModel.CurrentLayer.TumourRotation = TumourLayer.GetGraphState(48);
        TumourGeneratorView view = new() { DataContext = viewModel };
        using HeadlessViewHost host = HeadlessViewHost.Show(view);
        Slider scaleSlider = host.Find<Slider>("TumourScaleValueSlider");
        Slider lengthSlider = host.Find<Slider>("TumourLengthValueSlider");
        Slider rotationSlider = host.Find<Slider>("TumourRotationValueSlider");
        viewModel.CurrentLayer!.TumourRotation = TumourLayer.GetGraphState(48);

        // Act
        host.Click(scaleSlider);
        double sliderEditedScale = viewModel.CurrentLayer.TumourScaleValue;
        host.Click(host.Find<ToggleSwitch>("AdvancedOptionsToggle"));
        ValueOrGraphControl scaleGraph = host.Find<ValueOrGraphControl>("TumourScaleGraphControl");
        ValueOrGraphControl lengthGraph = host.Find<ValueOrGraphControl>("TumourLengthGraphControl");
        ValueOrGraphControl rotationGraph = host.Find<ValueOrGraphControl>("TumourRotationGraphControl");
        TextBox scaleInput = scaleGraph.GetVisualDescendants().OfType<TextBox>().Single();
        TextBox lengthInput = lengthGraph.GetVisualDescendants().OfType<TextBox>().Single();
        TextBox rotationInput = rotationGraph.GetVisualDescendants().OfType<TextBox>().Single();
        host.Click(scaleInput);
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("12");
        host.Click(lengthInput);
        host.PressKey(Key.A, RawInputModifiers.Control, PhysicalKey.A, "a");
        host.TypeText("0:24:0:0|1:48:0:0");
        host.Click(rotationInput);
        bool graphsVisible = scaleGraph.IsEffectivelyVisible
                             && lengthGraph.IsEffectivelyVisible
                             && rotationGraph.IsEffectivelyVisible;
        bool scalarSlidersVisible = scaleSlider.IsEffectivelyVisible
                                    && lengthSlider.IsEffectivelyVisible
                                    && rotationSlider.IsEffectivelyVisible;
        double[] graphValues =
        [
            scaleGraph.GraphState!.GetValue(0),
            lengthGraph.GraphState!.GetValue(0),
            rotationGraph.GraphState!.GetValue(0),
        ];
        double editedScale = viewModel.CurrentLayer.TumourScaleValue;
        double editedLength = viewModel.CurrentLayer.TumourLengthValue;
        double editedRotation = viewModel.CurrentLayer.TumourRotationValue;
        host.Click(host.Find<ToggleSwitch>("AdvancedOptionsToggle"));

        // Assert
        sliderEditedScale.Should().BeGreaterThan(0);
        editedScale.Should().Be(12);
        editedLength.Should().Be(36);
        editedRotation.Should().Be(48);
        new[] { editedScale, editedLength, editedRotation }.Distinct().Should().HaveCount(3);
        graphsVisible.Should().BeTrue();
        scalarSlidersVisible.Should().BeTrue();
        graphValues.Should().Equal(editedScale, 24, editedRotation);
        host.Find<Slider>("TumourScaleValueSlider").Value.Should().Be(editedScale);
        host.Find<Slider>("TumourLengthValueSlider").Value.Should().Be(editedLength);
        host.Find<Slider>("TumourRotationValueSlider").Value.Should().Be(editedRotation);
        host.Find<Slider>("TumourScaleValueSlider").IsEffectivelyVisible.Should().BeTrue();
    }

    private static TumourGeneratorViewModel Create(bool activate)
    {
        DesktopApplicationSettings settings = new() { EditorReload = EditorReloadMode.Disabled };
        TumourGeneratorViewModel viewModel = new(
            new IdleTumourGeneratorService(),
            new ToolExecutionService(new UserNotificationService(), TimeProvider.System),
            new TestBeatmapWorkspace(),
            settings,
            new TestDialogService());
        if (activate) viewModel.Activate();
        return viewModel;
    }

    private static void ClickCommand(HeadlessViewHost host, CommandBar commandBar, System.Windows.Input.ICommand command)
    {
        CommandBarButton? primaryButton = commandBar.VisiblePrimaryCommands
            .OfType<CommandBarButton>()
            .SingleOrDefault(button => ReferenceEquals(button.Command, command));
        if (primaryButton is not null)
        {
            Popup primaryOverflowPopup = commandBar.GetVisualDescendants().OfType<Popup>().Single();
            if (commandBar.IsOpen || primaryOverflowPopup.IsOpen)
            {
                Button primaryOverflowButton = commandBar.GetVisualDescendants().OfType<Button>()
                    .Single(button => button.Name == "PART_OverflowButton");
                host.Click(primaryOverflowButton);
                if (commandBar.IsOpen || primaryOverflowPopup.IsOpen)
                {
                    throw new InvalidOperationException("The command overflow popup did not close.");
                }
            }

            host.Click(primaryButton);
            return;
        }

        commandBar.IsOverflowButtonVisible.Should().BeTrue();
        Button overflowButton = commandBar.GetVisualDescendants().OfType<Button>()
            .SingleOrDefault(button => button.Name == "PART_OverflowButton")
            ?? throw new InvalidOperationException(string.Join(", ", commandBar.GetVisualDescendants()
                .Select(control => $"{control.GetType().Name}:{control.Name}")));
        Popup overflowPopup = commandBar.GetVisualDescendants().OfType<Popup>().Single();
        if (!commandBar.IsOpen && !overflowPopup.IsOpen)
        {
            host.Click(overflowButton);
        }
        if (!commandBar.IsOpen && !overflowPopup.IsOpen)
        {
            throw new InvalidOperationException("The command overflow popup did not open.");
        }
        CommandBarButton overflowItem = overflowPopup.Child!.GetVisualDescendants().OfType<CommandBarButton>()
            .Single(button => ReferenceEquals(button.Command, command));
        TopLevel popup = TopLevel.GetTopLevel(overflowItem)
                         ?? throw new InvalidOperationException("The overflow command has no popup input root.");
        Point point = overflowItem.TranslatePoint(
                          new Point(overflowItem.Bounds.Width / 2, overflowItem.Bounds.Height / 2), popup)
                      ?? throw new InvalidOperationException("Could not locate the overflow command in its popup.");
        popup.MouseMove(point);
        popup.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        popup.MouseUp(point, MouseButton.Left);
        HeadlessViewHost.RunDispatcherJobs();
        if (overflowPopup.IsOpen)
        {
            host.Click(overflowButton);
            if (commandBar.IsOpen || overflowPopup.IsOpen)
            {
                throw new InvalidOperationException("The command overflow popup did not close.");
            }
        }
    }

    private sealed class IdleTumourGeneratorService : ITumourGeneratorService
    {
        public Task<TumourImportResult> ImportAsync(
            string path,
            HitObjectSelectionMode mode,
            string? timeCode,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TumourImportResult([], 0, false));
        }

        public Task<TumourRunResult> RunAsync(
            IReadOnlyList<string> paths,
            TumourGeneratorServiceOptions project,
            bool quickRun,
            IProgress<double>? progress = null,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new TumourRunResult(paths, 0, quickRun));
        }
    }
}
