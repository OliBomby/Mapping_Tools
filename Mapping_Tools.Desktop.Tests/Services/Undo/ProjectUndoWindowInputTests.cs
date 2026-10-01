using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CommunityToolkit.Mvvm.ComponentModel;
using Mapping_Tools.Desktop.Services.Undo;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mapping_Tools.Desktop.Tests.Services.Undo;

[TestClass]
public sealed class ProjectUndoWindowInputTests
{
    [TestMethod]
    public void Attach_WithReplayCallback_RoutesUndoAndRedoThroughCallback()
    {
        // Arrange
        Window window = new();
        List<bool> requests = [];
        ProjectUndoWindowInput.Attach(window, () => null, requests.Add);

        // Act
        RaiseShortcut(window, Key.Z);
        RaiseShortcut(window, Key.Y);
        window.Close();

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
        dialog.Value = 1;
        history.Capture();

        // Act
        RaiseShortcut(window, Key.Z);
        int undone = dialog.Value;
        RaiseShortcut(window, Key.Y);
        window.Close();

        // Assert
        undone.Should().Be(0);
        dialog.Value.Should().Be(1);
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
        window.Show();

        // Act
        textBox.Focus();
        dialog.Value = 1;
        history.Capture();
        history.Undo();
        window.Close();

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
        window.Show();
        textBox.Focus();
        int captures = 0;
        history.Changed += (_, _) => captures++;

        // Act
        dialog.Value = 1;
        Dispatcher.UIThread.RunJobs();
        dialog.Value = 2;
        Dispatcher.UIThread.RunJobs();
        bool recordedWhileFocused = history.CanUndo;
        if (closeWhileFocused) window.Close();
        else other.Focus();
        Dispatcher.UIThread.RunJobs();
        int capturesBeforeUndo = captures;
        history.Undo();
        window.Close();

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
        window.Show();
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
        window.Close();

        // Assert
        undone.Should().BeNullOrEmpty();
        textBox.Text.Should().Be("typed");
        requests.Should().BeEmpty();
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
        window.Show();
        textBox.Focus();
        dialog.Value = 1;
        dialog.Value = 2;

        // Act
        RaiseShortcut(window, Key.Z);
        int undone = dialog.Value;
        RaiseShortcut(window, Key.Y);
        int redone = dialog.Value;
        window.Close();
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
            window.Show();
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
            window.Close();

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
