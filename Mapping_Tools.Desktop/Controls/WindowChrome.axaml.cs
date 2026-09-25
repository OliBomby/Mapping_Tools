using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>
///     Provides the shared title bar used by Mapping Tools dialog windows.
/// </summary>
[SuppressMessage("ReSharper", "UnusedParameter.Local")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class WindowChrome : UserControl
{
    /// <summary>Identifies the title displayed beside the application logo.</summary>
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<WindowChrome, string>(nameof(Title), string.Empty);

    /// <summary>
    ///     Identifies the optional Boolean result returned when the close button dismisses a dialog.
    /// </summary>
    public static readonly StyledProperty<bool?> CloseResultProperty =
        AvaloniaProperty.Register<WindowChrome, bool?>(nameof(CloseResult));

    /// <summary>Creates the shared dialog title bar.</summary>
    public WindowChrome()
    {
        InitializeComponent();
    }

    /// <summary>Gets or sets the title displayed beside the application logo.</summary>
    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    /// <summary>
    ///     Gets or sets the optional Boolean result passed to the containing dialog when it closes.
    /// </summary>
    public bool? CloseResult
    {
        get => GetValue(CloseResultProperty);
        set => SetValue(CloseResultProperty, value);
    }

    private void DragWindow(object? sender, PointerPressedEventArgs eventArgs)
    {
        if (eventArgs.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            (TopLevel.GetTopLevel(this) as Window)?.BeginMoveDrag(eventArgs);
    }

    private void ToggleMaximizeWindow(object? sender, TappedEventArgs eventArgs)
    {
        if (TopLevel.GetTopLevel(this) is not Window window || !window.CanMaximize) return;

        window.WindowState = window.WindowState == WindowState.Maximized
            ? WindowState.Normal
            : WindowState.Maximized;
    }

    private void CloseWindow(object? sender, RoutedEventArgs eventArgs)
    {
        (TopLevel.GetTopLevel(this) as Window)?.Close(CloseResult);
    }
}
