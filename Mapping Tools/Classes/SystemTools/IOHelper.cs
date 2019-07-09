using System;
using System.IO;
using System.Windows.Forms;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Mapping_Tools.Classes.SystemTools
{
    public class IOHelper
    {
        public static bool SaveMapBackup(string fileToCopy, bool forced=false) {
            if (!SettingsManager.GetMakeBackups() && !forced)
                return false;

            DateTime now = DateTime.Now;
            string destinationDirectory = SettingsManager.GetBackupsPath();
            try {
                File.Copy(fileToCopy, Path.Combine(destinationDirectory, now.ToString("yyyy-MM-dd HH-mm-ss") + "___" + Path.GetFileName(fileToCopy)));
                return true;
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
                return false;
            }
        }

        public static string FolderDialog(string initialDirectory = "") {
            bool restore = initialDirectory == "";

            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                IsFolderPicker = true,
                InitialDirectory = initialDirectory,
                RestoreDirectory = restore
            };

            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                return dialog.FileName;
            }
            return "";
        }

        public static string SaveProjectDialog(string initialDirectory = "") {
            bool restore = initialDirectory == "";

            SaveFileDialog saveFileDialog1 = new SaveFileDialog {
                Filter = "JSON File|*.json",
                Title = "Save a project",
                InitialDirectory = initialDirectory,
                RestoreDirectory = restore
            };
            saveFileDialog1.ShowDialog();
            return saveFileDialog1.FileName;
        }

        public static string LoadProjectDialog(string initialDirectory = "") {
            bool restore = initialDirectory == "";

            OpenFileDialog saveFileDialog1 = new OpenFileDialog {
                Filter = "JSON File|*.json",
                Title = "Open a project",
                InitialDirectory = initialDirectory,
                RestoreDirectory = restore
            };
            saveFileDialog1.ShowDialog();
            return saveFileDialog1.FileName;
        }

        public static string FileDialog() {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                RestoreDirectory = true,
                CheckFileExists = true
            };
            openFileDialog.ShowDialog();
            return openFileDialog.FileName;
        }

        public static string MIDIFileDialog() {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "MIDI files (*.mid)|*.mid",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true
            };
            openFileDialog.ShowDialog();
            return openFileDialog.FileName;
        }

        public static string SampleFileDialog() {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "Audio files (*.wav;*.ogg)|*.wav;*.ogg|SoundFont files (*.sf2)|*.sf2",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true
            };
            openFileDialog.ShowDialog();
            return openFileDialog.FileName;
        }

        public static string AudioFileDialog() {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "Audio files (*.wav;*.ogg)|*.wav;*.ogg",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true
            };
            openFileDialog.ShowDialog();
            return openFileDialog.FileName;
        }

        public static string BeatmapFileDialog() {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                InitialDirectory = SettingsManager.GetSongsPath(),
                Filter = "Osu files (*.osu)|*.osu",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true
            };
            openFileDialog.ShowDialog();
            return openFileDialog.FileName;
        }

        public static string CurrentBeatmap() {
            OsuMemoryDataProvider.DataProvider.Initalize();
            var reader = OsuMemoryDataProvider.DataProvider.Instance;
            string folder = reader.GetMapFolderName();
            string filename = reader.GetOsuFileName();
            string songs = SettingsManager.GetSongsPath(); 
            string path = Path.Combine(songs, folder, filename);

            if (songs == "" || folder == "" || filename == "") { return ""; }
            return path;
        }
    }
}
