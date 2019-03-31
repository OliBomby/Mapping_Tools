using System;
using System.IO;
using System.Windows.Forms;

namespace Mapping_Tools.Classes.SystemTools
{
    public class FileFinder
    {
        public static string FileDialog() {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                RestoreDirectory = true,
                CheckFileExists = true
            };
            openFileDialog.ShowDialog();
            return openFileDialog.FileName;
        }

        public static string AudioFileDialog() {
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "Audio files (*.wav)|*.wav",
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
