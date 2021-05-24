using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.HitsoundPreviewHelper
{
    /// <summary>
    /// Interactielogica voor HitsoundCopierView.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    public partial class HitsoundPreviewHelperView : ISavable<HitsoundPreviewHelperVm>, IQuickRun
    {
        public event EventHandler RunFinished;

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "hspreviewproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Hitsound Preview Projects");

        public static readonly string ToolName = "Hitsound Preview Helper";
        public static readonly string ToolDescription =
            $@"Hitsound Preview Helper helps by placing hitsounds on all the objects of the current map based on the positions of the objects. " +
            $@"That way you can hear the hitsounds play while you hitsound without having to assign them manually and later import them to Hitsound Studio." +
            $@"{Environment.NewLine}This tool is meant to help a very specific hitsounding workflow." +
            $@" If you hitsound by placing circles on different parts on the screen and treat each position as a different layer of hitsounds." +
            $@" For example using a mania map and have each column represent a different sound.";

        /// <summary>
        /// 
        /// </summary>
        public HitsoundPreviewHelperView()
        {
            InitializeComponent();
            DataContext = new HitsoundPreviewHelperVm();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var bgw = sender as BackgroundWorker;
            e.Result = PlaceHitsounds((Arguments) e.Argument, bgw, e);
        }

        private struct Arguments
        {
            public string[] Paths;
            public bool Quick;
            public List<HitsoundZone> Zones;

            public Arguments(string[] paths, bool quick, List<HitsoundZone> zones)
            {
                Paths = paths;
                Quick = quick;
                Zones = zones;
            }
        }

        private string PlaceHitsounds(Arguments args, BackgroundWorker worker, DoWorkEventArgs _)
        {
            if (args.Zones.Count == 0)
                return "There are no zones!";

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            foreach (string path in args.Paths)
            {
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader);
                Beatmap beatmap = editor.Beatmap;
                Timeline timeline = beatmap.GetTimeline();

                for (int i = 0; i < timeline.TimelineObjects.Count; i++)
                {
                    var tlo = timeline.TimelineObjects[i];

                    var column = args.Zones.FirstOrDefault();
                    double best = double.MaxValue;
                    foreach (var c in args.Zones)
                    {
                        double dist = c.Distance(tlo.Origin.Pos);
                        if (dist < best)
                        {
                            best = dist;
                            column = c;
                        }
                    }

                    if (column == null) continue;

                    tlo.Filename = column.Filename;
                    tlo.SampleSet = column.SampleSet;
                    tlo.AdditionSet = column.AdditionsSet;
                    tlo.CustomIndex = column.CustomIndex;
                    tlo.SetHitsound(column.Hitsound);
                    tlo.HitsoundsToOrigin();

                    UpdateProgressBar(worker, (int) (100f * i / beatmap.HitObjects.Count));
                }

                // Save the file
                editor.SaveFile();
            }

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, args.Quick));

            return args.Quick ? "" : "Done!";
        }

        private void Start_Click(object sender, RoutedEventArgs e)
        {
            RunTool(MainWindow.AppWindow.GetCurrentMaps(), quick: false);
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

            BackgroundWorker.RunWorkerAsync(new Arguments(paths, quick,
                ((HitsoundPreviewHelperVm) DataContext).Items.ToList()));

            CanRun = false;
        }

        public HitsoundPreviewHelperVm GetSaveData()
        {
            return (HitsoundPreviewHelperVm) DataContext;
        }

        public void SetSaveData(HitsoundPreviewHelperVm saveData)
        {
            DataContext = saveData;

        }

    }
}
