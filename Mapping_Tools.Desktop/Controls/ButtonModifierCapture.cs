using Avalonia.Input;
using Avalonia.Interactivity;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>Captures modifiers for the next activation of an Avalonia button.</summary>
internal sealed class ButtonModifierCapture
{
    private readonly InputElement target;
    private KeyModifiers pendingModifiers;

    /// <summary>Starts observing pointer and keyboard activation on the target.</summary>
    /// <param name="target">The button whose activation modifiers should be captured.</param>
    public ButtonModifierCapture(InputElement target)
    {
        this.target = target ?? throw new ArgumentNullException(nameof(target));

        target.AddHandler(
            InputElement.PointerPressedEvent,
            OnPointerPressed,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        target.AddHandler(
            InputElement.PointerReleasedEvent,
            OnPointerReleased,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
        target.AddHandler(
            InputElement.PointerCaptureLostEvent,
            OnPointerCaptureLost,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
        target.AddHandler(
            InputElement.KeyDownEvent,
            OnKeyDown,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
        target.AddHandler(
            InputElement.KeyUpEvent,
            OnKeyUp,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
        target.AddHandler(
            InputElement.LostFocusEvent,
            OnLostFocus,
            RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    /// <summary>Returns and clears the modifiers captured for the next activation.</summary>
    public KeyModifiers Consume()
    {
        KeyModifiers modifiers = pendingModifiers;
        pendingModifiers = KeyModifiers.None;
        return modifiers;
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        pendingModifiers = eventArgs.GetCurrentPoint(target).Properties.IsLeftButtonPressed
            ? eventArgs.KeyModifiers
            : KeyModifiers.None;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs eventArgs)
    {
        pendingModifiers = KeyModifiers.None;
    }

    private void OnPointerCaptureLost(object? sender, PointerCaptureLostEventArgs eventArgs)
    {
        pendingModifiers = KeyModifiers.None;
    }

    private void OnKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.Key is Key.Enter or Key.Space)
            pendingModifiers = eventArgs.KeyModifiers;
    }

    private void OnKeyUp(object? sender, KeyEventArgs eventArgs)
    {
        pendingModifiers = KeyModifiers.None;
    }

    private void OnLostFocus(object? sender, FocusChangedEventArgs eventArgs)
    {
        pendingModifiers = KeyModifiers.None;
    }
}
