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

    private readonly FloatingButton button;
    private readonly ValidationCommand validationCommand;

    static ToolRunButton()
    {
        RunCommandProperty.Changed.AddClassHandler<ToolRunButton>(static (control, eventArgs) => control.SetCommand(eventArgs.NewValue as ICommand));
    }

    /// <summary>Creates the WPF-compatible floating play action.</summary>
    public ToolRunButton()
    {
        Width = 70;
        validationCommand = new ValidationCommand(this);
        button = new FloatingButton
        {
            Content = new MaterialIcon
            {
                Width = 36,
                Height = 36,
                Kind = MaterialIconKind.Play,
            },
        };
        ToolTip.SetTip(button, "Run this tool.");
        button.Command = validationCommand;
        Child = button;
    }

    /// <summary>Gets or sets the command invoked when the play action is selected.</summary>
    public ICommand? RunCommand
    {
        get => GetValue(RunCommandProperty);
        set => SetValue(RunCommandProperty, value);
    }

    private void SetCommand(ICommand? command)
    {
        validationCommand.Command = command;
    }

    private sealed class ValidationCommand(ToolRunButton owner) : ICommand
    {
        private ICommand? command;
        private EventHandler? canExecuteChanged;

        public event EventHandler? CanExecuteChanged
        {
            add => canExecuteChanged += value;
            remove => canExecuteChanged -= value;
        }

        public ICommand? Command
        {
            get => command;
            set
            {
                if (ReferenceEquals(command, value)) return;

                if (command is not null) command.CanExecuteChanged -= OnCanExecuteChanged;
                command = value;
                if (command is not null) command.CanExecuteChanged += OnCanExecuteChanged;
                canExecuteChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool CanExecute(object? parameter)
        {
            return command?.CanExecute(parameter) == true && !ToolValidationHelper.HasErrors(owner);
        }

        public void Execute(object? parameter)
        {
            if (CanExecute(parameter)) command!.Execute(parameter);
        }

        private void OnCanExecuteChanged(object? sender, EventArgs e)
        {
            canExecuteChanged?.Invoke(this, e);
        }
    }
}
