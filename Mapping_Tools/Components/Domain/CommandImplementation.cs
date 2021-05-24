using System;
using System.Windows.Input;

namespace Mapping_Tools.Components.Domain
{
    /// <summary>
    /// No WPF project is complete without it's own version of command implementation.
    /// </summary>
    public class CommandImplementation : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        /// <inheritdoc />
        public CommandImplementation(Action<object> execute) : this(execute, null) {
        }

        /// <inheritdoc />
        public CommandImplementation(Action<object> execute, Func<object, bool> canExecute) {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? (x => true);
        }


        /// <summary>Defines the method that determines whether the command can execute in its current state.</summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter) => _canExecute(parameter);

        /// <summary>Defines the method to be called when the command is invoked.</summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public void Execute(object parameter) => _execute(parameter);

        /// <summary>Occurs when changes occur that affect whether or not the command should execute.</summary>
        public event EventHandler CanExecuteChanged {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// Refreshes the Command queries.
        /// </summary>
        public void Refresh() => CommandManager.InvalidateRequerySuggested();
    }
}