using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Mapping_Tools.Classes.BeatmapHelper;
using Microsoft.WindowsAPICodePack.Dialogs;
using OsuMemoryDataProvider;

namespace Mapping_Tools.Classes.SystemTools {
    public class IOHelper {
        private static readonly IOsuMemoryReader PioReader = OsuMemoryReader.Instance;

        public static bool SaveMapBackup(string fileToCopy, bool forced = false, string customFileName = "") {
            if( !SettingsManager.GetMakeBackups() && !forced )
                return false;

            DateTime now = DateTime.Now;
            string destinationDirectory = SettingsManager.GetBackupsPath();
            try {
                File.Copy(fileToCopy, Path.Combine(destinationDirectory, now.ToString("yyyy-MM-dd HH-mm-ss") + "___" + (string.IsNullOrEmpty(customFileName) ? Path.GetFileName(fileToCopy) : customFileName)));

                // Delete old files if the number of backup files are over the limit
                foreach (var fi in new DirectoryInfo(SettingsManager.GetBackupsPath()).GetFiles().OrderByDescending(x => x.CreationTime).Skip(SettingsManager.Settings.MaxBackupFiles))
                    fi.Delete();

                return true;
            }
            catch( Exception ex ) {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public static bool SaveMapBackup(string[] filesToCopy, bool forced = false) {
            bool result = true;
            foreach( string fileToCopy in filesToCopy ) {
                result = SaveMapBackup(fileToCopy, forced) && result;
                if( !result )
                    break;
            }
            return result;
        }

        public static string FolderDialog(string initialDirectory = "") {
            bool restore = initialDirectory == "";

            using( CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                IsFolderPicker = true,
                InitialDirectory = initialDirectory,
                RestoreDirectory = restore
            } ) {
                if( dialog.ShowDialog() == CommonFileDialogResult.Ok ) {
                    return dialog.FileName;
                }
            }
            return "";
        }

        public static string SaveProjectDialog(string initialDirectory = "") {
            bool restore = initialDirectory == "";

            using( SaveFileDialog saveFileDialog1 = new SaveFileDialog {
                Filter = "JSON File|*.json",
                Title = "Save a project",
                InitialDirectory = initialDirectory,
                RestoreDirectory = restore
            } ) {
                saveFileDialog1.ShowDialog();
                return saveFileDialog1.FileName;
            }
        }

        public static string LoadProjectDialog(string initialDirectory = "") {
            bool restore = initialDirectory == "";

            using( OpenFileDialog saveFileDialog1 = new OpenFileDialog {
                Filter = "JSON File|*.json",
                Title = "Open a project",
                InitialDirectory = initialDirectory,
                RestoreDirectory = restore
            } ) {
                saveFileDialog1.ShowDialog();
                return saveFileDialog1.FileName;
            }
        }

        public static string FileDialog() {
            using( OpenFileDialog openFileDialog = new OpenFileDialog {
                RestoreDirectory = true,
                CheckFileExists = true
            } ) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileName;
            }
        }

        public static string ConfigFileDialog(string initialDirectory = "") {
            bool restore = initialDirectory == "";

            using (OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "Config files (*.cfg)|*.cfg",
                FilterIndex = 1,
                InitialDirectory = initialDirectory,
                RestoreDirectory = restore,
                CheckFileExists = true
            }) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileName;
            }
        }

        public static string MIDIFileDialog() {
            using( OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "MIDI files (*.mid)|*.mid",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true
            } ) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileName;
            }
        }

        public static string SampleFileDialog() {
            using( OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "Audio files (*.wav;*.ogg)|*.wav;*.ogg|SoundFont files (*.sf2)|*.sf2",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true
            } ) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileName;
            }
        }

        public static string AudioFileDialog() {
            using( OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "Audio files (*.wav;*.ogg)|*.wav;*.ogg",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true
            } ) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileName;
            }
        }

        public static string[] BeatmapFileDialog(bool multiselect = false, bool restore = false) {
            string path = MainWindow.AppWindow.GetCurrentMaps()[0];
            using( OpenFileDialog openFileDialog = new OpenFileDialog {
                InitialDirectory = restore ? "" : path != "" ? Editor.GetBeatmapFolder(path) : SettingsManager.GetSongsPath(),
                Filter = "Osu files (*.osu)|*.osu",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true,
                Multiselect = multiselect
            } ) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileNames;
            }
        }

        public static string GetCurrentBeatmap() {
            try {
                string songs = SettingsManager.GetSongsPath();

                string folder = PioReader.GetMapFolderName();
                string filename = PioReader.GetOsuFileName();
                string path = Path.Combine(songs, folder, filename);

                if( songs == "" || folder == "" || filename == "" ) { return ""; }
                return path;
            }
            catch( Exception ) {
                return "";
            }
        }
    }
}