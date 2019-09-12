using Mapping_Tools.Classes.SystemTools.OverlaySettings;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Input;
using System.Windows.Data;

namespace Mapping_Tools.Components {
    /// <summary>
    /// Interaction logic for RelevantObjectSettingsControl_.xaml
    /// </summary>
    public partial class RelevantObjectSettingsControl {
        public static readonly DependencyProperty RelevantObjectSettingsProperty = DependencyProperty.Register(
            nameof(RelevantObjectSettings), 
            typeof(RelevantObjectSettings), 
            typeof(RelevantObjectSettingsControl),
            new FrameworkPropertyMetadata(default(RelevantObjectSettings), 
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault)
            );

        public RelevantObjectSettings RelevantObjectSettings {
            get => (RelevantObjectSettings)GetValue(RelevantObjectSettingsProperty);
            set => SetValue(RelevantObjectSettingsProperty, value);
        }

        public RelevantObjectSettingsControl() {
            InitializeComponent();
        }

        private void DashStyleCombobox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e) {
            switch (DashStyleCombobox.SelectedItem.ToString()) {
                case "Dash":
                    RelevantObjectSettings.DashStyle = DashStyles.Dash;
                    break;
                case "Dot":
                    RelevantObjectSettings.DashStyle = DashStyles.Dot;
                    break;
                case "DashDot":
                    RelevantObjectSettings.DashStyle = DashStyles.DashDot;
                    break;
                case "DashDotDot":
                    RelevantObjectSettings.DashStyle = DashStyles.DashDotDot;
                    break;
                case "Solid":
                    RelevantObjectSettings.DashStyle = DashStyles.Solid;
                    break;
                default:
                    break;
            }
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            RelevantObjectSettings.Opacity = (float) e.NewValue;
        }

        private void ThicknessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            RelevantObjectSettings.Thickness = (int) e.NewValue;
        }

        private void ColorHexTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e) {
            RelevantObjectSettings.Color = (Color) ColorConverter.ConvertFromString(ColorHexTextBox.Text);
        }
    }
}
