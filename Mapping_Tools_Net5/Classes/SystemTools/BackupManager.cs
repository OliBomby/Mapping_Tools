using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.Exceptions;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;

namespace Mapping_Tools.Classes.SystemTools {
    public static class BackupManager {
        public static bool SaveMapBackup(string fileToCopy, bool forced = false, string filename = null, string backupCode = "") {
            try {
                if (!SettingsManager.GetMakeBackups() && !forced)
                    return false;

                if (string.IsNullOrEmpty(filename))
                    filename = Path.GetFileName(fileToCopy);

                if (SettingsManager.Settings.UseEditorReader) {
                    fileToCopy = GetNewestVersionPath(fileToCopy);
                }

                DateTime now = DateTime.Now;
                string destinationDirectory = SettingsManager.GetBackupsPath();
                var name = now.ToString("yyyy-MM-dd HH-mm-ss") + "_" + backupCode + "__" + filename;

                File.Copy(fileToCopy,
                    Path.Combine(destinationDirectory, name),
                    true);

                // Delete old files if the number of backup files are over the limit
                foreach (var fi in new DirectoryInfo(SettingsManager.GetBackupsPath()).GetFiles().OrderByDescending(x => x.CreationTime).Skip(SettingsManager.Settings.MaxBackupFiles))
                    fi.Delete();

                return true;
            } catch (Exception ex) {
                ex.Show();
                return false;
            }
        }

        public static bool SaveMapBackup(string[] filesToCopy, bool forced = false, string backupCode = "") {
            bool result = true;
            foreach (string fileToCopy in filesToCopy) {
                result = SaveMapBackup(fileToCopy, forced, backupCode: backupCode);
                if (!result)
                    break;
            }
            return result;
        }

        /// <summary>
        /// Copies a backup to replace a beatmap at the destination path.
        /// </summary>
        /// <param name="backupPath">Path to the backup map.</param>
        /// <param name="destination">Path to the destination map.</param>
        /// <param name="allowDifferentFilename">If false, this method throws an exception when the backup and the destination have mismatching beatmap metadata.</param>
        public static void LoadMapBackup(string backupPath, string destination, bool allowDifferentFilename = false) {
            var backupEditor = new BeatmapEditor(backupPath);
            var destinationEditor = new BeatmapEditor(destination);

            var backupFilename = backupEditor.Beatmap.GetFileName();
            var destinationFilename = destinationEditor.Beatmap.GetFileName();

            if (!allowDifferentFilename && !string.Equals(backupFilename, destinationFilename)) {
                throw new BeatmapIncompatibleException($"The backup and the destination beatmap have mismatching metadata.\n{backupFilename}\n{destinationFilename}");
            }

            File.Copy(backupPath, destination, true);
        }

        public static void QuickUndo() {
            try {
                var path = IOHelper.GetCurrentBeatmap();
                var backupFile = new DirectoryInfo(SettingsManager.GetBackupsPath()).GetFiles().OrderByDescending(x => x.CreationTime).FirstOrDefault();
                if (backupFile != null) {
                    try {
                        LoadMapBackup(backupFile.FullName, path, false);
                    } catch (BeatmapIncompatibleException ex) {
                        ex.Show();
                        var result = MessageBox.Show("Do you want to load the backup anyways?", "Load backup",
                            MessageBoxButton.YesNo);
                        if (result == MessageBoxResult.Yes) {
                            LoadMapBackup(backupFile.FullName, path, true);
                        } else {
                            return;
                        }
                    }
                    Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("Backup successfully loaded!"));

                    if (SettingsManager.Settings.AutoReload) {
                        ListenerManager.ForceReloadEditor();
                    }
                }
            } catch (Exception ex) {
                ex.Show();
            }
        }

        /// <summary>
        /// Generates a temp file with the newest version of specified map and returns the path to that temp file.
        /// </summary>
        /// <param name="mapPath"></param>
        /// <returns></returns>
        private static string GetNewestVersionPath(string mapPath) {
            var editor = EditorReaderStuff.GetNewestVersionOrNot(mapPath);

            // Save temp version
            var tempPath = Path.Combine(MainWindow.AppDataPath, "temp.osu");

            Editor.SaveFile(tempPath, editor.Beatmap.GetLines());

            return tempPath;
        }
    }
}