using System;
using System.IO;
using System.Linq;

namespace Mapping_Tools.Classes.SystemTools {
    public static class BackupManager {
        public static bool SaveMapBackup(string fileToCopy, bool forced = false, string customFileName = "") {
            if (!SettingsManager.GetMakeBackups() && !forced)
                return false;

            DateTime now = DateTime.Now;
            string destinationDirectory = SettingsManager.GetBackupsPath();
            try {
                File.Copy(fileToCopy,
                    Path.Combine(destinationDirectory, now.ToString("yyyy-MM-dd HH-mm-ss") + "___" +
                                                       (string.IsNullOrEmpty(customFileName) ? Path.GetFileName(fileToCopy) : customFileName)),
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

        public static bool SaveMapBackup(string[] filesToCopy, bool forced = false) {
            bool result = true;
            foreach (string fileToCopy in filesToCopy) {
                result = SaveMapBackup(fileToCopy, forced) && result;
                if (!result)
                    break;
            }
            return result;
        }
    }
}