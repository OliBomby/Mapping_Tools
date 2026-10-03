using System.ComponentModel;
using Avalonia.Input;
using Mapping_Tools.Application.Localization;
using Mapping_Tools.Application.Tools.GeometryDashboard.Models;
using Mapping_Tools.Core.BeatmapHelper;
using Mapping_Tools.Core.Settings.Models;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.GeometryDashboard.Serialization;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.Models;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Desktop.Localization;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Tools.GeometryDashboard.ViewModels;

[TestClass]
[DoNotParallelize]
public sealed class GeometryDashboardViewModelTests
{
    [TestMethod]
    public void Undo_GeneratorRowChange_RefreshesTableBindings()
    {
        // Arrange
        using var viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel();
        var row = viewModel.Generators.First();
        bool original = row.IsSequential;
        ProjectUndoHistory<GeometryDashboardProject> history = new(viewModel, new VersionedProjectJsonSerializer());
        viewModel.UndoHistory = history;
        List<string?> notifications = [];
        row.PropertyChanged += (_, args) => notifications.Add(args.PropertyName);

        // Act
        row.IsSequential = !original;
        history.Capture();
        notifications.Clear();
        history.Undo();

        // Assert
        row.IsSequential.Should().Be(original);
        notifications.Should().Contain(nameof(row.IsSequential));
    }

    [TestMethod]
    public void Constructor_WithCoreGenerators_GroupsAndFiltersRows()
    {
        // Arrange
        using var viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel();

        // Act
        viewModel.Generators.Should().NotBeEmpty();
        viewModel.GeneratorGroups.Should().NotBeEmpty();

        viewModel.Filter = "circle";

        // Assert
        viewModel.GeneratorGroups.SelectMany(group => group.Generators)
            .Should().OnlyContain(generator => generator.Name.Contains("circle", StringComparison.OrdinalIgnoreCase));
    }

    [TestMethod]
    public void NotifySettingsChanged_AfterGeneratorSettingsChange_NotifiesGeneratorRowBindings()
    {
        // Arrange
        using var viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel();
        var generator = viewModel.Generators.First();
        var changedProperties = new List<string?>();
        generator.PropertyChanged += (_, eventArgs) => changedProperties.Add(eventArgs.PropertyName);
        bool expectedSequential = !generator.IsSequential;
        const double expected_relevancy_ratio = 0.75;

        // Act
        generator.Model.Settings.IsSequential = expectedSequential;
        generator.Model.Settings.RelevancyRatio = expected_relevancy_ratio;
        generator.NotifySettingsChanged();

        // Assert
        generator.IsSequential.Should().Be(expectedSequential);
        generator.RelevancyRatio.Should().Be(expected_relevancy_ratio);
        changedProperties.Should().Contain(nameof(generator.IsSequential));
        changedProperties.Should().Contain(nameof(generator.RelevancyRatio));
    }

    [TestMethod]
    public void Apply_AfterGeneratorSettingChanges_CopiesValuesToOriginalSettings()
    {
        // Arrange
        var originalSettings = new GeneratorSettings
        {
            IsSequential = false,
            RelevancyRatio = 0.4,
        };
        var dialog = new GeometryDashboardGeneratorSettingsDialogViewModel(originalSettings);
        var sequentialRow = dialog.Rows.Single(row => row.Name == "Sequential");
        var relevancyRow = dialog.Rows.Single(row => row.Name == "Relevancy Ratio");
        sequentialRow.BooleanValue = true;
        relevancyRow.ValueText = "0.75";

        // Act
        dialog.ApplyCommand.Execute(null);

        // Assert
        originalSettings.IsSequential.Should().BeTrue();
        originalSettings.RelevancyRatio.Should().Be(0.75);
    }

    [TestMethod]
    public void Constructor_WithDerivedSettings_UsesDeclaredReflectionOrder()
    {
        // Arrange
        var settings = new GeneratorSettingsWithUnmappedProperty();

        // Act
        var dialog = new GeometryDashboardGeneratorSettingsDialogViewModel(settings);

        // Assert
        dialog.SpecificRows.Select(row => row.Name).Should().Equal("Unmapped", "Angle");
    }

