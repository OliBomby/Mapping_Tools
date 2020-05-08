using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.Aspirenator {
    /// <summary>
    /// Interactielogica voor AspirenatorView.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.Always)]
    public partial class AspirenatorView : IQuickRun, ISavable<AspirenatorVm> {
        public static readonly string ToolName = "Aspirenator";

        public static readonly string ToolDescription = $@"";

        public AspirenatorView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            DataContext = new AspirenatorVm();
            ProjectManager.LoadProject(this, message: false);
        }

        public AspirenatorVm ViewModel => (AspirenatorVm) DataContext;

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Aspirenate((AspirenatorVm) e.Argument, bgw, e);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps());
        }

        public void QuickRun() {
            RunTool(new[] {IOHelper.GetCurrentBeatmap()}, true);
        }

        private void RunTool(string[] paths, bool quick = false) {
            if (!CanRun) return;

            IOHelper.SaveMapBackup(paths);

            ViewModel.Paths = paths;
            ViewModel.Quick = quick;

            BackgroundWorker.RunWorkerAsync(ViewModel);

            CanRun = false;
        }

        private string Aspirenate(AspirenatorVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            int zeroSliders = 0;
            int fixedZeroSliders = 0;
            int bugSliders = 0;

            var reader = EditorReaderStuff.GetFullEditorReaderOrNot();

            foreach (string path in arg.Paths) {
                // Open beatmap
                var editor = EditorReaderStuff.GetNewestVersionOrNot(path, reader);
                Beatmap beatmap = editor.Beatmap;

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) {
                    worker.ReportProgress(20);
                }

                // Change all sliders with 100% pixellength to 0 pixellength
                // Change all nearly straight and super long bezier sliders to passthrough
                foreach (var ho in beatmap.HitObjects.Where(h => h.IsSlider && h.CurvePoints.Count < 1000)) {
                    var fullLength = ho.GetSliderPath(fullLength: true).Distance;

                    if (arg.DoZeroSliders && !arg.FixZeroSliders && Math.Abs(ho.PixelLength - fullLength) < arg.Leniency) {
                        ho.PixelLength = 0;
                        zeroSliders++;
                    }

                    if (arg.DoZeroSliders && arg.FixZeroSliders && ho.PixelLength == 0) {
                        ho.PixelLength = fullLength;
                        fixedZeroSliders++;
                    }

                    if (arg.DoBugSliders && ho.SliderType == PathType.Bezier && ho.CurvePoints.Count == 2 &&
                        ho.CurvePoints.Last().Length > 1000) {
                        ho.SliderType = PathType.PerfectCurve;
                        bugSliders++;
                    }
                }

                // Save the file
                editor.SaveFile();
            }
            

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) {
                worker.ReportProgress(100);
            }

            // Do QuickRun stuff
            if (arg.Quick)
                RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null));

            // Make an accurate message
            string message = $"Succesfully set 0 pixellength to {zeroSliders} sliders, fixed {fixedZeroSliders} sliders, and bugged {bugSliders} sliders!";

            return arg.Quick ? string.Empty : message;
        }

        private static bool CheckMpB(double mpbNew, IEnumerable<Marker> markers, TimingPoint redline, TimingHelperVm arg) {
            // For each their beatsFromRedline must stay the same AND their time must be within leniency of their resnapped time
            // If any of these times becomes incompatible, place a new anchor on the last time and not change the previous redline
            double mpbOld = redline.MpB;
            double beatsFromRedline = 0;
            bool canChangeRedline = true;
            foreach (Marker markerB in markers) {
                double timeB = markerB.Time;
                beatsFromRedline += markerB.BeatsFromLastMarker;

                // Get the beatsFromRedline after changing mpb
                redline.MpB = mpbNew;
                double resnappedTimeBa = redline.Offset + redline.MpB * beatsFromRedline;
                double beatsFromRedlineBa = (resnappedTimeBa - redline.Offset) / redline.MpB;

                // Change MpB back so the redline doesn't get changed
                redline.MpB = mpbOld;

                // Check changes
                if (MathHelper.ApproximatelyEquivalent(beatsFromRedlineBa, beatsFromRedline, 0.1) && IsSnapped(timeB, resnappedTimeBa, arg.Leniency)) {
                    continue;
                }
                canChangeRedline = false;
            }
            return canChangeRedline;
        }

        private static double HumanRoundMpB(double mpb, IReadOnlyCollection<Marker> markers, TimingPoint redline, TimingHelperVm arg) {
            double bpm = 60000 / mpb;

            // Round bpm
            double mpbInteger = 60000 / Math.Round(bpm);
            if (CheckMpB(mpbInteger, markers, redline, arg)) {
                return mpbInteger;
            }

            // Halves bpm
            double mpbHalves = 60000 / (Math.Round(bpm * 2) / 2);
            if (CheckMpB(mpbHalves, markers, redline, arg)) {
                return mpbHalves;
            }

            // Tenths bpm
            double mpbTenths = 60000 / (Math.Round(bpm * 10) / 10);
            if (CheckMpB(mpbTenths, markers, redline, arg)) {
                return mpbTenths;
            }

            // Hundredths bpm
            double mpbHundredths = 60000 / (Math.Round(bpm * 100) / 100);
            if (CheckMpB(mpbHundredths, markers, redline, arg)) {
                return mpbHundredths;
            }

            // Thousandths bpm
            double mpbThousandths = 60000 / (Math.Round(bpm * 1000) / 1000);
            if (CheckMpB(mpbThousandths, markers, redline, arg)) {
                return mpbThousandths;
            }

            // Return exact bpm
            return mpb;
        }

        private static double GetMpB(double timeFromRedline, double beatsFromRedline, double leniency) {
            // Will make human-like BPM values like integers, halves and tenths
            // If that doesn't work (like the time is really far from the redline) it will try thousandths
            
            // Exact MpB and BPM
            double mpb = timeFromRedline / beatsFromRedline;
            double bpm = 60000 / mpb;

            // Round bpm
            double mpbInteger = 60000 / Math.Round(bpm);
            if (IsSnapped(timeFromRedline, mpbInteger * beatsFromRedline, leniency)) {
                return mpbInteger;
            }

            // Halves bpm
            double mpbHalves = 60000 / (Math.Round(bpm * 2) / 2);
            if (IsSnapped(timeFromRedline, mpbHalves * beatsFromRedline, leniency)) {
                return mpbHalves;
            }

            // Tenths bpm
            double mpbTenths = 60000 / (Math.Round(bpm * 10) / 10);
            if (IsSnapped(timeFromRedline, mpbTenths * beatsFromRedline, leniency)) {
                return mpbTenths;
            }

            // Hundredths bpm
            double mpbHundredths = 60000 / (Math.Round(bpm * 100) / 100);
            if (IsSnapped(timeFromRedline, mpbHundredths * beatsFromRedline, leniency)) {
                return mpbHundredths;
            }

            // Thousandths bpm
            double mpbThousandths = 60000 / (Math.Round(bpm * 1000) / 1000);
            if (IsSnapped(timeFromRedline, mpbThousandths * beatsFromRedline, leniency)) {
                return mpbThousandths;
            }

            // Return exact bpm
            return mpb;
        }

        private static bool IsSnapped(double time, double resnappedTime, double leniency = 3) {
            return Math.Abs(resnappedTime - time) <= leniency;
        }

        private class Marker {
            public double Time { get; }
            public double BeatsFromLastMarker { get; set; }

            public Marker(double time) {
                Time = time;
                BeatsFromLastMarker = 0;
            }
        }

        public event EventHandler RunFinished;

        public AspirenatorVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(AspirenatorVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "aspirenatorproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Aspirenator Projects");
    }
}
