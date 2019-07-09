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

        private void MakeDark(object sender, RoutedEventArgs e) {
            new PaletteHelper().SetLightDark(true);
        }

        private void MakeLight(object sender, RoutedEventArgs e) {
            new PaletteHelper().SetLightDark(false);
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                OsuPathBox.Text = path;
                SettingsManager.Settings.OsuPath = path;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                SongsPathBox.Text = path;
                SettingsManager.Settings.SongsPath = path;
            }
        }

        private void Button_Click_2(object sender, RoutedEventArgs e) {
            string path = IOHelper.FolderDialog();
            if (!string.IsNullOrWhiteSpace(path)) {
                BackupsPathBox.Text = path;
                SettingsManager.Settings.BackupsPath = path;
            }
        }

        private void OsuPathBox_TextChanged(object sender, TextChangedEventArgs e) {
            SettingsManager.Settings.OsuPath = OsuPathBox.Text;
        }

        private void SongsPathBox_TextChanged(object sender, TextChangedEventArgs e) {
            SettingsManager.Settings.SongsPath = SongsPathBox.Text;
            Console.WriteLine(SettingsManager.GetSongsPath());
            Console.WriteLine(((Settings)DataContext).SongsPath);
        }

        private void BackupsPathBox_TextChanged(object sender, TextChangedEventArgs e) {
            SettingsManager.Settings.BackupsPath = BackupsPathBox.Text;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e) {
            SettingsManager.Settings.MakeBackups = true;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e) {
            SettingsManager.Settings.MakeBackups = false;
        }
    }
}
