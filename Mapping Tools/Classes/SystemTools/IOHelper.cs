using System;
using System.IO;
using System.Windows.Forms;

namespace Mapping_Tools.Classes.SystemTools
{
    public class IOHelper
    {
        public static void SaveMapBackup(string fileToCopy) {
            DateTime now = DateTime.Now;
            string destinationDirectory = MainWindow.AppWindow.BackupPath;
            try {
                File.Copy(fileToCopy, Path.Combine(destinationDirectory, now.ToString("yyyy-MM-dd HH-mm-ss") + "___" + Path.GetFileName(fileToCopy)));
            } catch (Exception ex) {
                MessageBox.Show(ex.Message);
            }
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
                InitialDirectory = MainWindow.AppWindow.settingsManager.GetSongsPath(),
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
            string songs = MainWindow.AppWindow.settingsManager.GetSongsPath(); 
            string path = Path.Combine(songs, folder, filename);

            if (songs == "" || folder == "" || filename == "") { return ""; }
            return path;
        }
    }
}
