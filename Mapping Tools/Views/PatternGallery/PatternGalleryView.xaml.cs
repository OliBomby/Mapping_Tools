using Editor_Reader;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools.PatternGallery;
using Mapping_Tools.Components.Dialogs.CustomDialog;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;
using System;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mapping_Tools.Views.PatternGallery {
    /// <summary>
    /// Interactielogica voor PatternGalleryView.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    //[HiddenTool]
    public partial class PatternGalleryView : ISavable<PatternGalleryVm>, IQuickRun, IHaveExtraProjectMenuItems {
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "patterngalleryproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Pattern Gallery Projects");

        public static readonly string ToolName = "Pattern Gallery";
        public static readonly string ToolDescription =
            $@"Import and export patterns from osu! beatmaps and create pattern collections which you can share with your friends."+Environment.NewLine+ 
            @"You can add or remove patterns by using the buttons at the bottom."+Environment.NewLine+ 
            @"To export a pattern to the current beatmap simply select one or more patterns and click the run button. You can also double-click a pattern to instantly export it."+Environment.NewLine+ 
            @"On the right there are export options which allow for additional processing on the pattern during export."+Environment.NewLine+
            @"With the 'Project' menu you can save/load/rename/import/export your pattern collections.";

        /// <summary>
        /// 
        /// </summary>
        public PatternGalleryView()
        {
            InitializeComponent();
            DataContext = new PatternGalleryVm();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            ProjectManager.LoadProject(this, message: false);
            InitializeOsuPatternFileHandler();
        }

        public PatternGalleryVm ViewModel => (PatternGalleryVm)DataContext;

        public event EventHandler RunFinished;

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var bgw = sender as BackgroundWorker;
            e.Result = ExportPattern((PatternGalleryVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            RunTool(ViewModel.ExportTimeMode == ExportTimeMode.Current
                ? new[] { IOHelper.GetCurrentBeatmapOrCurrentBeatmap() }
                : MainWindow.AppWindow.GetCurrentMaps(), quick: false);
        }

        public void QuickRun()
        {
            RunTool(new[] {IOHelper.GetCurrentBeatmapOrCurrentBeatmap()}, quick: true);
        }

        private void RunTool(string[] paths, bool quick = false)
        {
            if (!CanRun) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            BackupManager.SaveMapBackup(paths);

            ViewModel.Paths = paths;
            ViewModel.Quick = quick;

            BackgroundWorker.RunWorkerAsync(DataContext);

            CanRun = false;
        }

        private string ExportPattern(PatternGalleryVm args, BackgroundWorker worker, DoWorkEventArgs _) {
            EditorReader reader;
            double exportTime = 0;
            bool usePatternOffset = false;
            switch (args.ExportTimeMode) {
                case ExportTimeMode.Current:
                    try {
                        reader = EditorReaderStuff.GetFullEditorReader();
                        exportTime = reader.EditorTime();
                    }
                    catch (Exception e) {
                        throw new Exception("Could not fetch the current editor time.", e);
                    }
                    break;
                case ExportTimeMode.Pattern:
                    reader = EditorReaderStuff.GetFullEditorReaderOrNot();
                    usePatternOffset = true;
                    break;
                case ExportTimeMode.Custom:
                    reader = EditorReaderStuff.GetFullEditorReaderOrNot();
                    exportTime = args.CustomExportTime;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(ExportTimeMode), "Invalid value encountered");
            }
           
            var editor = EditorReaderStuff.GetNewestVersionOrNot(args.Paths[0], reader);

            var patternCount = args.Patterns.Count(o => o.IsSelected);

            if (patternCount == 0)
                throw new Exception("No pattern has been selected to export.");

            var patternPlacer = args.OsuPatternPlacer;
            foreach (var pattern in args.Patterns.Where(o => o.IsSelected)) {
                var patternBeatmap = pattern.GetPatternBeatmap(args.FileHandler);

                if (usePatternOffset) {
                    patternPlacer.PlaceOsuPattern(patternBeatmap, editor.Beatmap, protectBeatmapPattern:false);
                } else {
                    patternPlacer.PlaceOsuPatternAtTime(patternBeatmap, editor.Beatmap, exportTime, false);
                }

                // Increase pattern use count and time
                pattern.UseCount++;
                pattern.LastUsedTime = DateTime.Now;
            }

            editor.SaveFile();

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, args.Quick));

            return "Successfully exported pattern!";
        }

        private void InitializeOsuPatternFileHandler() {
            // Make sure the file handler always uses the right pattern files folder
            if (ViewModel.FileHandler != null) {
                ViewModel.FileHandler.BasePath = DefaultSaveFolder;
                ViewModel.FileHandler.EnsureCollectionFolderExists();
            }
        }

        public PatternGalleryVm GetSaveData()
        {
            return (PatternGalleryVm) DataContext;
        }

        public void SetSaveData(PatternGalleryVm saveData)
        {
            DataContext = saveData;
            InitializeOsuPatternFileHandler();
        }

        public MenuItem[] GetMenuItems() {
            var renameMenu = new MenuItem {
                Header = "_Rename collection", Icon = new PackIcon { Kind = PackIconKind.Rename },
                ToolTip = "Rename this collection and the collection's directory in the Pattern Files directory."
            };
            renameMenu.Click += DoRenameCollection;

            var importMenu = new MenuItem {
                Header = "_Import collection", Icon = new PackIcon { Kind = PackIconKind.Import },
                ToolTip = "Import a collection zip file to the projects folder."
            };
            importMenu.Click += DoImportCollection;

            var exportMenu = new MenuItem {
                Header = "_Export collection", Icon = new PackIcon { Kind = PackIconKind.Export },
                ToolTip = "Export this collection to the Exports folder. The exported file can later be imported with the import menu."
            };
            exportMenu.Click += DoExportCollection;

            return new[] { renameMenu, importMenu, exportMenu };
        }

        private async void DoRenameCollection(object sender, RoutedEventArgs e) {
            try {
                var viewModel = new CollectionRenameVm {
                    NewName = ViewModel.CollectionName, 
                    NewFolderName = ViewModel.FileHandler.CollectionFolderName
                };

                var dialog = new CustomDialog(viewModel, 0);
                var result = await DialogHost.Show(dialog, "RootDialog");

                if (!(bool)result) return;

                ViewModel.CollectionName = viewModel.NewName;
                ViewModel.FileHandler.RenameCollectionFolder(viewModel.NewFolderName);

                await Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("Successfully renamed this collection!"));
            } catch (ArgumentException) { } catch (Exception ex) {
                ex.Show();
            }
        }

        private async void DoImportCollection(object sender, RoutedEventArgs e) {
            try {
                var path = IOHelper.ZipFileDialog();
                if (string.IsNullOrEmpty(path)) return;

                string archiveFolderName;
                using (ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Read)) {
                    // Assuming the first folder in the zip file is the collection folder
                    archiveFolderName = archive.Entries[0].FullName.Split('\\')[0];

                    if (ViewModel.FileHandler.CollectionFolderExists(archiveFolderName)) {
                        throw new DuplicateNameException($"A collection with the name \"{archiveFolderName}\" already exists in {ViewModel.FileHandler.BasePath}.");
                    }

                    archive.ExtractToDirectory(ViewModel.FileHandler.BasePath);
                }

                await Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("Successfully imported the collection!"));

                var result = MessageBox.Show(
                        "Do you want to load the newly imported collection right now?\n Warning: Unsaved changes will be lost.",
                        "Load new collection",
                        MessageBoxButton.YesNo);

                if (result == MessageBoxResult.Yes) {
                    string collectionFolderPath = Path.Combine(ViewModel.FileHandler.BasePath, archiveFolderName);
                    // Get the first .json file in the imported collection folder
                    string savePath = Directory.GetFiles(collectionFolderPath).First(o => Path.GetExtension(o) == ".json");
                    var project = ProjectManager.LoadJson<PatternGalleryVm>(savePath);

                    SetSaveData(project);
                }
            } catch (ArgumentException) { } catch (Exception ex) {
                ex.Show();
            }
        }

        private async void DoExportCollection(object sender, RoutedEventArgs e) {
            try {
                string exportFolder = MainWindow.ExportPath;
                string saveName = ViewModel.CollectionName;
                string savePath = Path.Combine(exportFolder, saveName + ".zip");

                using (FileStream zipToOpen = new FileStream(savePath, FileMode.OpenOrCreate)) {
                    using (ZipArchive archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create)) {
                        // Write the save json in the archive
                        var saveEntryName = Path.Combine(ViewModel.FileHandler.CollectionFolderName, saveName + ".json");
                        ZipArchiveEntry saveEntry = archive.CreateEntry(saveEntryName);
                        using (StreamWriter writer = new StreamWriter(saveEntry.Open())) {
                            ProjectManager.WriteJson(writer, GetSaveData());
                        }

                        // Add the folder of pattern files
                        foreach (var pattern in ViewModel.Patterns) {
                            var patternFilePath = ViewModel.FileHandler.GetPatternPath(pattern.FileName);
                            var entryName = ViewModel.FileHandler.GetPatternRelativePath(pattern.FileName);
                            archive.CreateEntryFromFile(patternFilePath, entryName);
                        }
                    }
                }

                await Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("Successfully exported this collection!"));
                ShowSelectedInExplorer.FilesOrFolders(savePath);
            } catch (ArgumentException) { } catch (Exception ex) {
                ex.Show();
            }
        }

        private void PatternRow_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            try {
                QuickRun();
            } catch (Exception ex) { ex.Show(); }
        }

        private void CollectionName_MouseDown(object sender, MouseButtonEventArgs e) {
            // Check for double-click, because there is no MouseDoubleClick event
            if (e.ClickCount == 2) {
                DoRenameCollection(sender, e);
            }
        }
    }
}
