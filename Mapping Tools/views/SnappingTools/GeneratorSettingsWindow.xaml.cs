using Mapping_Tools.Classes.SnappingTools.DataStructure.RelevantObjectGenerators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
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
            var type = settings.GetType();
            if (type.BaseType == typeof(GeneratorSettings)) {
                var sharedProperties = type.BaseType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                var uniqueProperties = type.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);

                CreateCard(sharedProperties, settings);
                CreateCard(uniqueProperties, settings);
            } else {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                CreateCard(properties, settings);
            }
        }

        private void CreateCard(IReadOnlyCollection<PropertyInfo> props, object settings) {
            if (props.Count == 0) return;
            var card = new MaterialDesignThemes.Wpf.Card {Margin = new Thickness(10)};
            var panel = new StackPanel();
            card.Content = panel;

            foreach (var prop in props) {
                var e = GetSettingControl(prop, settings);
                if (e != null)
                    panel.Children.Add(e);
            }

            Panel.Children.Add(card);
        }

        private StackPanel GetSettingControl(PropertyInfo prop, object settings) {
            if (!prop.CanWrite || !prop.CanRead) return null;
            if (!SettingsTypes.Contains(prop.PropertyType)) return null;

            var value = prop.GetValue(settings);
            if (value == null) return null;

            var horizontalPanel = new StackPanel {Orientation = Orientation.Horizontal, Margin = new Thickness(10)};

            var name = new TextBlock {Width = 150};
            if (prop.GetCustomAttribute(typeof(DisplayNameAttribute)) is DisplayNameAttribute n) {
                name.Text = n.DisplayName;
            } else {
                name.Text = prop.Name;
            }
            if (prop.GetCustomAttribute(typeof(DescriptionAttribute)) is DescriptionAttribute d) {
                name.ToolTip = d.Description;
            }
            horizontalPanel.Children.Add(name);

            switch (value) {
                case bool _:
                    var toggleButton = new ToggleButton {Cursor = Cursors.Hand};
                    Binding toggleBinding = new Binding(prop.Name) {
                        Source = DataContext
                    };
                    toggleButton.SetBinding(ToggleButton.IsCheckedProperty, toggleBinding);
                    horizontalPanel.Children.Add(toggleButton);
                    break;
                case double _:
                    var doubleTextBox = new TextBox {Width = 100};
                    Binding doubleBinding = new Binding(prop.Name) {
                        Source = DataContext,
                        Converter = new DoubleToStringConverter()
                    };
                    doubleTextBox.SetBinding(TextBox.TextProperty, doubleBinding);
                    horizontalPanel.Children.Add(doubleTextBox);
                    break;
                case string _:
                    var stringTextBox = new TextBox {Width = 100};
                    Binding stringBinding = new Binding(prop.Name) {
                        Source = DataContext
                    };
                    stringTextBox.SetBinding(TextBox.TextProperty, stringBinding);
                    horizontalPanel.Children.Add(stringTextBox);
                    break;
            }

            return horizontalPanel;
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