    [TestMethod]
    public async Task RefreshOnceAsync_WhenInputPlatformIsUnavailable_ShowsGracefulStatus()
    {
        // Arrange
        using var viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel(false);

        // Act
        await viewModel.RefreshOnceAsync();

        // Assert
        viewModel.Status.Should().Be(DesktopStrings.GeometryDashboard_StatusRequiresWindows);
        viewModel.StatusIndicatorState.Should().Be(GeometryDashboardStatusIndicatorState.Error);
    }

    [TestMethod]
    public async Task RefreshOnceAsync_WhenEditorIsUnfocused_ShowsGreenUnfocusedStatus()
    {
        // Arrange
        var editor = GeometryDashboardViewModelTestFactory.CreateRuntimeSnapshot(DecodeHitObject("64,96,1000,1,0,0:0:0:0:"), 0, []).Editor;
        using var viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel(
            snapshots: new GeometryDashboardRuntimeSnapshot(editor, false));

        // Act
        await viewModel.RefreshOnceAsync();

        // Assert
        viewModel.Status.Should().Be(ApplicationText.Format(
            viewModel.DrawableCount == 1 ? DesktopStrings.GeometryDashboard_StatusUnfocusedOne : DesktopStrings.GeometryDashboard_StatusUnfocusedMany,
            viewModel.DrawableCount));
        viewModel.StatusIndicatorState.Should().Be(GeometryDashboardStatusIndicatorState.Running);
    }

    [TestMethod]
    public async Task RefreshOnceAsync_WhenDashboardRunsInDutch_FormatsLocalizedDrawableCount()
    {
        // Arrange
        string? previousLanguage = TranslationManager.Language;
        TranslationManager.SetLanguage("nl");
        using var viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel(
            snapshots: GeometryDashboardViewModelTestFactory.CreateRuntimeSnapshot(
                DecodeHitObject("64,96,1000,1,0,0:0:0:0:"), 0, []));

        try
        {
            // Act
            await viewModel.RefreshOnceAsync();

            // Assert
            viewModel.Status.Should().Be(ApplicationText.Format(
                viewModel.DrawableCount == 1 ? DesktopStrings.GeometryDashboard_StatusRunningOne : DesktopStrings.GeometryDashboard_StatusRunningMany,
                viewModel.DrawableCount));
            viewModel.Status.Should().Contain(viewModel.DrawableCount == 1 ? "virtueel object" : "virtuele objecten");
            viewModel.StatusIndicatorState.Should().Be(GeometryDashboardStatusIndicatorState.Running);
        }
        finally
        {
            TranslationManager.SetLanguage(previousLanguage);
        }
    }

    [TestMethod]
    public async Task Deactivate_AfterRuntimeRefresh_UpdatesStatusThroughServiceEvent()
    {
        // Arrange
        using var viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel();
        await viewModel.RefreshOnceAsync();
        var changedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

        // Act
        viewModel.Deactivate();

        // Assert
        viewModel.Status.Should().Be(DesktopStrings.GeometryDashboard_StatusStopped);
        changedProperties.Should().Contain(nameof(viewModel.Status));
    }

    [TestMethod]
    public async Task RefreshOnceAsync_WhenOverlayConfigurationFails_ShowsDutchActionableStatusAndPreservesDiagnostic()
    {
        // Arrange
        string? previousLanguage = TranslationManager.Language;
        TranslationManager.SetLanguage("nl");
        using var viewModel = GeometryDashboardViewModelTestFactory.CreateViewModelSession(
            overlaySupported: true,
            overlayConfigurationStatus: "The overlay configuration file could not be read.",
            snapshots: GeometryDashboardViewModelTestFactory.CreateRuntimeSnapshot(
                DecodeHitObject("64,96,1000,1,0,0:0:0:0:"), 0, [])).ViewModel;

        try
        {
            // Act
            await viewModel.RefreshOnceAsync();

            // Assert
            viewModel.Status.Should().Be(DesktopStrings.GeometryDashboard_StatusConfigurationUnavailable);
            viewModel.Status.Should().Contain("configuratie");
            viewModel.DiagnosticStatus.Should().Contain("The overlay configuration file could not be read.");
            viewModel.StatusIndicatorState.Should().Be(GeometryDashboardStatusIndicatorState.Error);
        }
        finally
        {
            TranslationManager.SetLanguage(previousLanguage);
        }
    }

