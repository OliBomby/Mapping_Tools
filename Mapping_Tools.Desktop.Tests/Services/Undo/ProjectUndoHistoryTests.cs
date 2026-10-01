using AwesomeAssertions;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Services.Undo;

[TestClass]
public sealed class ProjectUndoHistoryTests
{
    [TestMethod]
    public void Undo_WhileShortcutOrMenuGestureIsOpen_RestoresPreviousState()
    {
        // Arrange
        TestFeature feature = new();
        ProjectUndoHistory<TestProject> history = new(feature, new VersionedProjectJsonSerializer());
        feature.UndoHistory = history;
        feature.Value = 1;
        history.CanUndo.Should().BeTrue();

        // Act
        using (history.BeginGesture()) history.Undo();

        // Assert
        feature.Value.Should().Be(0);
        history.CanRedo.Should().BeTrue();
    }

    [TestMethod]
    public void BeginEdit_WithNestedEdits_RecordsOneProjectState()
    {
        // Arrange
        TestFeature feature = new();
        ProjectUndoHistory<TestProject> history = new(feature, new VersionedProjectJsonSerializer());
        feature.UndoHistory = history;

        // Act
        using (history.BeginEdit())
        {
            feature.Value = 1;
            using (history.BeginEdit()) feature.Value = 2;
            feature.Value = 3;
        }

        // Assert
        history.CanUndo.Should().BeTrue();
        history.Undo();
        feature.Value.Should().Be(0);
        history.CanUndo.Should().BeFalse();
        history.CanRedo.Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void BeginEdit_InsideGesture_BlocksUndoAndRedoUntilOperationCompletes(bool redo)
    {
        // Arrange
        TestFeature feature = new();
        ProjectUndoHistory<TestProject> history = new(feature, new VersionedProjectJsonSerializer());
        feature.Value = 1;
        feature.Value = 2;
        if (redo) history.Undo();
        int original = feature.Value;

        // Act
        int duringOperation;
        using (history.BeginGesture())
        {
            using (history.BeginEdit())
            {
                feature.Value = 3;
                if (redo) history.Redo();
                else history.Undo();
                duringOperation = feature.Value;
            }
            history.Undo();
        }

        // Assert
        duringOperation.Should().Be(3);
        feature.Value.Should().Be(original);
        history.CanRedo.Should().BeTrue();
    }

    [TestMethod]
    public void Undo_AfterNewEdit_DiscardsRedoStates()
    {
        // Arrange
        TestFeature feature = new();
        ProjectUndoHistory<TestProject> history = new(feature, new VersionedProjectJsonSerializer());
        feature.UndoHistory = history;
        feature.Value = 1;
        feature.Value = 2;
        history.Undo();

        // Act
        feature.Value = 4;

        // Assert
        history.CanRedo.Should().BeFalse();
        history.Undo();
        feature.Value.Should().Be(1);
    }

    [TestMethod]
    public void BeginGesture_WithSeveralChanges_RecordsOneProjectState()
    {
        // Arrange
        TestFeature feature = new();
        ProjectUndoHistory<TestProject> history = new(feature, new VersionedProjectJsonSerializer());
        feature.UndoHistory = history;

        // Act
        using (history.BeginGesture())
        {
            feature.Value = 1;
            feature.Value = 2;
            feature.Value = 3;
        }

        // Assert
        history.Undo();
        feature.Value.Should().Be(0);
        history.CanUndo.Should().BeFalse();
    }

    [TestMethod]
    public void Undo_WithExternalChange_ReplaysExternalAndProjectState()
    {
        // Arrange
        TestFeature feature = new();
        ProjectUndoHistory<TestProject> history = new(feature, new VersionedProjectJsonSerializer());
        feature.UndoHistory = history;
        TestExternalChange external = new();

        // Act
        using (history.BeginEdit())
        {
            feature.Value = 5;
            external.Redo();
            history.AddExternalChange(external);
        }
        history.Undo();

        // Assert
        feature.Value.Should().Be(0);
        external.IsApplied.Should().BeFalse();
        history.Redo();
        feature.Value.Should().Be(5);
        external.IsApplied.Should().BeTrue();
    }

    [TestMethod]
    public void Undo_WhenLaterExternalChangeFails_RollsBackAppliedChanges()
    {
        // Arrange
        TestFeature feature = new();
        ProjectUndoHistory<TestProject> history = new(feature, new VersionedProjectJsonSerializer());
        feature.UndoHistory = history;
        TestExternalChange failing = new() { FailOnUndo = true };
        TestExternalChange applied = new();
        using (history.BeginEdit())
        {
            feature.Value = 5;
            history.AddExternalChange(failing);
            history.AddExternalChange(applied);
            failing.Redo();
            applied.Redo();
        }

        // Act
        Action act = history.Undo;

        // Assert
        act.Should().Throw<IOException>();
        feature.Value.Should().Be(5);
        applied.IsApplied.Should().BeTrue();
        history.CanUndo.Should().BeTrue();
    }

    [TestMethod]
    public void RebaseUntracked_AfterEdit_PreservesMetadataAcrossUndo()
    {
        // Arrange
        TestFeature feature = new();
        ProjectUndoHistory<TestProject> history = new(feature, new VersionedProjectJsonSerializer());
        feature.UndoHistory = history;
        feature.Value = 5;
        feature.Metadata = 7;

        // Act
        history.RebaseUntracked(project => project.Metadata = 7);
        history.Undo();

        // Assert
        feature.Value.Should().Be(0);
        feature.Metadata.Should().Be(7);
        history.CanUndo.Should().BeFalse();
    }

    [TestMethod]
    public void Undo_WithMarkedNestedStateOutsideToolNamespace_RestoresEditableValue()
    {
        // Arrange
        TestFeature feature = new();
        ProjectUndoHistory<TestProject> history = new(feature, new VersionedProjectJsonSerializer());

        // Act
        feature.Child.SelectedValue = 5;
        Dispatcher.UIThread.RunJobs();
        bool recorded = history.CanUndo;
        history.Undo();

        // Assert
        recorded.Should().BeTrue();
        feature.Child.SelectedValue.Should().Be(0);
        history.CanRedo.Should().BeTrue();
    }

    [TestMethod]
    public void OnPropertyChanged_WithUnmarkedNestedProperty_DoesNotScheduleSnapshot()
    {
        // Arrange
        TestFeature feature = new();
        ProjectUndoHistory<TestProject> history = new(feature, new VersionedProjectJsonSerializer());
        int snapshots = feature.SnapshotCount;

        // Act
        feature.Child.Untracked = 7;
        Dispatcher.UIThread.RunJobs();

        // Assert
        feature.SnapshotCount.Should().Be(snapshots);
        history.CanUndo.Should().BeFalse();
    }

    private sealed class TestFeature : ObservableObject, IShellProjectFeature<TestProject>
    {
        public IProjectUndoHistory? UndoHistory { get; set; }

        [Undoable]
        public int Value
        {
            get;
            set => SetProperty(ref field, value);
        }

        public int Metadata { get; set; }

        [Undoable]
        public TestChild Child { get; } = new();

        public int SnapshotCount { get; private set; }

        public ProjectDefinition<TestProject> ProjectDefinition { get; } =
            new("undo-test.json", "Undo Tests", () => new TestProject());

        public TestProject Snapshot()
        {
            SnapshotCount++;
            return new TestProject { Value = Value, Metadata = Metadata, ChildValue = Child.SelectedValue };
        }

        public void Install(TestProject project)
        {
            Value = project.Value;
            Metadata = project.Metadata;
            Child.SelectedValue = project.ChildValue;
        }
    }

    private sealed class TestChild : ObservableObject
    {
        [Undoable]
        public int SelectedValue
        {
            get;
            set => SetProperty(ref field, value);
        }

        public int Untracked
        {
            get;
            set => SetProperty(ref field, value);
        }

        public object RuntimeService => throw new InvalidOperationException("History must only visit marked state.");
    }

    private sealed class TestExternalChange : IProjectUndoExternalChange
    {
        public bool IsApplied { get; private set; }

        public bool FailOnUndo { get; set; }

        public void Undo()
        {
            if (FailOnUndo) throw new IOException("Test failure");
            IsApplied = false;
        }

        public void Redo() => IsApplied = true;
    }

    internal sealed class TestProject
    {
        public int Value { get; set; }

        public int Metadata { get; set; }

        public int ChildValue { get; set; }
    }
}
