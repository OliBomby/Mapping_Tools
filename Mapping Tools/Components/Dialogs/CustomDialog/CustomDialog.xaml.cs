using System;
using Mapping_Tools.Components.Domain;
using MaterialDesignThemes.Wpf;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Components.Dialogs.CustomDialog {
    /// <summary>
    /// Interaction logic for OsuPatternImportDialog.xaml
    /// </summary>
    public partial class CustomDialog {
        private readonly int _autoSelectIndex;
        private int _populationIndex;
        private UIElement _autoSelectElement;

        public CustomDialog(object viewModel, int autoSelectIndex = -1) {
            if (viewModel == null) return;

            InitializeComponent();

            DataContext = viewModel;
            _autoSelectIndex = autoSelectIndex;
            PopulateSettings(DataContext);

            AcceptButton.Command = new CommandImplementation(AcceptButtonCommand);
        }

        private void AcceptButtonCommand(object parameter) {
            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            DialogHost.CloseDialogCommand.Execute(parameter, this);
        }

        private void PopulateSettings(object settings) {
            _populationIndex = 0;

            var type = settings.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            AddPropertyControls(properties, settings);
        }

        private void AddPropertyControls(IReadOnlyCollection<PropertyInfo> props, object settings, bool useCard = false) {
            if (props.Count == 0) return;

            if (useCard) {
                var card = new Card {Margin = new Thickness(10)};
                var cardPanel = new StackPanel();
                card.Content = cardPanel;

                foreach (var prop in props) {
                    var e = GetSettingControl(prop, settings);
                    if (e != null) {
                        cardPanel.Children.Add(e);
                    }
                }

                Panel.Children.Add(card);
            }
            else {
                foreach (var prop in props) {
                    var e = GetSettingControl(prop, settings);
                    if (e != null) {
                        Panel.Children.Add(e);
                    }
                }
            }
        }

        private UIElement GetSettingControl(PropertyInfo prop, object settings) {
            if (!prop.CanWrite || !prop.CanRead) return null;

            var value = prop.GetValue(settings);
            if (value == null) return null;

            string name;
            if (prop.GetCustomAttribute(typeof(DisplayNameAttribute)) is DisplayNameAttribute n) {
                name = n.DisplayName;
            } else {
                name = prop.Name;
            }

            string description = null;
            if (prop.GetCustomAttribute(typeof(DescriptionAttribute)) is DescriptionAttribute d) {
                description = d.Description;
            }

            UIElement content = null;
            switch (value) {
                case bool _:
                    var checkBox = new CheckBox {
                        Content = name, 
                        ToolTip = description, 
                        Margin = new Thickness(0, 0, 0, 5)
                    };

                    Binding toggleBinding = new Binding(prop.Name) {
                        Source = settings
                    };
                    checkBox.SetBinding(ToggleButton.IsCheckedProperty, toggleBinding);
                    content = checkBox;
                    break;
                case double _:
                    var doubleTextBox = new TextBox {
                        MinWidth = 100, 
                        ToolTip = description,
                        Margin = new Thickness(0, 0, 0, 5),
                        Style = Application.Current.FindResource("MaterialDesignFloatingHintTextBox") as Style
                    };
                    HintAssist.SetHint(doubleTextBox, name);

                    Binding doubleBinding = new Binding(prop.Name) {
                        Source = settings,
                        Converter = new DoubleToStringConverter()
                    };

                    if (prop.GetCustomAttribute(typeof(TimeInputAttribute)) != null) {
                        doubleBinding.Converter = new TimeToStringConverter();
                    }

                    if (prop.GetCustomAttribute(typeof(ConverterParameterAttribute)) is ConverterParameterAttribute doubleConverterParameterAttribute) {
                        doubleBinding.ConverterParameter = doubleConverterParameterAttribute.Parameter;
                    }

                    doubleTextBox.SetBinding(TextBox.TextProperty, doubleBinding);
                    content = doubleTextBox;
                    break;
                case string _:
                    var stringTextBox = new TextBox {
                        MinWidth = 100, 
                        ToolTip = description,
                        Margin = new Thickness(0, 0, 0, 5),
                        Style = Application.Current.FindResource("MaterialDesignFloatingHintTextBox") as Style };
                    HintAssist.SetHint(stringTextBox, name);

                    if (prop.GetCustomAttribute(typeof(TextWrappingAttribute)) is TextWrappingAttribute stringTextWrappingAttribute) {
                        stringTextBox.TextWrapping = stringTextWrappingAttribute.TextWrapping;
                    }
                    if (prop.GetCustomAttribute(typeof(MultiLineInputAttribute)) != null) {
                        stringTextBox.AcceptsReturn = true;
                    }

                    Binding stringBinding = new Binding(prop.Name) {
                        Source = settings
                    };
                    stringTextBox.SetBinding(TextBox.TextProperty, stringBinding);

                    content = stringTextBox;

                    // Attach a file browser button
                    if (prop.GetCustomAttribute(typeof(FileBrowseAttribute)) != null) {
                        content = AttachFileBrowseButton(stringTextBox, prop, settings);
                    }

                    // Attach a beatmap browser button
                    if (prop.GetCustomAttribute(typeof(BeatmapBrowseAttribute)) != null) {
                        content = AttachBeatmapBrowseHelp(stringTextBox, prop, settings);
                    }

                    break;
            }

            if (content != null && _autoSelectIndex == _populationIndex) {
                _autoSelectElement = content;
            }
            _populationIndex++;

            return content;
        }

        private void CustomDialog_OnLoaded(object sender, RoutedEventArgs e) {
            if (_autoSelectElement != null) {
                _autoSelectElement.Focus();
                if (_autoSelectElement is TextBox textBox) {
                    textBox.SelectAll();
                }
            }
        }

        private static Grid AttachFileBrowseButton(TextBox textBox, PropertyInfo prop, object settings) {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            textBox.MaxWidth = 200;

            Grid.SetColumn(textBox, 0);
            grid.Children.Add(textBox);

            var browseButton = new Button {
                Cursor = Cursors.Hand,
                Style = Application.Current.FindResource("IconButton") as Style,
                VerticalAlignment = VerticalAlignment.Bottom,
                ToolTip = @"Select files with File Explorer.",
                Content = new PackIcon {
                    Kind = PackIconKind.Folder, Width = 30, Height = 30, Cursor = Cursors.Hand,
                    Foreground = Application.Current.FindResource("PrimaryHueMidBrush") as Brush
                },
                Command = new CommandImplementation(_ => {
                    try {
                        string path = IOHelper.FileDialog();
                        if (!string.IsNullOrEmpty(path)) {
                            textBox.Text = path;
                            prop.SetValue(settings, path);
                        }
                    } catch (Exception ex) { ex.Show(); }
                })
            };

            Grid.SetColumn(browseButton, 2);
            grid.Children.Add(browseButton);

            return grid;
        }

        private static Grid AttachBeatmapBrowseHelp(TextBox textBox, PropertyInfo prop, object settings) {
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(5, GridUnitType.Pixel) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            textBox.MaxWidth = 200;

            Grid.SetColumn(textBox, 0);
            grid.Children.Add(textBox);

            var getButton = new Button {
                Cursor = Cursors.Hand,
                Style = Application.Current.FindResource("IconButton") as Style,
                VerticalAlignment = VerticalAlignment.Bottom,
                ToolTip = @"Fetch the selected beatmap from your osu! client.",
                Content = new PackIcon {
                    Kind = PackIconKind.RestoreFromTrash, Width = 30, Height = 30, Cursor = Cursors.Hand,
                    Foreground = Application.Current.FindResource("PrimaryHueMidBrush") as Brush
                },
                Command = new CommandImplementation(_ => {
                    try {
                        string path = IOHelper.GetCurrentBeatmap();
                        if (!string.IsNullOrEmpty(path)) {
                            textBox.Text = path;
                            prop.SetValue(settings, path);
                        }
                    } catch (Exception ex) { ex.Show(); }
                })
            };

            Grid.SetColumn(getButton, 2);
            grid.Children.Add(getButton);

            var browseButton = new Button {
                Cursor = Cursors.Hand,
                Style = Application.Current.FindResource("IconButton") as Style,
                VerticalAlignment = VerticalAlignment.Bottom,
                ToolTip = @"Select beatmaps with File Explorer.",
                Content = new PackIcon {
                    Kind = PackIconKind.Folder, Width = 30, Height = 30, Cursor = Cursors.Hand,
                    Foreground = Application.Current.FindResource("PrimaryHueMidBrush") as Brush
                },
                Command = new CommandImplementation(_ => {
                    try {
                        string[] paths = IOHelper.BeatmapFileDialog(restore: !SettingsManager.Settings.CurrentBeatmapDefaultFolder);
                        if (paths.Length > 0) {
                            textBox.Text = paths[0];
                            prop.SetValue(settings, paths[0]);
                        }
                    } catch (Exception ex) { ex.Show(); }
                })
            };

            Grid.SetColumn(browseButton, 4);
            grid.Children.Add(browseButton);

            return grid;
        }
    }
}
