using System;
using System.IO;
using Mapping_Tools.Classes.BeatmapHelper;
using Microsoft.WindowsAPICodePack.Dialogs;
using OsuMemoryDataProvider;
using Mapping_Tools.Classes.ToolHelpers;
using Microsoft.Win32;
using OsuMemoryDataProvider.OsuMemoryModels;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;

namespace Mapping_Tools.Classes.SystemTools;

public class IOHelper {
    private static readonly StructuredOsuMemoryReader pioStructuredReader = StructuredOsuMemoryReader.Instance;
    private static readonly OsuBaseAddresses osuBaseAddresses = new();
    private static readonly object pioReaderLock = new();

    public static string FolderDialog(string initialDirectory = "") {
        bool restore = initialDirectory == "";

        using CommonOpenFileDialog dialog = new CommonOpenFileDialog {
            IsFolderPicker = true,
            InitialDirectory = initialDirectory,
            RestoreDirectory = restore
        };

        return dialog.ShowDialog() == CommonFileDialogResult.Ok ? dialog.FileName : string.Empty;
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

    public static string ZipFileDialog() {
        OpenFileDialog saveFileDialog1 = new OpenFileDialog {
            Filter = "ZIP File|*.zip",
            FilterIndex = 1,
            RestoreDirectory = true,
            CheckFileExists = true
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

    public static string ConfigFileDialog(string initialDirectory = "") {
        bool restore = initialDirectory == "";

        OpenFileDialog openFileDialog = new OpenFileDialog {
            Filter = "Config files (*.cfg)|*.cfg",
            FilterIndex = 1,
            InitialDirectory = initialDirectory,
            RestoreDirectory = restore,
            CheckFileExists = true
        };
        openFileDialog.ShowDialog();
        return openFileDialog.FileName;
    }

    public static string MidiFileDialog() {
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
            Filter = "Sample files (*.wav;*.ogg;*.sf2)|*.wav;*.ogg;*.sf2|SoundFont files (*.sf2)|*.sf2",
            FilterIndex = 1,
            RestoreDirectory = true,
            CheckFileExists = true
        };
        openFileDialog.ShowDialog();
        return openFileDialog.FileName;
    }

    public static string[] AudioFileDialog(bool multiselect = false) {
        OpenFileDialog openFileDialog = new OpenFileDialog {
            Filter = "Audio files (*.wav;*.ogg)|*.wav;*.ogg",
            FilterIndex = 1,
            RestoreDirectory = true,
            CheckFileExists = true,
            Multiselect = multiselect
        };
        openFileDialog.ShowDialog();
        return openFileDialog.FileNames;
    }

    public static string[] BeatmapFileDialog(bool multiselect = false, bool restore = false) {
        string path = MainWindow.AppWindow.GetCurrentMaps()[0];
        OpenFileDialog openFileDialog = new OpenFileDialog {
            InitialDirectory = restore ? "" : path != "" ? Editor.GetParentFolder(path) : SettingsManager.GetSongsPath(),
            Filter = "osu! files (*.osu;*.osb)|*.osu;*.osb",
            FilterIndex = 1,
            RestoreDirectory = true,
            CheckFileExists = true,
            Multiselect = multiselect
        };
        openFileDialog.ShowDialog();
        return openFileDialog.FileNames;
    }

    public static string[] BeatmapFileDialog(string initialDirectory, bool multiselect = false) {
        string path = MainWindow.AppWindow.GetCurrentMaps()[0];
        OpenFileDialog openFileDialog = new OpenFileDialog {
            InitialDirectory = initialDirectory,
            Filter = "osu! files (*.osu;*.osb)|*.osu;*.osb",
            FilterIndex = 1,
            RestoreDirectory = true,
            CheckFileExists = true,
            Multiselect = multiselect
        };
        openFileDialog.ShowDialog();
        return openFileDialog.FileNames;
    }

    private static T ReadClassProperty<T>(object readObj, string propName, T defaultValue = default) where T : class {
        lock (pioReaderLock) {
            if (pioStructuredReader.TryReadProperty(readObj, propName, out var readResult))
                return (T) readResult;
        }

        return defaultValue;
    }

    private static string ReadString(object readObj, string propName)
        => ReadClassProperty<string>(readObj, propName);

    public static string GetCurrentBeatmap() {
        string path;
        try {
            string songs = SettingsManager.GetSongsPath();

            if (string.IsNullOrEmpty(songs)) {
                throw new Exception(
                    @"Can't fetch current in-game beatmap, because there is no Songs path specified in Preferences.");
            }

            string folder = ReadString(osuBaseAddresses.Beatmap, nameof(CurrentBeatmap.FolderName));
            string filename = ReadString(osuBaseAddresses.Beatmap, nameof(CurrentBeatmap.OsuFileName));

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
                lock (EditorReaderStuff.EditorReaderLock) {
                    var reader = EditorReaderStuff.GetEditorReader();
                    reader.SetProcess(EditorReaderStuff.GetOsuProcess());
                    reader.FetchHOM();
                    reader.FetchBeatmap();
                    path = EditorReaderStuff.GetCurrentBeatmap(reader);
                }
            }
            catch (Exception ex2) {
                Console.WriteLine(ex2.Message);
                Console.WriteLine(ex2.StackTrace);
                throw ex;
            }
        }
            
        return path;
    }

    public static string GetCurrentBeatmapOrCurrentBeatmap(bool updateCurrentBeatmap = true) {
        try {
            string path = GetCurrentBeatmap();

            if (File.Exists(path)) {
                if (updateCurrentBeatmap) {
                    MainWindow.AppWindow.SetCurrentMapsString(path);
                }

                return path;
            }
        } catch (Exception ex) {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
        }

        return MainWindow.AppWindow.GetCurrentMaps()[0];
    }

    public static void ReplaceSettingTypePaths(string path, Tuple<string, string>[] replacements) {
        if (replacements.Length == 0) return;

        var contents = File.ReadAllText(path);

        foreach (var (oldPath, newPath) in replacements) {
            contents = contents.Replace(oldPath, newPath);
        }

        File.WriteAllText(path, contents);
    }
}