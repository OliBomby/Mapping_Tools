using Mapping_Tools.Classes;
using Mapping_Tools.Components.Domain;
using MaterialDesignThemes.Wpf;
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
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectGenerators.GeneratorInputSelection;

namespace Mapping_Tools.Views.SnappingTools {
    /// <summary>
    /// Interaction logic for SnappingToolsPreferencesWindow.xaml
    /// </summary>
    public partial class GeneratorSettingsWindow {
        protected GeneratorSettings OriginalSettings;

        private static IEnumerable<Type> SettingsTypes => new[] {typeof(bool), typeof(double), typeof(string), typeof(SelectionPredicateCollection)};

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
            var card = new Card {Margin = new Thickness(10)};
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
                        Source = settings
                    };
                    toggleButton.SetBinding(ToggleButton.IsCheckedProperty, toggleBinding);
                    horizontalPanel.Children.Add(toggleButton);
                    break;
                case double _:
                    var doubleTextBox = new TextBox {Width = 100};
                    Binding doubleBinding = new Binding(prop.Name) {
                        Source = settings,
                        Converter = new DoubleToStringConverter()
                    };
                    doubleTextBox.SetBinding(TextBox.TextProperty, doubleBinding);
                    horizontalPanel.Children.Add(doubleTextBox);
                    break;
                case string _:
                    var stringTextBox = new TextBox {Width = 100};
                    Binding stringBinding = new Binding(prop.Name) {
                        Source = settings
                    };
                    stringTextBox.SetBinding(TextBox.TextProperty, stringBinding);
                    horizontalPanel.Children.Add(stringTextBox);
                    break;
                case SelectionPredicateCollection c:
                    var list = new ListView {Width = 270, ItemsSource = c.Predicates};
                    list.ItemTemplate = FindResource("SelectionPredicateTemplate") as DataTemplate;
                    var cm = new ContextMenu();
                    cm.Items.Add(new MenuItem {Header = "Duplicate", 
                        ToolTip = "Duplicate selected predicates.",
                        Command = new CommandImplementation(_ => {
                                var itemsToDupe = new SelectionPredicate[list.SelectedItems.Count];
                                var i = 0;
                                foreach (var listSelectedItem in list.SelectedItems) {
                                    itemsToDupe[i++] = (SelectionPredicate)listSelectedItem;
                                }
                                foreach (var listSelectedItem in itemsToDupe) {
                                    c.Predicates.Insert(list.Items.IndexOf(listSelectedItem)+1, (SelectionPredicate)listSelectedItem.Clone());
                                }
                        })});
                    cm.Items.Add(new MenuItem {Header = "Remove",
                        ToolTip = "Remove all selected predicates",
                        Command = new CommandImplementation(_ =>
                            c.Predicates.RemoveAll(o => list.SelectedItems.Contains(o)))
                    });
                    list.ContextMenu = cm;

                    var addButton = new Button {Style = FindResource("MaterialDesignFloatingActionMiniLightButton") as Style,
                        ToolTip = "Add a new selection predicate.",
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Content = new PackIcon {Kind = PackIconKind.Plus, Width = 24, Height = 24},
                        Margin = new Thickness(5),
                        Command = new CommandImplementation(_ => c.Predicates.Add(new SelectionPredicate()))};

                    var removeButton = new Button {Style = FindResource("MaterialDesignFloatingActionMiniLightButton") as Style,
                        ToolTip = "Remove all selected predicates or the last predicate if nothing is selected.",
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Content = new PackIcon {Kind = PackIconKind.Minus, Width = 24, Height = 24},
                        Margin = new Thickness(5),
                        Command = new CommandImplementation(_ => {
                            if (list.SelectedItems.Count == 0 && c.Predicates.Count > 0) {
                                c.Predicates.RemoveAt(c.Predicates.Count - 1);
                                return;
                            }
                            c.Predicates.RemoveAll(o => list.SelectedItems.Contains(o));
                        })};

                    horizontalPanel.Children.Add(list);
                    horizontalPanel.Children.Add(addButton);
                    horizontalPanel.Children.Add(removeButton);
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
