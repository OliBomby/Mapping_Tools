using System;
using System.IO;
using System.Linq;

namespace Mapping_Tools.Classes.SystemTools {
    public static class BackupManager {
        public static bool SaveMapBackup(string fileToCopy, bool forced = false, string customFileName = "", string backupCode = "") {
            if (!SettingsManager.GetMakeBackups() && !forced)
                return false;

            DateTime now = DateTime.Now;
            string destinationDirectory = SettingsManager.GetBackupsPath();
            try {
                var name = now.ToString("yyyy-MM-dd HH-mm-ss") + "_" + backupCode + "__" +
                           (string.IsNullOrEmpty(customFileName) ? Path.GetFileName(fileToCopy) : customFileName);

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
    }
}