using System;
using System.IO;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.Tools;
using Microsoft.WindowsAPICodePack.Dialogs;
using OsuMemoryDataProvider;
using System.Windows.Forms;
using Mapping_Tools.Classes.ToolHelpers;

namespace Mapping_Tools.Classes.SystemTools {
    public class IOHelper {
        private static readonly IOsuMemoryReader PioReader = OsuMemoryReader.Instance;

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

        public static string ZipFileDialog() {
            using (OpenFileDialog saveFileDialog1 = new OpenFileDialog {
                Filter = "ZIP File|*.zip",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true
            }) {
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
                InitialDirectory = restore ? "" : path != "" ? Editor.GetParentFolder(path) : SettingsManager.GetSongsPath(),
                Filter = "osu! files (*.osu;*.osb)|*.osu;*.osb",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true,
                Multiselect = multiselect
            } ) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileNames;
            }
        }

        public static string[] BeatmapFileDialog(string initialDirectory, bool multiselect = false) {
            string path = MainWindow.AppWindow.GetCurrentMaps()[0];
            using (OpenFileDialog openFileDialog = new OpenFileDialog {
                InitialDirectory = initialDirectory,
                Filter = "osu! files (*.osu;*.osb)|*.osu;*.osb",
                FilterIndex = 1,
                RestoreDirectory = true,
                CheckFileExists = true,
                Multiselect = multiselect
            }) {
                openFileDialog.ShowDialog();
                return openFileDialog.FileNames;
            }
        }

        public static string GetCurrentBeatmap() {
            string path;
            try {
                string songs = SettingsManager.GetSongsPath();

                if (string.IsNullOrEmpty(songs)) {
                    throw new Exception(
                        @"Can't fetch current in-game beatmap, because there is no Songs path specified in Preferences.");
                }

                string folder = PioReader.GetMapFolderName();
                string filename = PioReader.GetOsuFileName();

                if (string.IsNullOrEmpty(folder)) {
                    throw new Exception(@"Can't fetch the folder name of the current in-game beatmap.");
                }

                if (string.IsNullOrEmpty(filename)) {
                    throw new Exception(@"Can't fetch the file name of the current in-game beatmap.");
                }

                path = Path.Combine(songs, folder, filename);
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                try {
                    var reader = EditorReaderStuff.GetEditorReader();
                    reader.FetchHOM();
                    reader.FetchBeatmap();
                    path = EditorReaderStuff.GetCurrentBeatmap(reader);
                }
                catch (Exception ex2) {
                    Console.WriteLine(ex2.Message);
                    Console.WriteLine(ex2.StackTrace);
                    throw ex;
                }
            }
            
            return path;
        }

        public static string GetCurrentBeatmapOrCurrentBeatmap() {
            try {
                return GetCurrentBeatmap();
            }
            catch {
                return MainWindow.AppWindow.GetCurrentMaps()[0];
            }
        }
    }
}