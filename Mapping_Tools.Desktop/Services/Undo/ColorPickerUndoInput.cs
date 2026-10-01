using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Mapping_Tools.Desktop.Services.Undo;

/// <summary>Groups color-picker popup drags that do not route input through their owning window.</summary>
internal sealed class ColorPickerUndoInput : IDisposable
{
    private readonly Window window;
    private readonly Func<IProjectUndoHistory?> getHistory;
    private readonly Dictionary<ColorPicker, FlyoutInput> pickers = [];
    private readonly IDisposable templates;
    private readonly IDisposable unloaded;

    /// <summary>Observes the window's color-picker templates and attaches to their popup input.</summary>
    /// <param name="window">The window containing the color pickers.</param>
    /// <param name="getHistory">Returns the history to use when a popup drag starts.</param>
    public ColorPickerUndoInput(Window window, Func<IProjectUndoHistory?> getHistory)
    {
        this.window = window;
        this.getHistory = getHistory;
        templates = TemplatedControl.TemplateAppliedEvent.AddClassHandler<ColorPicker>(OnTemplateApplied);
        unloaded = Control.UnloadedEvent.AddClassHandler<ColorPicker>(OnUnloaded);
        foreach (var picker in window.GetVisualDescendants().OfType<ColorPicker>()) Attach(picker);
    }

    /// <summary>Removes template observers and completes any active popup gestures.</summary>
    public void Dispose()
    {
        templates.Dispose();
        unloaded.Dispose();
        foreach (var input in pickers.Values) input.Dispose();
        pickers.Clear();
    }

    private void OnTemplateApplied(ColorPicker picker, TemplateAppliedEventArgs args)
    {
        if (TopLevel.GetTopLevel(picker) == window) Attach(picker);
    }

    private void OnUnloaded(ColorPicker picker, RoutedEventArgs args)
    {
        if (pickers.Remove(picker, out var input)) input.Dispose();
    }

    private void Attach(ColorPicker picker)
    {
        // The popup is exposed by the template's button. Reattach when the template is replaced.
        if (pickers.Remove(picker, out var previous)) previous.Dispose();
        if (picker.GetVisualDescendants().OfType<DropDownButton>().FirstOrDefault()?.Flyout is Flyout flyout)
            pickers.Add(picker, new FlyoutInput(flyout, getHistory));
    }

    private sealed class FlyoutInput : IDisposable
    {
        private readonly Flyout flyout;
        private readonly Func<IProjectUndoHistory?> getHistory;
        private InputElement? content;
        private IDisposable? gesture;

        public FlyoutInput(Flyout flyout, Func<IProjectUndoHistory?> getHistory)
        {
            this.flyout = flyout;
            this.getHistory = getHistory;
            flyout.Opened += OnOpened;
            flyout.Closed += OnClosed;
            if (flyout.IsOpen) OnOpened(flyout, EventArgs.Empty);
        }

        public void Dispose()
        {
            DetachContent();
            flyout.Opened -= OnOpened;
            flyout.Closed -= OnClosed;
        }

        private void OnOpened(object? sender, EventArgs args)
        {
            if (flyout.Content is not InputElement input) return;
            content = input;
            // The popup has a separate input root, so its pointer events never reach the window.
            input.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed, RoutingStrategies.Tunnel, true);
            input.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased, RoutingStrategies.Bubble, true);
        }

        private void OnClosed(object? sender, EventArgs args)
        {
            DetachContent();
        }

        private void OnPointerPressed(object? sender, PointerPressedEventArgs args)
        {
            if (gesture is not null || content is null || args.Source is TextBox
                || !args.GetCurrentPoint(content).Properties.IsLeftButtonPressed) return;
            gesture = getHistory()?.BeginGesture();
        }

        private void OnPointerReleased(object? sender, PointerReleasedEventArgs args)
        {
            gesture?.Dispose();
            gesture = null;
        }

        private void DetachContent()
        {
            gesture?.Dispose();
            gesture = null;
            if (content is null) return;
            content.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
            content.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
            content = null;
        }
    }
}
