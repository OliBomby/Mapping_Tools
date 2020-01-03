using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Graph;
using Mapping_Tools.Components.Graph.Markers;
using Mapping_Tools.Components.ObjectVisualiser;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.HitsoundStuff;
using Mapping_Tools.Classes.SliderPathStuff;
using Mapping_Tools.Classes.Tools;
using HitObject = Mapping_Tools.Classes.BeatmapHelper.HitObject;

namespace Mapping_Tools.Views {
    //[HiddenTool]
    public partial class SlideratorView {
        public static readonly string ToolName = "Sliderator";

        public static readonly string ToolDescription = "";

        private SlideratorVm ViewModel => (SlideratorVm) DataContext;

        private GraphMode _graphMode;

        public enum GraphMode {
            Position,
            Velocity
        }

        public SlideratorView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;

            DataContext = new SlideratorVm();
            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;

            Graph.VerticalMarkerGenerator = new DoubleMarkerGenerator(0, 0.25);
            Graph.HorizontalMarkerGenerator = new DividedBeatMarkerGenerator(4);

            Graph.SetBrush(new SolidColorBrush(Color.FromArgb(255, 0, 255, 255)));

            Graph.MoveAnchorTo(Graph.Anchors[0], Vector2.Zero);
            Graph.MoveAnchorTo(Graph.Anchors[Graph.Anchors.Count - 1], Vector2.One);

            Graph.GraphStateChanged += GraphOnGraphStateChanged;

            SetGraphMode(GraphMode.Position);
        }

