using System;
using System.Windows;
using System.Windows.Controls;

namespace Mapping_Tools.Views {
    public abstract class MappingTool : UserControl {
        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(MappingTool),
                new FrameworkPropertyMetadata(default(bool), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public bool IsActive {
            get => (bool)GetValue(IsActiveProperty);
            set => SetValue(IsActiveProperty, value);
        }

        public void Activate() {
            IsActive = true;
        }
        public void Deactivate() {
            IsActive = false;
        }
    }
}
