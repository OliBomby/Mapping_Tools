using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Mapping_Tools.Desktop.Controls.Graph;

namespace Mapping_Tools.Desktop.Services.Undo;

/// <summary>Connects a window's input to its active edit history.</summary>
public static class ProjectUndoWindowInput
{
    /// <summary>Installs undo shortcuts and groups text, pointer, keyboard, and graph input into edits.</summary>
    /// <param name="window">The window that receives project input.</param>
    /// <param name="getHistory">Returns the currently attached project history.</param>
    /// <param name="replay">Optionally routes undo and redo through the window's commands; true requests undo.</param>
    /// <param name="includeTextEdit">Optionally excludes text boxes that do not edit project state.</param>
    public static void Attach(
        Window window,
        Func<IProjectUndoHistory?> getHistory,
        Action<bool>? replay = null,
        Predicate<TextBox>? includeTextEdit = null)
    {
        ArgumentNullException.ThrowIfNull(window);
        ArgumentNullException.ThrowIfNull(getHistory);
        new WindowInput(window, getHistory, replay, includeTextEdit).Attach();
    }

    /// <summary>Creates a separate draft history for a modal window's data context.</summary>
    /// <param name="window">The modal window containing editable inputs.</param>
    public static void AttachDialog(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        DialogUndoHistory? history = null;
        Attach(window, () => history);
        window.Opened += (_, _) =>
        {
            if (window.DataContext is not { } model) return;
            history = new DialogUndoHistory(model, () =>
            {
                window.DataContext = null;
                window.DataContext = model;
            });
        };
        window.Closed += (_, _) => history?.Dispose();
    }

    private sealed class WindowInput(
        Window window,
        Func<IProjectUndoHistory?> getHistory,
        Action<bool>? replay,
        Predicate<TextBox>? includeTextEdit)
    {
        private readonly Dictionary<TextBox, (IProjectUndoHistory History, IDisposable Scope)> textEdits = [];
        private readonly Dictionary<GraphControl, IDisposable> graphEdits = [];
        private ColorPickerUndoInput? colorPickers;
        private IDisposable? pointerGesture;
        private IDisposable? keyGesture;
        private Key? gestureKey;

        public void Attach()
        {
            window.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
            window.AddHandler(InputElement.KeyUpEvent, OnKeyUp, RoutingStrategies.Bubble, true);
            window.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, true);
            window.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Bubble, true);
            window.AddHandler(InputElement.GotFocusEvent, OnGotFocus, RoutingStrategies.Bubble, true);
            window.AddHandler(InputElement.LostFocusEvent, OnLostFocus, RoutingStrategies.Bubble, true);
            window.AddHandler(GraphControl.EditStartedEvent, OnGraphEditStarted);
            window.AddHandler(GraphControl.EditCompletedEvent, OnGraphEditCompleted);
            window.Deactivated += OnDeactivated;
            window.Closed += OnClosed;

            colorPickers = new ColorPickerUndoInput(window, getHistory);
        }

        private void OnKeyDown(object? sender, KeyEventArgs args)
        {
            if (args.KeyModifiers == KeyModifiers.Control && args.Key is Key.Z or Key.Y)
                ReplayShortcut(args);
            else
                BeginKeyGesture(args.Key);
        }

        private void ReplayShortcut(KeyEventArgs args)
        {
            // Tunnel routing lets us choose between the text box's local history and project history.
            if (window.FocusManager.GetFocusedElement() is TextBox textBox && !textBox.IsReadOnly
                && (args.Key == Key.Z ? textBox.CanUndo : textBox.CanRedo))
            {
                // TextBox handles Ctrl+Z itself; explicitly support Ctrl+Y for local redo.
                if (args.Key == Key.Y)
                {
                    textBox.Redo();
                    args.Handled = true;
                }
                return;
            }

            var history = getHistory();
            history?.Capture();
            if (replay is not null) replay(args.Key == Key.Z);
            else if (args.Key == Key.Z) history?.Undo();
            else history?.Redo();
            args.Handled = true;
        }

        private void BeginKeyGesture(Key key)
        {
            // Hold one scope across key repeats. Text input is grouped by focus instead.
            if (window.FocusManager.GetFocusedElement() is TextBox { IsReadOnly: false } || keyGesture is not null
                || key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
                    or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin) return;

            keyGesture = getHistory()?.BeginGesture();
            gestureKey = keyGesture is null ? null : key;
        }

        private void OnKeyUp(object? sender, KeyEventArgs args)
        {
            if (args.Key != gestureKey) return;
            keyGesture?.Dispose();
            keyGesture = null;
            gestureKey = null;
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs args)
        {
            // Graphs supply their own edit boundaries; menu popups do not release through this window.
            if (args.Source is TextBox || pointerGesture is not null
                || args.Source is GraphControl
                || args.Source is Visual visual && visual.GetVisualAncestors().OfType<GraphControl>().Any()
                || args.GetCurrentPoint(window).Properties.IsRightButtonPressed
                || IsInsideMenu(args.Source as StyledElement)) return;

            pointerGesture = getHistory()?.BeginGesture();
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs args)
        {
            pointerGesture?.Dispose();
            pointerGesture = null;
        }

        private void OnGotFocus(object? sender, RoutedEventArgs args)
        {
            if (args.Source is not TextBox { IsReadOnly: false } textBox || includeTextEdit?.Invoke(textBox) == false
                || textEdits.ContainsKey(textBox) || getHistory() is not { } history) return;

            textEdits.Add(textBox, (history, history.BeginGesture()));
        }

        private void OnLostFocus(object? sender, RoutedEventArgs args)
        {
            if (args.Source is not TextBox textBox || !textEdits.Remove(textBox, out var edit)) return;
            edit.Scope.Dispose();
            // Capture again after the focus event finishes, including any final binding updates.
            Dispatcher.UIThread.Post(edit.History.Capture, DispatcherPriority.Background);
        }

        private void OnGraphEditStarted(object? sender, RoutedEventArgs args)
        {
            if (args.Source is GraphControl graph && !graphEdits.ContainsKey(graph)
                && getHistory() is { } history)
                graphEdits.Add(graph, history.BeginGesture());
        }

        private void OnGraphEditCompleted(object? sender, RoutedEventArgs args)
        {
            if (args.Source is GraphControl graph && graphEdits.Remove(graph, out var edit)) edit.Dispose();
        }

        private void OnDeactivated(object? sender, EventArgs args)
        {
            EndInputGestures();
        }

        private void OnClosed(object? sender, EventArgs args)
        {
            colorPickers?.Dispose();
            EndInputGestures();
            foreach (var edit in textEdits.Values) edit.Scope.Dispose();
            textEdits.Clear();
            getHistory()?.Capture();
        }

        private void EndInputGestures()
        {
            pointerGesture?.Dispose();
            pointerGesture = null;
            keyGesture?.Dispose();
            keyGesture = null;
            gestureKey = null;
            foreach (var edit in graphEdits.Values) edit.Dispose();
            graphEdits.Clear();
        }

        private static bool IsInsideMenu(StyledElement? element)
        {
            for (var current = element; current is not null; current = current.Parent)
                if (current is MenuItem) return true;
            return element is Visual visual && visual.GetVisualAncestors().OfType<MenuItem>().Any();
        }
    }
}
