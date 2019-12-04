using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.ComboColourStudio;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;

namespace Mapping_Tools.Views {
    /// <summary>
    /// Interactielogica voor ComboColourStudioView.xaml
    /// </summary>
    public partial class ComboColourStudioView : ISavable<ComboColourProject> {
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "combocolourproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Combo Colour Studio Projects");

        public static readonly string ToolName = "Combo Colour Studio";

        public static readonly string ToolDescription = $@"With Combo Colour Studio you can easily customize the combo colours of your beatmap.{Environment.NewLine}You define colored sections much like how you use timing points in the osu! editor. Just add a new colour point and define the sequence of combo colours.{Environment.NewLine}You can also define colour points which only work for one combo, so you can emphasize specific patterns using colour.";

        private ComboColourStudioVm ViewModel => (ComboColourStudioVm) DataContext;

        public ComboColourStudioView() {
            InitializeComponent();
            DataContext = new ComboColourStudioVm();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Export_ComboColours((ComboColourStudioVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            ViewModel.ExportPath = MainWindow.AppWindow.GetCurrentMapsString();

            var filesToCopy = ViewModel.ExportPath.Split('|');
            foreach (var fileToCopy in filesToCopy) {
                IOHelper.SaveMapBackup(fileToCopy);
            }

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }


        private static string Export_ComboColours(ComboColourStudioVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            var paths = arg.ExportPath.Split('|');
            var mapsDone = 0;

            var orderedComboColours = arg.Project.ComboColours.OrderBy(o => o.Name).ToList();

            var editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);

            foreach (var path in paths) {
                var editor = editorRead ? EditorReaderStuff.GetNewestVersion(path, reader) : new BeatmapEditor(path);
                var beatmap = editor.Beatmap;

                // Setting the combo colours
                beatmap.ComboColours = new List<ComboColour>(arg.Project.ComboColours);

                // Setting the combo skips
                if (beatmap.HitObjects.Count > 0 && arg.Project.ColourPoints.Count > 0) {
                    int lastColourPointColourIndex = -1;
                    var lastColourPoint = arg.Project.ColourPoints[0];
                    int lastColourIndex = 0;
                    var exceptions = new List<ColourPoint>();
                    foreach (var newCombo in beatmap.HitObjects.Where(o => o.NewCombo || o == beatmap.HitObjects[0])) {
                        // Get the colour point for this new combo
                        var colourPoint = GetColourPoint(arg.Project.ColourPoints, newCombo.Time, exceptions);

                        // Add the colour point to the exceptions so it doesnt get used again
                        if (colourPoint.Mode == ColourPointMode.Burst) {
                            exceptions.Add(colourPoint);
                        }

                        // Get the last colour index on the sequence of this colour point
                        lastColourPointColourIndex = lastColourPointColourIndex == -1 || lastColourPoint.Equals(colourPoint) ? 
                            lastColourPointColourIndex : 
                            colourPoint.ColourSequence.IndexOf(lastColourPoint.ColourSequence[lastColourPointColourIndex]);
                        // Get the next colour index on this colour point
                        var colourPointColourIndex = lastColourPointColourIndex == -1
                            ? 0
                            : Mod(lastColourPointColourIndex + 1, colourPoint.ColourSequence.Count);
                        //Console.WriteLine("colourPointColourIndex: " + colourPointColourIndex);
                        //Console.WriteLine("colourPointColour: " + colourPoint.ColourSequence[colourPointColourIndex].Name);

                        // Find the combo index of the chosen colour in the sequence
                        var colourIndex =
                            orderedComboColours.FindIndex(o => o.Name == colourPoint.ColourSequence[colourPointColourIndex].Name);

                        //Console.WriteLine("colourIndex: " + colourIndex);

                        var comboChange = colourIndex - lastColourIndex;

                        // Do -1 combo skip since it always does +1 combo colour for each new combo
                        newCombo.ComboSkip = Mod(comboChange - 1, arg.Project.ComboColours.Count);
                        // Set new combo to true for the case this is the first object and new combo is false
                        if (!newCombo.NewCombo && newCombo.ComboSkip != 0) {
                            newCombo.NewCombo = true;
                        }

                        //Console.WriteLine("comboSkip: " + newCombo.ComboSkip);

                        lastColourPointColourIndex = colourPointColourIndex;
                        lastColourPoint = colourPoint;
                        lastColourIndex = colourIndex;
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

        private static ColourPoint GetColourPoint(ObservableCollection<ColourPoint> colourPoints, double time, List<ColourPoint> exceptions) {
            return colourPoints.Except(exceptions).LastOrDefault(o => o.Time <= time) ?? colourPoints.Except(exceptions).FirstOrDefault() ?? colourPoints[0];
        }
        private static int Mod(int x, int m) {
            int r = x % m;
            return r < 0 ? r + m : r;
        }

        public ComboColourProject GetSaveData() {
            return ViewModel.Project;
        }

        public void SetSaveData(ComboColourProject saveData) {
            ViewModel.Project = saveData;
        }
    }
}
