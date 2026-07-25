using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mapping_Tools.Classes.SystemTools {
    
    /// <summary>
    /// Provides equality-checked property mutation and change notification for editable domain models.
    /// </summary>
    public abstract class BindableBase : INotifyPropertyChanged {
        /// <summary>
        /// Notifies subscribers after a bindable property changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sets the property and executes an action and invokes the property changed event if the property changed.
        /// The action gets executed before the property changed event.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target">The variable to change.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">The property name for the property changed event.</param>
        /// <param name="action">The action to execute before the property change.</param>
        /// <returns>Whether the property changed or not.</returns>
        protected bool Set<T>(ref T target, T value, [CallerMemberName] string propertyName = "", Action action = null) {
            if (target != null && target.Equals(value)) return false;
            target = value;
            action?.Invoke();
            RaisePropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises <see cref="PropertyChanged"/> for a property changed outside <see cref="Set{T}"/>.
        /// </summary>
        /// <param name="propertyName">The changed property name, inferred from the caller by default.</param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
