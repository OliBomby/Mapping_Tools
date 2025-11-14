using System;
using Avalonia;
using Avalonia.Input;

namespace Mapping_Tools.Desktop.Helpers;

public class FocusExtension
{
    public static readonly AttachedProperty<bool> IsFocusedProperty =
        AvaloniaProperty.RegisterAttached<FocusExtension, InputElement, bool>("IsFocused");

    static FocusExtension()
    {
        IsFocusedProperty.Changed.Subscribe(args =>
        {
            if (args.Sender is InputElement element &&
                args.NewValue.GetValueOrDefault<bool>())
            {
                element.Focus();
            }
        });
    }

    public static bool GetIsFocused(AvaloniaObject obj)
    {
        return obj.GetValue(IsFocusedProperty);
    }

    public static void SetIsFocused(AvaloniaObject obj, bool value)
    {
        obj.SetValue(IsFocusedProperty, value);
    }
}