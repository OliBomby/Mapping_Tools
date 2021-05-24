using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace Mapping_Tools.Views {
    [HiddenTool]
    public class MappingTool : UserControl, INotifyPropertyChanged, IDisposable {
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(MappingTool),
                new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsActive {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public virtual void Activate() {
            IsActive = true;
        }
        public virtual void Deactivate() {
            IsActive = false;
        }

        protected void Set<T>(ref T target, T value, [CallerMemberName] string propertyName = "") {
            target = value;
            RaisePropertyChanged(propertyName);
        }

        protected void RaisePropertyChanged([CallerMemberName] string propertyName = "") {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public virtual void Dispose() {
            IsActive = false;
        }
    }
}
