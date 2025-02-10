using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Drawing;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Viewmodels;

namespace Mapping_Tools.Views.SliderPicturator {
    /// <summary>
    /// Interaktionslogik für SliderPicturatorView.xaml
    /// </summary>
    [SmartQuickRunUsage(SmartQuickRunTargets.AnySelection)]
    [VerticalContentScroll]
    [HorizontalContentScroll]
    public partial class SliderPicturatorView : IQuickRun, ISavable<SliderPicturatorVm> {
        public event EventHandler RunFinished;

        public static readonly string ToolName = "Slider Picturator";

        public static readonly string ToolDescription = $@"Import an image and this program will distort a slider into it! To get started click the Browse button to select an image, then play with the colors and options until it looks right. Click the run button to export the slider picture at the specified time and position.";

        /// <inheritdoc />
        public SliderPicturatorView()
        {
            InitializeComponent();
            Width = MainWindow.AppWindow.ContentViews.Width;
            Height = MainWindow.AppWindow.ContentViews.Height;
            DataContext = new SliderPicturatorVm();
            ProjectManager.LoadProject(this, message: false);
        }

        public SliderPicturatorVm ViewModel => (SliderPicturatorVm) DataContext;

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Picturate((SliderPicturatorVm) e.Argument, bgw, e);
        }

       
        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps()[0], SettingsManager.Settings.AlwaysQuickRun);
        }

        public void QuickRun() {
            RunTool(IOHelper.GetCurrentBeatmapOrCurrentBeatmap(), quick: true);
        }

        private void RunTool(string path, bool quick = false) {
            if (!CanRun) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            BackupManager.SaveMapBackup(path);

            ViewModel.Path = path;
            ViewModel.Quick = quick;

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        private string Picturate(SliderPicturatorVm arg, BackgroundWorker worker, DoWorkEventArgs _) {
            if (arg.PictureFile == null) {
                throw new Exception("No image file selected.");
            }

            Bitmap img;
            try {
                img = new Bitmap(arg.PictureFile);
            } catch {
                throw new Exception("Not a valid image file.");
            }

            // Get the latest version of the beatmap
            var reader = EditorReaderStuff.GetFullEditorReaderOrNot(out var _);
            var editor = EditorReaderStuff.GetNewestVersionOrNot(arg.Path, reader, out var _, out var _);
            var beatmap = editor.Beatmap;

            long GPU = arg.ViewportSize;
            bool R = arg.RedOn;
            bool G = arg.GreenOn;
            bool B = arg.BlueOn;
            bool borderOff = !arg.BorderOn;
            bool blackOff = !arg.BlackOn;
            bool opaqueOff = !arg.AlphaOn;
            int quality = arg.Quality;
            double startTime = arg.TimeCode;
            double startPosX = arg.SliderStartX;
            double startPosY = arg.SliderStartY;
            double startPosPicX = arg.ImageStartX;
            double startPosPicY = arg.ImageStartY;
            double duration = arg.SelectedSlider?.TemporalLength ?? arg.Duration;
            double resY = arg.YResolution;
            Vector2 startPos = new Vector2(startPosX, startPosY);
            Vector2 startPosPic = new Vector2(startPosPicX, startPosPicY);

            var circleSize = beatmap.Difficulty["CircleSize"].DoubleValue;
            Color sliderColor = arg.CurrentTrackColor;
            Color borderColor = Color.FromArgb(arg.BorderColor.R, arg.BorderColor.G, arg.BorderColor.B);
            Color backgroundColor = Color.FromArgb(0,0,0);

            //string[] files = Directory.GetFiles(@"C:\Users\User\Desktop\badapple\images-6fps@360p", "*.png");
            //double TIME_SPACING = 1000/6;
            //double time = 0;
            //HitObject ho;
            //List<Vector2> sliderPath;
            //foreach (string file in files) {
            //    img = new Bitmap(file);
            //    sliderPath = SliderPicturator.Picturate(img, sliderColor, sliderBorder, backgroundColor, circleSize, resY, GPU, false, true);
            //    ho = new HitObject(time, 0, SampleSet.None, SampleSet.None)
            //    {
            //        IsCircle = false,
            //        IsSpinner = false,
            //        IsHoldNote = false,
            //        IsSlider = true
            //    };
            //    ho.SetAllCurvePoints(sliderPath);
            //    ho.SliderType = PathType.Linear;
            //    ho.PixelLength = OsuStableDistance(sliderPath);
            //    beatmap.HitObjects.Add(ho);
            //    time += TIME_SPACING;
            //    worker.ReportProgress((int)Math.Round(100 * (time / TIME_SPACING) / files.Length));
            //}

            var (sliderPath, frameDist) = Classes.Tools.SlideratorStuff.SliderPicturator.Picturate(img, sliderColor, borderColor, backgroundColor, circleSize, startPos, startPosPic, arg.SelectedSlider, resY, GPU, blackOff, borderOff, opaqueOff, R, G, B, quality);

            // Find nearest hitobject before startTime and get its combo color index
            int currentColorIdx = 0;
            int idx = beatmap.HitObjects.Select(hitObject => hitObject.Time).ToList().BinarySearch(startTime);
            if (idx < 0) {
                idx = ~idx - 1;
            }
            if (idx >= 0) {
                currentColorIdx = beatmap.HitObjects[idx].ColourIndex;
            }

            // Get requested color's combo color index, if it exists
            int foundColorIdx = beatmap.ComboColours.FindIndex(cc => cc.Color.R == sliderColor.R && cc.Color.G == sliderColor.G && cc.Color.B == sliderColor.B);
            if (foundColorIdx == -1) {
                foundColorIdx = 0;
            }

            var ho = new HitObject(startTime, 0, SampleSet.None, SampleSet.None)
            {
                IsCircle = false,
                IsSpinner = false,
                IsHoldNote = false,
                IsSlider = true,
                ComboSkip = foundColorIdx - currentColorIdx - 1
            };
            ho.SetAllCurvePoints(sliderPath);
            ho.SliderType = PathType.Linear;
            ho.PixelLength = OsuStableDistance(sliderPath);
            beatmap.HitObjects.Add(ho);
            beatmap.SortHitObjects();

            var timing = beatmap.BeatmapTiming;
            var tpAfter = timing.GetRedlineAtTime(ho.Time).Copy();
            var tpOn = tpAfter.Copy();

            tpAfter.Offset = ho.Time;
            tpOn.Offset = ho.Time - 1;  // This one will be on the slider

            tpAfter.OmitFirstBarLine = true;
            tpOn.OmitFirstBarLine = true;

            // Express velocity in BPM
            if (frameDist == 0) {
                // We want ho.PixelLength = 5/3*timing.SliderMultiplier*bpm*duration/1000
                // so bpm = ho.PixelLength*600/(timing.SliderMultiplier*duration).
                // Converting to MpB we get 60000*(timing.SliderMultiplier*duration/(ho.PixelLength*600))
                // = 100*timing.SliderMultiplier*duration/ho.PixelLength
                tpOn.MpB = 100 * timing.SliderMultiplier * duration / ho.PixelLength;
            } else {
                // We want frameDist = 5/3*timing.SliderMultiplier*bpm/1000
                // so MpB = 100*timing.SliderMultiplier/frameDist
                tpOn.MpB = 100 * timing.SliderMultiplier / frameDist;
            }
            // NaN SV results in removal of slider ticks
            ho.SliderVelocity = double.NaN;

            // Add redlines
            var timingPointsChanges = new List<TimingPointsChange> {
                new(tpOn, mpb: true, unInherited: true, omitFirstBarLine: true, fuzzyness: Precision.DoubleEpsilon),
                new(tpAfter, mpb: true, unInherited: true, omitFirstBarLine: true, fuzzyness: Precision.DoubleEpsilon)
            };

            ho.Time -= 1;

            timingPointsChanges.AddRange(beatmap.HitObjects.Select(bmho => {
                var sv = bmho == ho ? bmho.SliderVelocity : timing.GetSvAtTime(bmho.Time);
                var tp = timing.GetTimingPointAtTime(bmho.Time).Copy();
                tp.MpB = sv;
                tp.Offset = bmho.Time;
                return new TimingPointsChange(tp, mpb: true, fuzzyness: Precision.DoubleEpsilon);
            }));

            TimingPointsChange.ApplyChanges(timing, timingPointsChanges);

            // Set the beatmap slider colors
            if (arg.SetBeatmapColors) {
                if (!arg.UseMapComboColors)
                    beatmap.SpecialColours["SliderTrackOverride"] = new ComboColour(sliderColor.R, sliderColor.G, sliderColor.B);
                beatmap.SpecialColours["SliderBorder"] = new ComboColour(borderColor.R, borderColor.G, borderColor.B);
            }

            editor.SaveFile();

            // Complete progressbar
            if (worker is { WorkerReportsProgress: true })
            {
                worker.ReportProgress(100);
            }

            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true, reader != null, arg.Quick));
            return arg.Quick ? "" : "Done!";
        }
        public SliderPicturatorVm GetSaveData() {
            return ViewModel;
        }

        public void SetSaveData(SliderPicturatorVm saveData) {
            DataContext = saveData;
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "sliderpicturatorproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Slider Picturator Projects");

        private static double OsuStableDistance(List<Vector2> controlPoints)
        {
            double length = 0;
            for (int i = 1; i < controlPoints.Count; i++) {
                Vector2 lp = controlPoints.ElementAt(i - 1);
                Vector2 cp = controlPoints.ElementAt(i);
                float num1 = (float)Math.Round(lp.X) - (float)Math.Round(cp.X);
                float num2 = (float)Math.Round(lp.Y) - (float)Math.Round(cp.Y);
                float num3 = num1 * num1 + num2 * num2;

                length += (float)Math.Sqrt(num3);
            }
            return length;
        }
    }
}