    [TestMethod]
    public async Task Activate_WithSaveSlot_RegistersGlobalBindingAndLoadsSlot()
    {
        // Arrange
        var globalHotkeys = new RecordingGlobalHotkeyService();
        using var viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel(globalHotkeys: globalHotkeys);
        var slot = new GeometryDashboardSaveSlot
        {
            ProjectHotkey = new HotkeySettings(56, 2),
        };
        slot.Preferences.AcceptableDifference = 70;
        viewModel.Project.CurrentPreferences.AcceptableDifference = 2;
        viewModel.Project.SaveSlots.Add(slot);

        // Act
        viewModel.Activate();
        string bindingId = globalHotkeys.Bindings.Single().Key;
        await globalHotkeys.Callbacks[bindingId](CancellationToken.None);

        // Assert
        globalHotkeys.Bindings[bindingId].Should().Be(new HotkeySettings(56, 2));
        viewModel.Project.CurrentPreferences.AcceptableDifference.Should().Be(70);
    }

    [TestMethod]
    public void Deactivate_WithActiveSaveSlot_RemovesGlobalBinding()
    {
        // Arrange
        var globalHotkeys = new RecordingGlobalHotkeyService();
        using var viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel(globalHotkeys: globalHotkeys);
        viewModel.Project.SaveSlots.Add(new GeometryDashboardSaveSlot
        {
            ProjectHotkey = new HotkeySettings(56, 0),
        });
        viewModel.Activate();
        string bindingId = globalHotkeys.Bindings.Single().Key;

        // Act
        viewModel.Deactivate();

        // Assert
        globalHotkeys.Bindings[bindingId].Should().BeNull();
    }

    [TestMethod]
    public void ToggleSelected_WithShiftModifierAndEmptyGraph_DoesNotCreateObjects()
    {
        // Arrange
        using var viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel();

        // Act
        viewModel.ToggleSelected(KeyModifiers.Shift);

        // Assert
        viewModel.DrawableCount.Should().Be(0);
        viewModel.SelectedCount.Should().Be(0);
    }

    [TestMethod]
    public async Task RefreshOnceAsync_WhenEditorSelectionChanges_SynchronizesRootSelectionState()
    {
        // Arrange
        HitObject initialHitObject = DecodeHitObject("64,96,1000,1,0,0:0:0:0:");
        HitObject selectedHitObject = DecodeHitObject("64,96,1000,1,0,0:0:0:0:");
        HitObject finalHitObject = DecodeHitObject("64,96,1000,1,0,0:0:0:0:");
        using var viewModel = GeometryDashboardViewModelTestFactory.CreateViewModel(
            snapshots:
            [
                GeometryDashboardViewModelTestFactory.CreateRuntimeSnapshot(initialHitObject, 0, []),
                GeometryDashboardViewModelTestFactory.CreateRuntimeSnapshot(selectedHitObject, 1, [selectedHitObject]),
                GeometryDashboardViewModelTestFactory.CreateRuntimeSnapshot(finalHitObject, 2, []),
            ]);

        // Act
        await viewModel.RefreshOnceAsync();
        int unselectedCount = viewModel.SelectedCount;
        await viewModel.RefreshOnceAsync();
        int selectedCount = viewModel.SelectedCount;
        await viewModel.RefreshOnceAsync();

        // Assert
        unselectedCount.Should().Be(0);
        selectedCount.Should().BeGreaterThan(0);
        viewModel.SelectedCount.Should().Be(0);
    }

    private sealed class GeneratorSettingsWithUnmappedProperty : GeneratorSettings
    {
        [DisplayName("Unmapped")] public double Unmapped { get; set; }

        [DisplayName("Angle")] public double Angle { get; set; }

        public override object Clone()
        {
            return new GeneratorSettingsWithUnmappedProperty
            {
                Unmapped = Unmapped,
                Angle = Angle,
            };
        }
    }
}
