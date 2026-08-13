using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Material.Styles.Controls;
using Material.Icons;
using Material.Icons.Avalonia;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>
/// Presents one stable tool action button that runs while idle and cancels while running.
/// </summary>
public sealed class ToolRunButton : FloatingButton
{
    private readonly MaterialIcon _icon = new();

    /// <summary>Identifies the command invoked while the tool is idle.</summary>
    public static readonly StyledProperty<ICommand?> RunCommandProperty =
        AvaloniaProperty.Register<ToolRunButton, ICommand?>(nameof(RunCommand));

    /// <summary>Identifies the command invoked while the tool is running.</summary>
    public static readonly StyledProperty<ICommand?> CancelCommandProperty =
        AvaloniaProperty.Register<ToolRunButton, ICommand?>(nameof(CancelCommand));

    /// <summary>Identifies whether the button currently represents cancellation.</summary>
    public static readonly StyledProperty<bool> IsRunningProperty =
        AvaloniaProperty.Register<ToolRunButton, bool>(nameof(IsRunning));

    static ToolRunButton()
    {
        RunCommandProperty.Changed.AddClassHandler<ToolRunButton>(
            static (button, _) => button.UpdatePresentation());
        CancelCommandProperty.Changed.AddClassHandler<ToolRunButton>(
            static (button, _) => button.UpdatePresentation());
        IsRunningProperty.Changed.AddClassHandler<ToolRunButton>(
            static (button, _) => button.UpdatePresentation());
    }

    /// <summary>Creates a circular primary action button with a play icon.</summary>
    public ToolRunButton()
    {
        Classes.Add("tool-run-action");
        Classes.Add("no-transitions");
        _icon.Width = 42;
        _icon.Height = 42;
        Content = _icon;
        UpdatePresentation();
    }

    /// <summary>Gets or sets the command invoked while the tool is idle.</summary>
    public ICommand? RunCommand
    {
        get => GetValue(RunCommandProperty);
        set => SetValue(RunCommandProperty, value);
    }

    /// <summary>Gets or sets the command invoked while the tool is running.</summary>
    public ICommand? CancelCommand
    {
        get => GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    /// <summary>Gets or sets whether the button currently cancels an active run.</summary>
    public bool IsRunning
    {
        get => GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    private void UpdatePresentation()
    {
        SetCurrentValue(CommandProperty, IsRunning ? CancelCommand : RunCommand);
        _icon.Kind = IsRunning ? MaterialIconKind.Stop : MaterialIconKind.Play;
        ToolTip.SetTip(this, IsRunning ? "Stop this tool." : "Run this tool.");
    }
}
