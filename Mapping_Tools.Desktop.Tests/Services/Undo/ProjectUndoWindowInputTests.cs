using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using Avalonia.Data;
using Mapping_Tools.Application.Projects.Models;
using Mapping_Tools.Desktop.Services.Undo;
using Mapping_Tools.Desktop.Shell;
using Mapping_Tools.Desktop.Tests.TestHelpers;
using Mapping_Tools.Infrastructure.Projects;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Services.Undo;

[TestClass]
[SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Windows are closed before the test's history is disposed.")]
public sealed class ProjectUndoWindowInputTests
{
    [TestMethod]
    public void Attach_WithReplayCallback_RoutesUndoAndRedoThroughCallback()
    {
        // Arrange
        Window window = new();
        List<bool> requests = [];
        ProjectUndoWindowInput.Attach(window, () => null, requests.Add);
        using HeadlessViewHost host = HeadlessViewHost.Attach(window);

        // Act
        window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        RaiseShortcut(window, Key.Y);

        // Assert
        requests.Should().Equal(true, false);
    }

    [TestMethod]
    public void Attach_WithDialogHistory_UndoesAndRedoesDraft()
    {
        // Arrange
        TestDialog dialog = new();
        using DialogUndoHistory history = new(dialog, () => { });
        Window window = new();
        ProjectUndoWindowInput.Attach(window, () => history);
        using HeadlessViewHost host = HeadlessViewHost.Attach(window);
        dialog.Value = 1;
        history.Capture();

        // Act
        RaiseShortcut(window, Key.Z);
        int undone = dialog.Value;
        RaiseShortcut(window, Key.Y);

        // Assert
        undone.Should().Be(0);
        dialog.Value.Should().Be(1);
    }

    [TestMethod]
    public void AttachDialog_Undo_DoesNotChangeProjectHistory()
    {
        // Arrange
        UndoFeature feature = new();
        ProjectUndoHistory<UndoProject> projectHistory = new(feature, new VersionedProjectJsonSerializer());
        feature.UndoHistory = projectHistory;
        Window projectWindow = new();
        ProjectUndoWindowInput.Attach(projectWindow, () => projectHistory);
        using HeadlessViewHost projectHost = HeadlessViewHost.ShowWindow(projectWindow);
        TestDialog draft = new();
        Window dialogWindow = new() { DataContext = draft };
        ProjectUndoWindowInput.AttachDialog(dialogWindow);
        using HeadlessViewHost dialogHost = HeadlessViewHost.ShowWindow(dialogWindow);
        HeadlessViewHost.RunDispatcherJobs();

        // Act
        feature.Value = "project";
        draft.Value = 1;
        HeadlessViewHost.RunDispatcherJobs();
        dialogWindow.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        int dialogAfterUndo = draft.Value;
        string projectBeforeUndo = feature.Value;
        projectWindow.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");

        // Assert
        dialogAfterUndo.Should().Be(0);
        projectBeforeUndo.Should().Be("project");
        feature.Value.Should().BeEmpty();
    }

    [TestMethod]
    public void Attach_WithExcludedTextBox_DoesNotKeepDraftHistoryInTextEdit()
    {
        // Arrange
        TestDialog dialog = new();
        using DialogUndoHistory history = new(dialog, () => { });
        TextBox textBox = new();
        Window window = new() { Content = textBox };
        ProjectUndoWindowInput.Attach(window, () => history, includeTextEdit: _ => false);
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);

        // Act
        textBox.Focus();
        dialog.Value = 1;
        history.Capture();
        history.Undo();

        // Assert
        dialog.Value.Should().Be(0);
        history.CanRedo.Should().BeTrue();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Attach_WithFocusedTextBox_GroupsChangesUntilBlurOrClose(bool closeWhileFocused)
    {
        // Arrange
        TestDialog dialog = new();
        using DialogUndoHistory history = new(dialog, () => { });
        TextBox textBox = new();
        Button other = new();
        Window window = new() { Content = new StackPanel { Children = { textBox, other } } };
        ProjectUndoWindowInput.Attach(window, () => history);
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        textBox.Focus();
        HeadlessViewHost.RunDispatcherJobs();
        int captures = 0;
        history.Changed += (_, _) => captures++;

        // Act
        dialog.Value = 1;
        HeadlessViewHost.RunDispatcherJobs();
        dialog.Value = 2;
        HeadlessViewHost.RunDispatcherJobs();
        bool recordedWhileFocused = history.CanUndo;
        if (closeWhileFocused) window.Close();
        else other.Focus();
        HeadlessViewHost.RunDispatcherJobs();
        int capturesBeforeUndo = captures;
        history.Undo();

        // Assert
        recordedWhileFocused.Should().BeFalse();
        capturesBeforeUndo.Should().Be(1);
        dialog.Value.Should().Be(0);
        history.CanUndo.Should().BeFalse();
    }

    [TestMethod]
    public void Attach_WithFocusedTextBoxWithLocalUndo_PreservesTextUndoAndRedo()
    {
        // Arrange
        TextBox textBox = new();
        Window window = new() { Content = textBox };
        List<bool> requests = [];
        ProjectUndoWindowInput.Attach(window, () => new RecordingHistory(), requests.Add);
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        textBox.Focus();
        textBox.RaiseEvent(new TextInputEventArgs
        {
            RoutedEvent = InputElement.TextInputEvent,
            Text = "typed",
        });

        // Act
        RaiseShortcut(textBox, Key.Z);
        string? undone = textBox.Text;
        RaiseShortcut(textBox, Key.Y);

        // Assert
        undone.Should().BeNullOrEmpty();
        textBox.Text.Should().Be("typed");
        requests.Should().BeEmpty();
    }

    [TestMethod]
    public void Attach_WithFocusedTextBox_CommitsOneProjectEditOnBlur()
    {
        // Arrange
        UndoFeature feature = new();
        ProjectUndoHistory<UndoProject> history = new(feature, new VersionedProjectJsonSerializer());
        feature.UndoHistory = history;
        TextBox textBox = new();
        textBox.Bind(TextBox.TextProperty, new Binding(nameof(UndoFeature.Value)) { Mode = BindingMode.TwoWay });
        Button other = new();
        Window window = new() { DataContext = feature, Content = new StackPanel { Children = { textBox, other } } };
        ProjectUndoWindowInput.Attach(window, () => history);
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        textBox.Focus();
        HeadlessViewHost.RunDispatcherJobs();

        // Act
        textBox.RaiseEvent(new TextInputEventArgs
        {
            RoutedEvent = InputElement.TextInputEvent,
            Text = "typed",
        });
        window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        string? locallyUndone = textBox.Text;
        window.KeyPress(Key.Y, RawInputModifiers.Control, PhysicalKey.Y, "y");
        string? locallyRedone = textBox.Text;
        textBox.RaiseEvent(new TextInputEventArgs
        {
            RoutedEvent = InputElement.TextInputEvent,
            Text = "!",
        });
        other.Focus();
        HeadlessViewHost.RunDispatcherJobs();
        string committed = feature.Value;
        window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        string restored = feature.Value;

        // Assert
        locallyUndone.Should().BeNullOrEmpty();
        locallyRedone.Should().Be("typed");
        committed.Should().Be("typed!");
        restored.Should().BeEmpty();
        history.CanUndo.Should().BeFalse();
    }

    [TestMethod]
    public void Attach_WithFocusedTextBoxWithoutLocalUndo_AllowsProjectUndoAndRedo()
    {
        // Arrange
        TestDialog dialog = new();
        using DialogUndoHistory history = new(dialog, () => { });
        TextBox textBox = new();
        Window window = new() { Content = textBox };
        ProjectUndoWindowInput.Attach(window, () => history);
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        textBox.Focus();
        dialog.Value = 1;
        dialog.Value = 2;

        // Act
        RaiseShortcut(window, Key.Z);
        int undone = dialog.Value;
        RaiseShortcut(window, Key.Y);
        int redone = dialog.Value;
        history.Undo();

        // Assert
        undone.Should().Be(0);
        redone.Should().Be(2);
        dialog.Value.Should().Be(0);
        history.CanUndo.Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Attach_WithSliderGesture_RecordsOneProjectStepOnReleaseOrClose(bool closeDuringGesture)
    {
        // Arrange
        UndoFeature feature = new();
        ProjectUndoHistory<UndoProject> history = new(feature, new VersionedProjectJsonSerializer());
        feature.UndoHistory = history;
        Slider slider = new() { Minimum = 0, Maximum = 10, Width = 400, Height = 40 };
        slider.Bind(RangeBase.ValueProperty, new Binding(nameof(UndoFeature.SliderValue)) { Mode = BindingMode.TwoWay });
        Window window = new() { Width = 900, Height = 700, DataContext = feature, Content = slider };
        ProjectUndoWindowInput.Attach(window, () => history);
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        slider.ApplyTemplate();
        Thumb thumb = slider.GetVisualDescendants().OfType<Thumb>().Single();
        Point center = new(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2);
        Point point = thumb.TranslatePoint(center, window)
                      ?? throw new InvalidOperationException("Could not locate the slider inside its host window.");

        // Act
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseMove(point + new Vector(20, 0), RawInputModifiers.LeftMouseButton);
        double valueDuringGesture = feature.SliderValue;
        if (closeDuringGesture) window.Close();
        else
        {
            window.MouseUp(point + new Vector(20, 0), MouseButton.Left);
            window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");
        }
        if (closeDuringGesture) history.Undo();

        // Assert
        valueDuringGesture.Should().BeGreaterThan(0);
        feature.SliderValue.Should().Be(0);
        history.CanUndo.Should().BeFalse();
        history.CanRedo.Should().BeTrue();
    }

    [TestMethod]
    public void Attach_WithRadioSelection_GroupsTheSelectionIntoOneProjectStep()
    {
        // Arrange
        UndoFeature feature = new() { IsFirstOption = true };
        ProjectUndoHistory<UndoProject> history = new(feature, new VersionedProjectJsonSerializer());
        feature.UndoHistory = history;
        RadioButton first = new() { GroupName = "Options" };
        RadioButton second = new() { GroupName = "Options" };
        first.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(UndoFeature.IsFirstOption)) { Mode = BindingMode.TwoWay });
        second.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(UndoFeature.IsSecondOption)) { Mode = BindingMode.TwoWay });
        Window window = new()
        {
            Width = 900,
            Height = 700,
            DataContext = feature,
            Content = new StackPanel { Children = { first, second } },
        };
        ProjectUndoWindowInput.Attach(window, () => history);
        using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
        Point center = new(second.Bounds.Width / 2, second.Bounds.Height / 2);
        Point point = second.TranslatePoint(center, window)
                      ?? throw new InvalidOperationException("Could not locate the radio button inside its host window.");

        // Act
        window.MouseDown(point, MouseButton.Left, RawInputModifiers.LeftMouseButton);
        window.MouseUp(point, MouseButton.Left);
        bool selectedSecond = feature.IsSecondOption;
        window.KeyPress(Key.Z, RawInputModifiers.Control, PhysicalKey.Z, "z");

        // Assert
        selectedSecond.Should().BeTrue();
        feature.IsFirstOption.Should().BeTrue();
        feature.IsSecondOption.Should().BeFalse();
        history.CanUndo.Should().BeFalse();
    }

    [DataTestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Attach_ColorPickerPopupGesture_CompletesOneUndoGesture(bool closeDuringDrag)
    {
        // Arrange
        SynchronizationContext? previousContext = SynchronizationContext.Current;
        AvaloniaSynchronizationContext.InstallIfNeeded();
        try
        {
            RecordingHistory history = new();
            ColorPicker picker = new();
            Window window = new() { Content = picker };
            ProjectUndoWindowInput.Attach(window, () => history);
            using HeadlessViewHost host = HeadlessViewHost.ShowWindow(window);
            picker.ApplyTemplate();
            DropDownButton button = picker.GetVisualDescendants().OfType<DropDownButton>().Single();
            Flyout flyout = (Flyout)button.Flyout!;
            flyout.ShowAt(button);
            Control content = (Control)flyout.Content!;
            TopLevel popup = TopLevel.GetTopLevel(content)
                             ?? throw new InvalidOperationException("Color picker popup has no input root.");
            Point start = new(popup.Bounds.Width / 2, popup.Bounds.Height / 2);

            // Act
            popup.MouseDown(start, MouseButton.Left);
            popup.MouseMove(start + new Vector(5, 0));
            if (!closeDuringDrag) popup.MouseUp(start + new Vector(5, 0), MouseButton.Left);
            flyout.Hide();

            // Assert
            history.Started.Should().Be(1);
            history.Ended.Should().Be(1);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    private static void RaiseShortcut(InputElement input, Key key)
    {
        input.RaiseEvent(new KeyEventArgs
        {
            RoutedEvent = InputElement.KeyDownEvent,
            Source = input,
            Key = key,
            KeyModifiers = KeyModifiers.Control,
        });
    }

    private sealed class TestDialog : ObservableObject
    {
        [Undoable]
        public int Value
        {
            get;
            set => SetProperty(ref field, value);
        }
    }

    private sealed class UndoFeature : ObservableObject, IShellProjectFeature<UndoProject>
    {
        [Undoable]
        public string Value
        {
            get;
            set => SetProperty(ref field, value);
        } = string.Empty;

        [Undoable]
        public double SliderValue
        {
            get;
            set => SetProperty(ref field, value);
        }

        [Undoable]
        public bool IsFirstOption
        {
            get;
            set => SetProperty(ref field, value);
        }

        [Undoable]
        public bool IsSecondOption
        {
            get;
            set => SetProperty(ref field, value);
        }

        public IProjectUndoHistory? UndoHistory { get; set; }

        public ProjectDefinition<UndoProject> ProjectDefinition { get; } =
            new("undo-input-test.json", "Undo Input Test", () => new UndoProject());

        public UndoProject Snapshot() => new()
        {
            Value = Value,
            SliderValue = SliderValue,
            IsFirstOption = IsFirstOption,
            IsSecondOption = IsSecondOption,
        };

        public void Install(UndoProject project)
        {
            Value = project.Value;
            SliderValue = project.SliderValue;
            IsFirstOption = project.IsFirstOption;
            IsSecondOption = project.IsSecondOption;
        }
    }

    private sealed class UndoProject
    {
        public string Value { get; set; } = string.Empty;

        public double SliderValue { get; set; }

        public bool IsFirstOption { get; set; }

        public bool IsSecondOption { get; set; }
    }

    private sealed class RecordingHistory : IProjectUndoHistory
    {
        public event EventHandler? Changed { add { } remove { } }
        public bool CanUndo => false;
        public bool CanRedo => false;
        public bool IsRestoring => false;
        public int Started { get; private set; }
        public int Ended { get; private set; }
        public IDisposable BeginEdit() => new Gesture(() => { });
        public IDisposable BeginGesture()
        {
            Started++;
            return new Gesture(() => Ended++);
        }
        public IDisposable SuspendRecording() => new Gesture(() => { });
        public void Capture() { }
        public void AddExternalChange(IProjectUndoExternalChange change) { }
        public void Undo() { }
        public void Redo() { }

        private sealed class Gesture(Action end) : IDisposable
        {
            public void Dispose() => end();
        }
    }
}