        private void GraphOnGraphStateChanged(object sender, DependencyPropertyChangedEventArgs e) {
            AnimateProgress(GraphHitObjectElement);
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case nameof(ViewModel.VisibleHitObject):
                case nameof(ViewModel.GraphDuration):
                    AnimateProgress(GraphHitObjectElement);
                    break;
                case nameof(ViewModel.BeatSnapDivisor):
                    Graph.HorizontalMarkerGenerator = new DividedBeatMarkerGenerator(ViewModel.BeatSnapDivisor);
                    break;
                case nameof(ViewModel.VelocityLimit):
                    if (_graphMode == GraphMode.Velocity) {
                        Graph.MinY = -ViewModel.VelocityLimit;
                        Graph.MaxY = ViewModel.VelocityLimit;
                    }
                    break;
            }
        }

        private void AnimateProgress(HitObjectElement element) {
            if (ViewModel.VisibleHitObject == null) return;

            var graphDuration = ViewModel.GraphDuration;
            var extraDuration = graphDuration.Add(TimeSpan.FromSeconds(1));

            DoubleAnimationBase animation;
            if (_graphMode == GraphMode.Velocity) {
                animation = new GraphIntegralDoubleAnimation {
                    GraphState = Graph.GetGraphState(), From = Graph.MinX, To = Graph.MaxX,
                    Duration = graphDuration,
                    BeginTime = TimeSpan.Zero
                };
            } else {
                animation = new GraphDoubleAnimation {
                    GraphState = Graph.GetGraphState(), From = Graph.MinX, To = Graph.MaxX,
                    Duration = graphDuration,
                    BeginTime = TimeSpan.Zero
                };
            }
            var animation2 = new DoubleAnimation(0, 0, TimeSpan.FromSeconds(1)) {BeginTime = graphDuration};

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTarget(animation2, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath(HitObjectElement.ProgressProperty));
            Storyboard.SetTargetProperty(animation2, new PropertyPath(HitObjectElement.ProgressProperty));

            var timeline = new ParallelTimeline {RepeatBehavior = RepeatBehavior.Forever, Duration = extraDuration};
            timeline.Children.Add(animation);
            timeline.Children.Add(animation2);

            var storyboard = new Storyboard();
            storyboard.Children.Add(timeline);

            element.BeginStoryboard(storyboard);
        }

        private async void ScaleCompleteButton_OnClick(object sender, RoutedEventArgs e) {
            var dialog = new TypeValueDialog(1);

            var result = await DialogHost.Show(dialog, "RootDialog");

            if (!(bool) result) return;
            if (!TypeConverters.TryParseDouble(dialog.ValueBox.Text, out double value)) return;

            var maxValue = value;
            if (_graphMode == GraphMode.Velocity) {
                // Integrate the graph to get the end value
                maxValue = Graph.GetMaxIntegral();
            } else if (_graphMode == GraphMode.Position) {
                maxValue = Graph.GetMaxValue();
            }
            Graph.ScaleAnchors(new Size(1, value / maxValue));
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e) {
            var messageBoxResult = MessageBox.Show("Clear the graph?", "Confirm deletion", MessageBoxButton.YesNo);
            if (messageBoxResult != MessageBoxResult.Yes) return;

            Graph.Clear();
        }

        private void GraphToggleButton_OnClick(object sender, RoutedEventArgs e) {
            switch (_graphMode) {
                case GraphMode.Position:
                    SetGraphMode(GraphMode.Velocity);
                    break;
                case GraphMode.Velocity:
                    SetGraphMode(GraphMode.Position);
                    break;
                default:
                    SetGraphMode(GraphMode.Position);
                    break;
            }
        }

        public void SetGraphMode(GraphMode graphMode) {
            switch (graphMode) {
                case GraphMode.Position:
                    GraphToggleContentTextBlock.Text = "X";
                    Graph.HorizontalAxisVisible = false;
                    Graph.VerticalAxisVisible = false;

                    // Make sure the start point is locked at y = 0
                    Graph.StartPointLockedY = true;
                    var firstAnchor = Graph.Anchors.FirstOrDefault();
                    if (firstAnchor != null) {
                        firstAnchor.Pos = new Vector2(firstAnchor.Pos.X, 0);
                    }
                    
                    Graph.MinY = 0;
                    Graph.MaxY = 1;
                    Graph.VerticalMarkerGenerator = new DoubleMarkerGenerator(0, 0.25);
                    break;
                case GraphMode.Velocity:
                    GraphToggleContentTextBlock.Text = "V";
                    Graph.HorizontalAxisVisible = true;
                    Graph.VerticalAxisVisible = false;
                    Graph.StartPointLockedY = false;

                    Graph.MinY = -ViewModel.VelocityLimit;
                    Graph.MaxY = ViewModel.VelocityLimit;
                    Graph.VerticalMarkerGenerator = new DoubleMarkerGenerator(0, 1, "x");
                    break;
                default:
                    GraphToggleContentTextBlock.Text = "";
                    break;
            }

            _graphMode = graphMode;

            AnimateProgress(GraphHitObjectElement);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps()[0]);
        }

        private void RunTool(string path, bool quick = false) {
            if (!CanRun) return;

            IOHelper.SaveMapBackup(path);

            ViewModel.Path = path;
            ViewModel.GraphState = Graph.GetGraphState();
            if (ViewModel.GraphState.CanFreeze) {
                ViewModel.GraphState.Freeze();
            }

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Sliderate((SlideratorVm) e.Argument, bgw);
        }

        private string Sliderate(SlideratorVm arg, BackgroundWorker worker) {
            var sliderPath = arg.VisibleHitObject.GetSliderPath();
            var path = new List<Vector2>();
            sliderPath.GetPathToProgress(path, 0, 1);

            var sliderator = new Sliderator {
                PositionFunction = arg.GraphState.GetValue, MaxT = arg.GraphState.MaxX
            };
            sliderator.SetPath(path);

            var slideration = sliderator.Sliderate();

            // Exporting stuff
            var editor = new BeatmapEditor(arg.Path);
            var beatmap = editor.Beatmap;

            var hitObjectHere = beatmap.HitObjects.FirstOrDefault(o => Math.Abs(arg.ExportTime - o.Time) < 5) ??
                                new HitObject(arg.ExportTime, 0, SampleSet.Auto, SampleSet.Auto);

            var clone = new HitObject(hitObjectHere.GetLine()) {
                IsCircle = false, IsSpinner = false, IsHoldNote = false, IsSlider = true
            };
            clone.SetSliderPath(new SliderPath(PathType.Bezier, slideration.ToArray()));

            if (arg.ExportMode == ExportMode.Add) {
                beatmap.HitObjects.Add(clone);
            } else {
                beatmap.HitObjects.Remove(hitObjectHere);
                beatmap.HitObjects.Add(clone);
            }
            beatmap.SortHitObjects();

            editor.SaveFile();

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);

            return "Done!";
        }
    }
}
