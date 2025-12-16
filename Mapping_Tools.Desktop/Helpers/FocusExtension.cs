using System;
using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Mapping_Tools.Desktop.Helpers;

public class FocusExtension
{
    public static readonly AttachedProperty<bool> IsFocusedProperty =
        AvaloniaProperty.RegisterAttached<FocusExtension, InputElement, bool>("IsFocused");

    static FocusExtension()
    {
        IsFocusedProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is InputElement element)
            {
                // Keep handlers attached to reflect UI-driven focus changes back into the bound property
                AttachFocusHandlers(element);

                if (args.NewValue.GetValueOrDefault<bool>())
                {
                    // Request focus when bound value becomes true
                    element.Focus();
                }
                // No direct API to forcibly clear focus; rely on LostFocus to update when user moves focus
                // If another element gets focused programmatically, LostFocus will fire and sync the property.
            }
        });
    }

    private static void AttachFocusHandlers(InputElement element)
    {
        // Avoid attaching handlers multiple times
        if (element.GetValue(AttachedHandlersProperty))
            return;

        element.GotFocus += ElementOnGotFocus;
        element.LostFocus += ElementOnLostFocus;

        element.SetValue(AttachedHandlersProperty, true);
    }

    private static void ElementOnGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (sender is InputElement element)
        {
            // Update the attached property to true when control gets focus
            element.SetValue(IsFocusedProperty, true);
        }
    }

    private static void ElementOnLostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is InputElement element)
        {
            // Update the attached property to false when control loses focus
            element.SetValue(IsFocusedProperty, false);
        }
    }

    // Backing attached property to track whether handlers are attached
    private static readonly AttachedProperty<bool> AttachedHandlersProperty =
        AvaloniaProperty.RegisterAttached<FocusExtension, InputElement, bool>("AttachedHandlers");

    public static bool GetIsFocused(AvaloniaObject obj)
    {
        return obj.GetValue(IsFocusedProperty);
    }

    public static void SetIsFocused(AvaloniaObject obj, bool value)
    {
        obj.SetValue(IsFocusedProperty, value);
    }
}