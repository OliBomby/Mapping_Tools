using System;
using System.IO;
using System.Windows.Forms;
using Editor_Reader;
using Microsoft.WindowsAPICodePack.Dialogs;
using OsuMemoryDataProvider;

namespace Mapping_Tools.Classes.SystemTools
{
    public class IOHelper
    {
        private static readonly IOsuMemoryReader PioReader = OsuMemoryReader.Instance;
        private static readonly EditorReader KarooReader = new EditorReader();

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

            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                IsFolderPicker = true,
                InitialDirectory = initialDirectory,
                RestoreDirectory = restore
            }) {
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                    return dialog.FileName;
                }
            }
            return "";
        }

        public static string SaveProjectDialog(string initialDirectory = "") {
            bool restore = initialDirectory == "";

            using (SaveFileDialog saveFileDialog1 = new SaveFileDialog {
                Filter = "JSON File|*.json",
                Title = "Save a project",
                InitialDirectory = initialDirectory,
                RestoreDirectory = restore
            }) {
                saveFileDialog1.ShowDialog();
                return saveFileDialog1.FileName;
            }
        }

        public static string LoadProjectDialog(string initialDirectory = "") {
            bool restore = initialDirectory == "";

            using (OpenFileDialog saveFileDialog1 = new OpenFileDialog {
                Filter = "JSON File|*.json",
                Title = "Open a project",
                InitialDirectory = initialDirectory,
                RestoreDirectory = restore
            }) {
                saveFileDialog1.ShowDialog();
                return saveFileDialog1.FileName;
            }
        }

        public static string FileDialog() {
            using (OpenFileDialog openFileDialog = new OpenFileDialog {
                RestoreDirectory = true,
                CheckFileExists = true
            }) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileName;
            }
        }

        public static string MIDIFileDialog() {
            using (OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "MIDI files (*.mid)|*.mid",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true
            }) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileName;
            }
        }

        public static string SampleFileDialog() {
            using (OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "Audio files (*.wav;*.ogg)|*.wav;*.ogg|SoundFont files (*.sf2)|*.sf2",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true
            }) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileName;
            }
        }

        public static string AudioFileDialog() {
            using (OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "Audio files (*.wav;*.ogg)|*.wav;*.ogg",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true
            }) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileName;
            }
        }

        public static string[] BeatmapFileDialog(bool multiselect=false) {
            string path = MainWindow.AppWindow.GetCurrentMap();
            using (OpenFileDialog openFileDialog = new OpenFileDialog {
                InitialDirectory = path != "" ? Directory.GetParent(path).FullName : SettingsManager.GetSongsPath(),
                Filter = "Osu files (*.osu)|*.osu",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true,
                Multiselect = multiselect
            }) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileNames;
            }
        }

        public static string CurrentBeatmap() {
            string songs = SettingsManager.GetSongsPath();

            bool inEditor = PioReader.GetCurrentStatus(out int _) == OsuMemoryStatus.EditingMap;
            if (inEditor) {
                KarooReader.FetchAll();
                string folder = KarooReader.ContainingFolder;
                string filename = KarooReader.Filename;
                string path = Path.Combine(songs, folder, filename);

                if (songs == "" || folder == "" || filename == "") { return ""; }
                return path;
            } else {
                string folder = PioReader.GetMapFolderName();
                string filename = PioReader.GetOsuFileName();
                string path = Path.Combine(songs, folder, filename);

                if (songs == "" || folder == "" || filename == "") { return ""; }
                return path;
            }
        }
    }
}
