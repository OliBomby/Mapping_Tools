using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Mapping_Tools.Classes.SystemTools {
    
    /// <summary>
    /// 
    /// </summary>
    public abstract class BindableBase : INotifyPropertyChanged {
        /// <summary>
        /// 
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <param name="value"></param>
        /// <param name="propertyName"></param>
        /// <returns>Whether the property changed or not.</returns>
        protected bool Set<T>(ref T target, T value, [CallerMemberName] string propertyName = "") {
            if (target != null && target.Equals(value)) return false;
            target = value;
            RaisePropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="propertyName"></param>
        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
