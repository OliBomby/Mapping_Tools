using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Tests.TestDoubles;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.Models;
using Mapping_Tools.Desktop.Tools.HitsoundStudio.ViewModels;
using Mapping_Tools.Desktop.Tools.GeometryDashboard.ViewModels;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Core.Tools.GeometryDashboard.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;
using Mapping_Tools.Core.Graph;
using Mapping_Tools.Core.MathUtil;
using Mapping_Tools.Desktop.ViewModels.Dialogs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Mapping_Tools.Desktop.Tests.Services.Undo;

[TestClass]
public sealed class DialogUndoHistoryTests
{
    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BeginEdit_InsideGesture_BlocksUndoAndRedoUntilOperationCompletes(bool redo)
    {
        // Arrange
        TestDialog dialog = new();
        using DialogUndoHistory history = new(dialog, () => { });
        dialog.Value = 1;
        history.Capture();
        dialog.Value = 2;
        history.Capture();
        if (redo) history.Undo();
        int original = dialog.Value;

        // Act
        int duringOperation;
        using (history.BeginGesture())
        {
            using (history.BeginEdit())
            {
                dialog.Value = 3;
                if (redo) history.Redo();
                else history.Undo();
                duringOperation = dialog.Value;
            }
            history.Undo();
        }

        // Assert
        duringOperation.Should().Be(3);
        dialog.Value.Should().Be(original);
        history.CanRedo.Should().BeTrue();
    }

    [TestMethod]
    public void Undo_WithUnmarkedState_RestoresOnlyEditableProperties()
    {
        // Arrange
        TestDialog dialog = new();
        using DialogUndoHistory history = new(dialog, () => { });
        dialog.Value = 1;
        history.Capture();

        // Act
        dialog.Untracked = 2;
        history.Capture();
        history.Undo();

        // Assert
        dialog.Value.Should().Be(0);
        dialog.Untracked.Should().Be(2);
        history.CanUndo.Should().BeFalse();
    }

    [TestMethod]
    public void Undo_AfterGeneratorPredicateSelection_RestoresDraftWithoutRecordingSelection()
    {
        // Arrange
        GeneratorSettings original = new() { RelevancyRatio = 0.4 };
        GeometryDashboardGeneratorSettingsDialogViewModel dialog = new(original);
        using DialogUndoHistory history = new(dialog, () => { });
        var row = dialog.Rows.Single(row => row.Name == "Relevancy Ratio");
        row.ValueText = "0.75";
        history.Capture();
        var selected = new SelectionPredicate();

        // Act
        dialog.SetSelectedPredicates([selected]);
        history.Capture();
        history.Undo();

        // Assert
        row.ValueText.Should().Be("0.4");
        original.RelevancyRatio.Should().Be(0.4);
        dialog.SelectedPredicate.Should().BeSameAs(selected);
        history.CanUndo.Should().BeFalse();
    }

    [TestMethod]
    public void Undo_WhileShortcutGestureIsOpen_RestoresDialogDraft()
    {
        // Arrange
        HitsoundStudioExportDialogViewModel dialog = new(new HitsoundStudioProject(), new TestFilePicker());
        using DialogUndoHistory history = new(dialog, () => { });
        dialog.ExportFolder = "after";
        history.Capture();

        // Act
        using (history.BeginGesture()) history.Undo();

        // Assert
        dialog.ExportFolder.Should().BeEmpty();
        history.CanRedo.Should().BeTrue();
    }

    [TestMethod]
    public void Undo_GraphDialog_RestoresReplacedGraphState()
    {
        // Arrange
        GraphEditorViewModel dialog = new(GraphState.CreateDefault());
        using DialogUndoHistory history = new(dialog, () => { });
        Vector2 original = dialog.GraphState.Anchors[0].Pos;

        // Act
        var next = dialog.GraphState.Clone();
        next.Anchors[0].Pos = new Vector2(0, 0.5f);
        dialog.GraphState = next;
        history.Capture();
        history.Undo();

        // Assert
        dialog.GraphState.Anchors[0].Pos.Should().Be(original);
        history.CanUndo.Should().BeFalse();
        history.Redo();
        dialog.GraphState.Anchors[0].Pos.Should().Be(new Vector2(0, 0.5f));
    }

    [TestMethod]
    public void Undo_GeneratorSettingsDialog_RestoresNumericAndBooleanRows()
    {
        // Arrange
        GeneratorSettings original = new() { IsSequential = false, RelevancyRatio = 0.4 };
        GeometryDashboardGeneratorSettingsDialogViewModel dialog = new(original);
        using DialogUndoHistory history = new(dialog, () => { });
        var sequential = dialog.Rows.Single(row => row.Name == "Sequential");
        var relevancy = dialog.Rows.Single(row => row.Name == "Relevancy Ratio");

        // Act
        using (history.BeginGesture())
        {
            sequential.BooleanValue = true;
            relevancy.ValueText = "0.75";
        }
        history.Undo();

        // Assert
        sequential.BooleanValue.Should().BeFalse();
        relevancy.ValueText.Should().Be("0.4");
        original.IsSequential.Should().BeFalse();
        original.RelevancyRatio.Should().Be(0.4);
        history.CanUndo.Should().BeFalse();
    }

    [TestMethod]
    public void Undo_ExportOptions_RestoresDialogDraftWithoutChangingProject()
    {
        // Arrange
        HitsoundStudioProject project = new() { ExportFolder = "before" };
        HitsoundStudioExportDialogViewModel dialog = new(project, new TestFilePicker());
        using DialogUndoHistory history = new(dialog, () => { });

        // Act
        using (history.BeginGesture())
        {
            dialog.ExportFolder = "after";
            dialog.ExportSamples = !dialog.ExportSamples;
        }
        history.Undo();

        // Assert
        dialog.ExportFolder.Should().Be("before");
        dialog.ExportSamples.Should().Be(project.ExportSamples);
        project.ExportFolder.Should().Be("before");
        history.CanUndo.Should().BeFalse();
        history.Redo();
        dialog.ExportFolder.Should().Be("after");
    }

    private sealed class TestDialog : ObservableObject
    {
        [Undoable]
        public int Value
        {
            get;
            set => SetProperty(ref field, value);
        }

        public int Untracked { get; set; }

        public object RuntimeService => throw new InvalidOperationException("History must not visit runtime services.");
    }
}
