using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor MetadataManagerView.xaml
    /// </summary>
    public partial class MetadataManagerView : ISavable<MetadataManagerVm> {
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "metadataproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Metadata Manager Projects");

        public static readonly string ToolName = "Metadata Manager";

        public static readonly string ToolDescription = $@"To save you the time of editing metadata on every individual difficulty, edit metadata in this tool and copy it to multiple diffs anytime.{Environment.NewLine}You can also import metadata from beatmaps, so you can copy metadata from A to B.{Environment.NewLine}Save and load metadata configurations, so you can work on multiple mapsets without hassle.";

        public MetadataManagerView() {
            InitializeComponent();
            DataContext = new MetadataManagerVm();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Copy_Metadata((MetadataManagerVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            var filesToCopy = ((MetadataManagerVm)DataContext).ExportPath.Split('|');
            foreach (var fileToCopy in filesToCopy) {
                IOHelper.SaveMapBackup(fileToCopy);
            }

            BackgroundWorker.RunWorkerAsync((MetadataManagerVm)DataContext);
            CanRun = false;
        }


        private static string Copy_Metadata(MetadataManagerVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            var paths = arg.ExportPath.Split('|');
            var mapsDone = 0;

            var editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (var path in paths) {
                var editor = EditorReaderStuff.GetBeatmapEditor(path, reader, editorRead);
                var beatmap = editor.Beatmap;

                beatmap.Metadata["ArtistUnicode"].StringValue = arg.Artist;
                beatmap.Metadata["Artist"].StringValue = arg.RomanisedArtist;
                beatmap.Metadata["TitleUnicode"].StringValue = arg.Title;
                beatmap.Metadata["Title"].StringValue = arg.RomanisedTitle;
                beatmap.Metadata["Creator"].StringValue = arg.BeatmapCreator;
                beatmap.Metadata["Source"].StringValue = arg.Source;
                beatmap.Metadata["Tags"].StringValue = arg.Tags;

                beatmap.General["PreviewTime"] = new TValue(arg.PreviewTime.ToRoundInvariant());
                if (arg.UseComboColours) {
                    beatmap.ComboColours = new List<ComboColour>(arg.ComboColours);
                    beatmap.SpecialColours.Clear();
                    foreach (var specialColour in arg.SpecialColours) {
                        beatmap.SpecialColours.Add(specialColour.Name, specialColour);
                    }
                }

                // Save the file
                editor.SaveFile();

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(++mapsDone * 100 / paths.Length);
                }
            }

            // Make an accurate message
            var message = $"Successfully exported metadata to {mapsDone} {(mapsDone == 1 ? "beatmap" : "beatmaps")}!";
            return message;
        }

        public MetadataManagerVm GetSaveData() {
            return (MetadataManagerVm)DataContext;
        }

        public void SetSaveData(MetadataManagerVm saveData) {
            DataContext = saveData;
        }
    }
}
