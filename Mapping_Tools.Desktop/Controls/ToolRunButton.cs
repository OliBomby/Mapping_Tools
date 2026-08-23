using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Material.Icons;
using Material.Icons.Avalonia;
using Material.Styles.Controls;

namespace Mapping_Tools.Desktop.Controls;

/// <summary>
///     Presents the ordinary play action used to start a mapping tool inside a 70-pixel view box.
/// </summary>
public sealed class ToolRunButton : Viewbox
{
    /// <summary>Identifies the command invoked when the play action is selected.</summary>
    public static readonly StyledProperty<ICommand?> RunCommandProperty =
        AvaloniaProperty.Register<ToolRunButton, ICommand?>(nameof(RunCommand));

    private readonly FloatingButton _button;

    static ToolRunButton()
    {
        RunCommandProperty.Changed.AddClassHandler<ToolRunButton>(static (control, eventArgs) => control._button.Command = eventArgs.NewValue as ICommand);
    }

    /// <summary>Creates the WPF-compatible floating play action.</summary>
    public ToolRunButton()
    {
        Width = 70;
        _button = new FloatingButton
        {
            Content = new MaterialIcon
            {
                Width = 36,
                Height = 36,
                Kind = MaterialIconKind.Play,
            },
        };
        ToolTip.SetTip(_button, "Run this tool.");
        Child = _button;
    }

    /// <summary>Gets or sets the command invoked when the play action is selected.</summary>
    public ICommand? RunCommand
    {
        get => GetValue(RunCommandProperty);
        set => SetValue(RunCommandProperty, value);
    }
}
