using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;

namespace Mapping_Tools.Views.SnappingTools {
    /// <summary>
    /// Interaction logic for SnappingToolsPreferencesWindow.xaml
    /// </summary>
    public partial class GeneratorSettingsWindow {
        protected GeneratorSettings OriginalSettings;

        public GeneratorSettingsWindow(GeneratorSettings settings) {
            InitializeComponent();

            if (settings == null) return;
            
            OriginalSettings = settings;
            DataContext = settings.Clone();

            PopulateSettings(DataContext);
        }

        private void PopulateSettings(object settings) {
            foreach (var prop in settings.GetType().GetProperties()) {
                if (!prop.CanWrite || !prop.CanRead) continue;
                
                var horizontalPanel = new StackPanel {Orientation = Orientation.Horizontal, Margin = new Thickness(10)};

                var value = prop.GetValue(settings);
                switch (value) {
                    case bool boolValue:
                        var name = new TextBlock {Text = prop.Name, Width = 100};
                        horizontalPanel.Children.Add(name);

                        var toggleButton = new ToggleButton {IsChecked = boolValue, Cursor = Cursors.Hand};
                        Binding myBinding = new Binding(prop.Name) {
                            Source = DataContext
                        };
                        toggleButton.SetBinding(ToggleButton.IsCheckedProperty, myBinding);
                        horizontalPanel.Children.Add(toggleButton);
                        break;
                }
                Panel.Children.Add(horizontalPanel);
            }
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            ((GeneratorSettings)DataContext).CopyTo(OriginalSettings);
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
