using Mapping_Tools.Classes.SystemTools;
using MaterialDesignThemes.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Mapping_Tools.Views
{
    /// <summary>
    /// Interaktionslogik für UserControl2.xaml
    /// </summary>
    public partial class PreferencesView : UserControl
    {
        public PreferencesView()
        {
            InitializeComponent();
            DataContext = SettingsManager.Settings;
        }

        [Obsolete]
        private void MakeDark(object sender, RoutedEventArgs e) {
            new PaletteHelper().SetLightDark(true);
        }

        [Obsolete]
        private void MakeLight(object sender, RoutedEventArgs e) {
            new PaletteHelper().SetLightDark(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                SettingsManager.Settings.OsuPath = path;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                SettingsManager.Settings.SongsPath = path;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                SettingsManager.Settings.BackupsPath = path;
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e) {
            MainWindow.AppWindow.listenerManager.ReloadHotkeys();
        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            string path = IOHelper.ConfigFileDialog(SettingsManager.GetOsuPath());
            if (!string.IsNullOrWhiteSpace(path)) {
                SettingsManager.Settings.OsuConfigPath = path;
            }
        }
    }
}
