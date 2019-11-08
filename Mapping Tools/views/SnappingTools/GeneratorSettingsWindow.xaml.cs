using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Components.Domain;

namespace Mapping_Tools.Views.SnappingTools {
    /// <summary>
    /// Interaction logic for SnappingToolsPreferencesWindow.xaml
    /// </summary>
    public partial class GeneratorSettingsWindow {
        protected GeneratorSettings OriginalSettings;

        private static IEnumerable<Type> SettingsTypes => new[] {typeof(bool), typeof(double), typeof(string)};

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
                if (!SettingsTypes.Contains(prop.PropertyType)) continue;

                var value = prop.GetValue(settings);
                if (value == null) continue;

                var horizontalPanel = new StackPanel {Orientation = Orientation.Horizontal, Margin = new Thickness(10)};

                var name = new TextBlock {Text = prop.Name, Width = 120};
                horizontalPanel.Children.Add(name);

                switch (value) {
                    case bool boolValue:
                        var toggleButton = new ToggleButton {IsChecked = boolValue, Cursor = Cursors.Hand};
                        Binding toggleBinding = new Binding(prop.Name) {
                            Source = DataContext
                        };
                        toggleButton.SetBinding(ToggleButton.IsCheckedProperty, toggleBinding);
                        horizontalPanel.Children.Add(toggleButton);
                        break;
                    case double doubleValue:
                        var doubleTextBox = new TextBox {Width = 100};
                        Binding doubleBinding = new Binding(prop.Name) {
                            Source = DataContext
                        };
                        doubleTextBox.SetBinding(TextBox.TextProperty, doubleBinding);
                        horizontalPanel.Children.Add(doubleTextBox);
                        break;
                    case string stringValue:
                        var stringTextBox = new TextBox {Width = 100};
                        Binding stringBinding = new Binding(prop.Name) {
                            Source = DataContext
                        };
                        stringTextBox.SetBinding(TextBox.TextProperty, stringBinding);
                        horizontalPanel.Children.Add(stringTextBox);
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
