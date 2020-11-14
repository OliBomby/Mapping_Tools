using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.BeatmapHelper.Enums;
using Mapping_Tools.Classes.BeatmapHelper.SliderPathStuff;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.SystemTools.QuickRun;
using Mapping_Tools.Classes.ToolHelpers;
using Mapping_Tools.Classes.Tools;
using Mapping_Tools.Classes.Tools.SlideratorStuff;
using Mapping_Tools.Components.Dialogs;
using Mapping_Tools.Components.Graph;
using Mapping_Tools.Components.Graph.Interpolation;
using Mapping_Tools.Components.Graph.Markers;
using Mapping_Tools.Components.ObjectVisualiser;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;
using HitObject = Mapping_Tools.Classes.BeatmapHelper.HitObject;

namespace Mapping_Tools.Views.Sliderator {
    //[HiddenTool]
    [SmartQuickRunUsage(SmartQuickRunTargets.SingleSelection)]
    public partial class SlideratorView : ISavable<SlideratorVm>, IQuickRun {
        public static readonly string ToolName = "Sliderator";

        public static readonly string ToolDescription = "Sliderator is a tool meant to make sliders with variable velocity. That means sliders that change speed during the animation. You can also make variable velocity streams with this tool." +
                                                        Environment.NewLine + Environment.NewLine +
                                                        "The UI consists of a slider import section, some options, a position/velocity graph, and a slider preview." +
                                                        Environment.NewLine + Environment.NewLine +
                                                        "To get started, simply import one or more sliders using the 'Import sliders' button. Use any of the three different import methods from the dropdown menu." +
                                                        Environment.NewLine + Environment.NewLine +
                                                        "The most important element is the position/velocity graph. This is where you tell Sliderator what you want your slider animation to look like. You can toggle between position and velocity mode by clicking the accent colored button below." +
                                                        Environment.NewLine +
                                                        "Add, remove, or edit anchors with right click and move stuff by dragging with left click. While dragging, hold Shift for horizontal clipping, hold Ctrl for vertical clipping, and hold Alt to disable snapping." +
                                                        Environment.NewLine + Environment.NewLine +
                                                        "Running Sliderator with a constant velocity will give back the original slider. You can manually choose a lower SV and bigger tumour length to optimise your slider." +
                                                        Environment.NewLine + Environment.NewLine +
                                                        "Check out all the options. The tooltips should help you further.";

        private bool _ignoreAnchorsChange;
        private bool _initialized;

        public SlideratorView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;

            DataContext = new SlideratorVm();
            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
            ViewModel.SlideratorView = this;

            Graph.VerticalMarkerGenerator = GetVerticalMarkerGenerator();
            Graph.HorizontalMarkerGenerator = GetHorizontalMarkerGenerator();

            Graph.MarkerSnappingHorizontal = true;
            Graph.MarkerSnappingVertical = true;
            Graph.MarkerSnappingRangeVertical = 0.01;

            Graph.SetBrush(new SolidColorBrush(Color.FromArgb(255, 0, 255, 255)));

            Graph.MoveAnchorTo(Graph.Anchors[0], Vector2.Zero);
            Graph.MoveAnchorTo(Graph.Anchors[Graph.Anchors.Count - 1], Vector2.One);

            Graph.Anchors.CollectionChanged += AnchorsOnCollectionChanged;
            Graph.Anchors.AnchorsChanged += AnchorsOnAnchorsChanged;

            UpdateGraphModeStuff();
            UpdatePointsOfInterest();
        }

        private void SlideratorView_OnLoaded(object sender, RoutedEventArgs e) {
            if (_initialized) return;

            ProjectManager.LoadProject(this, message: false);
            _initialized = true;
        }

        private SlideratorVm ViewModel => (SlideratorVm) DataContext;

        private void AnchorsOnAnchorsChanged(object sender, DependencyPropertyChangedEventArgs e) {
            if (_ignoreAnchorsChange) return;

            var anchor = (Anchor) sender;

            // Correct the anchor change if it resulted in a speed limit violation
            if (PrevOverSpeedLimit(anchor) || NextOverSpeedLimit(anchor)) {
                _ignoreAnchorsChange = true;
                Graph.IgnoreAnchorUpdates = true;

                // Use binary search to find the closest value to the limit
                const double d = 0.001;

                switch (e.NewValue) {
                    case double newDouble:
                        var oldDouble = (double) e.OldValue;

                        // Test if the old value is also a illegal speed violation
                        anchor.SetValue(e.Property, oldDouble);
                        if (PrevOverSpeedLimit(anchor) || NextOverSpeedLimit(anchor)) {
                            anchor.SetValue(e.Property, newDouble);
                            break;
                        }

                        anchor.SetValue(e.Property, BinarySearchUtil.DoubleBinarySearch(
                            oldDouble, newDouble, d,
                            mid => {
                                anchor.SetValue(e.Property, mid);
                                return !PrevOverSpeedLimit(anchor) && !NextOverSpeedLimit(anchor);
                            }));
                        break;
                    case Vector2 newVector2:
                        if (ViewModel.GraphModeSetting == SlideratorVm.GraphMode.Position && anchor.PreviousAnchor != null) {
                            // List of bounds. X represents the minimum Y value and Y represents the maximum Y value
                            // I use Vector2 here because it has useful math methods
                            var bounds = new List<Vector2>();

                            if (anchor.PreviousAnchor != null) {
                                var maxSpeed = InterpolatorHelper.GetBiggestDerivative(anchor.Interpolator);

                                if (Math.Abs(newVector2.X - anchor.PreviousAnchor.Pos.X) < Precision.DOUBLE_EPSILON)
                                    bounds.Add(new Vector2(anchor.PreviousAnchor.Pos.Y));
                                else
                                    bounds.Add(new Vector2(anchor.PreviousAnchor.Pos.Y) +
                                               new Vector2(Precision.DOUBLE_EPSILON).PerpendicularRight +
                                               new Vector2(ViewModel.VelocityLimit * ViewModel.SvGraphMultiplier *
                                                           (newVector2.X - anchor.PreviousAnchor.Pos.X) / maxSpeed)
                                                   .PerpendicularLeft);
                            }

                            if (anchor.NextAnchor != null) {
                                var maxSpeed = InterpolatorHelper.GetBiggestDerivative(anchor.NextAnchor.Interpolator);

                                if (Math.Abs(newVector2.X - anchor.NextAnchor.Pos.X) < Precision.DOUBLE_EPSILON)
                                    bounds.Add(new Vector2(anchor.NextAnchor.Pos.Y));
                                else
                                    bounds.Add(new Vector2(anchor.NextAnchor.Pos.Y) +
                                               new Vector2(Precision.DOUBLE_EPSILON).PerpendicularRight +
                                               new Vector2(ViewModel.VelocityLimit * ViewModel.SvGraphMultiplier *
                                                           (newVector2.X - anchor.NextAnchor.Pos.X) / maxSpeed)
                                                   .PerpendicularRight);
                            }

                            // Clamp the new Y value between all the bounds
                            var newY = bounds.Aggregate(newVector2.Y,
                                (current, bound) => MathHelper.Clamp(current, bound.X, bound.Y));

                            // Break if the resulting value is not inside all the bounds
                            if (!bounds.All(b => newY >= b.X && newY <= b.Y)) break;

                            anchor.SetValue(e.Property, new Vector2(newVector2.X, newY));
                        }

                        break;
                }

                _ignoreAnchorsChange = false;
                Graph.IgnoreAnchorUpdates = false;
            }

            if (ViewModel.PixelLength < HitObjectElement.MaxPixelLength)
                AnimateProgress(GraphHitObjectElement);
            UpdatePointsOfInterest();
            UpdateVelocity();
        }

        private bool NextOverSpeedLimit(Anchor anchor) {
            if (anchor.NextAnchor == null) return false;

            var diff = anchor.NextAnchor.Pos - anchor.Pos;

            if (ViewModel.GraphModeSetting == SlideratorVm.GraphMode.Position)
                return Math.Abs(InterpolatorHelper.GetBiggestDerivative(anchor.NextAnchor.Interpolator) * diff.Y /
                                diff.X)
                       / ViewModel.SvGraphMultiplier > ViewModel.VelocityLimit;
            return Math.Abs(InterpolatorHelper.GetBiggestValue(anchor.NextAnchor.Interpolator)) >
                   ViewModel.VelocityLimit;
        }

        private bool PrevOverSpeedLimit(Anchor anchor) {
            if (anchor.PreviousAnchor == null) return false;

            var diff = anchor.Pos - anchor.PreviousAnchor.Pos;

            if (ViewModel.GraphModeSetting == SlideratorVm.GraphMode.Position)
                return Math.Abs(InterpolatorHelper.GetBiggestDerivative(anchor.Interpolator) * diff.Y / diff.X)
                       / ViewModel.SvGraphMultiplier > ViewModel.VelocityLimit;
            return Math.Abs(InterpolatorHelper.GetBiggestValue(anchor.Interpolator)) > ViewModel.VelocityLimit;
        }

        private void AnchorsOnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
            if (ViewModel.PixelLength < HitObjectElement.MaxPixelLength)
                AnimateProgress(GraphHitObjectElement);
            UpdatePointsOfInterest();
            UpdateVelocity();
        }

        private void UpdateVelocity() {
            ViewModel.DistanceTraveled = ViewModel.GraphModeSetting == SlideratorVm.GraphMode.Position ? 
                Graph.Anchors.GetDistanceTraveled() * ViewModel.PixelLength : 
                Graph.Anchors.GetIntegralDistanceTraveled() * ViewModel.SvGraphMultiplier * ViewModel.PixelLength;
            if (!ViewModel.ManualVelocity) {
                ViewModel.NewVelocity = GetMaxVelocity(ViewModel, Graph.Anchors);
            }
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case nameof(ViewModel.ShowGraphAnchors):
                case nameof(ViewModel.ShowRedAnchors):
                    UpdatePointsOfInterest();
                    break;
                case nameof(ViewModel.VisibleHitObject):
                    if (ViewModel.PixelLength < HitObjectElement.MaxPixelLength)
                        AnimateProgress(GraphHitObjectElement);
                    UpdatePointsOfInterest();
                    break;
                case nameof(ViewModel.SvGraphMultiplier):
                case nameof(ViewModel.GraphDuration):
                    if (ViewModel.PixelLength < HitObjectElement.MaxPixelLength)
                        AnimateProgress(GraphHitObjectElement);
                    UpdatePointsOfInterest();
                    break;
                case nameof(ViewModel.BeatSnapDivisor):
                    Graph.HorizontalMarkerGenerator = GetHorizontalMarkerGenerator();
                    break;
                case nameof(ViewModel.VelocityLimit):
                    if (ViewModel.GraphModeSetting == SlideratorVm.GraphMode.Velocity) {
                        Graph.MinY = -ViewModel.VelocityLimit;
                        Graph.MaxY = ViewModel.VelocityLimit;
                    }

                    break;
                case nameof(ViewModel.GraphModeSetting):
                    UpdateGraphModeStuff();
                    UpdatePointsOfInterest();
                    break;
            }
        }

        private void UpdateEverything() {
            ViewModel.SlideratorView = this;
            UpdateGraphModeStuff();
            if (ViewModel.PixelLength < HitObjectElement.MaxPixelLength)
                AnimateProgress(GraphHitObjectElement);
            UpdatePointsOfInterest();
            UpdateVelocity();
            Graph.HorizontalMarkerGenerator = GetHorizontalMarkerGenerator();
            Graph.Anchors.CollectionChanged += AnchorsOnCollectionChanged;
            Graph.Anchors.AnchorsChanged += AnchorsOnAnchorsChanged;
        }

        private void UpdatePointsOfInterest() {
            if ((ViewModel.ShowRedAnchors || ViewModel.ShowGraphAnchors) && ViewModel.VisibleHitObject != null && ViewModel.VisibleHitObject.IsSlider) {
                var sliderPath = ViewModel.VisibleHitObject.GetSliderPath();
                var maxCompletion = GetMaxCompletion();
                var hitObjectMarkers = new ObservableCollection<HitObjectElementMarker>();

                if (ViewModel.ShowRedAnchors) {
                    var redAnchorCompletions = SliderPathUtil.GetRedAnchorCompletions(sliderPath).ToArray();

                    // Add red anchors to hit object preview
                    foreach (var completion in redAnchorCompletions) {
                        hitObjectMarkers.Add(new HitObjectElementMarker(completion / maxCompletion, 0.2, Brushes.Red));
                    }

                    // Add red anchors to graph
                    if (ViewModel.GraphModeSetting == SlideratorVm.GraphMode.Position) {
                        var markers = new ObservableCollection<GraphMarker>();

                        foreach (var completion in redAnchorCompletions) {
                            markers.Add(new GraphMarker {Orientation = Orientation.Horizontal, Value = completion,
                                CustomLineBrush = Brushes.Red, Text = null, Snappable = true
                            });
                        }

                        Graph.ExtraMarkers = markers;
                    } else {
                        Graph.ExtraMarkers.Clear();
                    }
                }
                if (ViewModel.ShowGraphAnchors) {
                    // Add graph anchors to hit objects preview
                    var graphAnchorCompletions = ViewModel.GraphModeSetting == SlideratorVm.GraphMode.Velocity
                        ? Graph.Anchors.Select(a => Graph.Anchors.GetIntegral(0, a.Pos.X) * ViewModel.SvGraphMultiplier)
                        : Graph.Anchors.Select(a => a.Pos.Y);

                    foreach (var completion in graphAnchorCompletions) {
                        hitObjectMarkers.Add(new HitObjectElementMarker(completion / maxCompletion, 0.2, Brushes.DodgerBlue));
                    }
                }
                
                if (ViewModel.PixelLength < HitObjectElement.MaxPixelLength)
                    GraphHitObjectElement.ExtraMarkers = hitObjectMarkers;

            } else {
                GraphHitObjectElement.ExtraMarkers.Clear();
                Graph.ExtraMarkers.Clear();
            }
        }

        private IMarkerGenerator GetVerticalMarkerGenerator() {
            return ViewModel.GraphModeSetting == SlideratorVm.GraphMode.Velocity
                ? new DoubleMarkerGenerator(0, 1 / 4d, "x")
                : new DoubleMarkerGenerator(0, 1 / 4d);
        }

        private IMarkerGenerator GetHorizontalMarkerGenerator() {
            return new DividedBeatMarkerGenerator(ViewModel.BeatSnapDivisor, true);
        }

        private void AnimateProgress(HitObjectElement element) {
            if (ViewModel.VisibleHitObject == null) return;

            // Set the pixel length to the pixel length of the graph
            var maxCompletion = GetMaxCompletion();
            element.CustomPixelLength = maxCompletion * ViewModel.PixelLength;

            var graphDuration = ViewModel.GraphDuration;
            var extraDuration = graphDuration.Add(TimeSpan.FromSeconds(1));

            DoubleAnimationBase animation;
            if (ViewModel.GraphModeSetting == SlideratorVm.GraphMode.Velocity)
                animation = new GraphIntegralDoubleAnimation {
                    GraphState = Graph.GetGraphState(), From = Graph.MinX, To = Graph.MaxX,
                    Duration = graphDuration,
                    BeginTime = TimeSpan.Zero,
                    // Here we use SvGraphMultiplier to get an accurate conversion from SV to slider completion per beat
                    // Completion = (100 * SliderMultiplier / PixelLength) * SV * Beats
                    Multiplier = ViewModel.SvGraphMultiplier / maxCompletion
                };
            else
                animation = new GraphDoubleAnimation {
                    GraphState = Graph.GetGraphState(), From = Graph.MinX, To = Graph.MaxX,
                    Duration = graphDuration,
                    BeginTime = TimeSpan.Zero,
                    Multiplier = 1 / maxCompletion
                };
            var animation2 = new DoubleAnimation(-1, -1, TimeSpan.FromSeconds(1)) {BeginTime = graphDuration};

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

        private double GetMaxCompletion() {
            return GetMaxCompletion(ViewModel, Graph.Anchors);
        }

        private static double GetMaxCompletion(SlideratorVm viewModel, IReadOnlyList<IGraphAnchor> anchors) {
            double maxValue;
            if (viewModel.GraphModeSetting == SlideratorVm.GraphMode.Velocity) // Integrate the graph to get the end value
                // Here we use SvGraphMultiplier to get an accurate conversion from SV to slider completion per beat
                // Completion = (100 * SliderMultiplier / PixelLength) * SV * Beats
                maxValue = AnchorCollection.GetMaxIntegral(anchors) * viewModel.SvGraphMultiplier;
            else
                maxValue = AnchorCollection.GetMaxValue(anchors);

            return maxValue;
        }

        private double GetMinCompletion() {
            return GetMinCompletion(ViewModel, Graph.Anchors);
        }

        private static double GetMinCompletion(SlideratorVm viewModel, IReadOnlyList<Anchor> anchors) {
            double minValue;
            if (viewModel.GraphModeSetting == SlideratorVm.GraphMode.Velocity) // Integrate the graph to get the end value
                // Here we use SvGraphMultiplier to get an accurate conversion from SV to slider completion per beat
                // Completion = (100 * SliderMultiplier / PixelLength) * SV * Beats
                minValue = AnchorCollection.GetMinIntegral(anchors) * viewModel.SvGraphMultiplier;
            else
                minValue = AnchorCollection.GetMinValue(anchors);

            return minValue;
        }

        // Gets max velocity in SV
        private static double GetMaxVelocity(SlideratorVm viewModel, IReadOnlyList<IGraphAnchor> anchors) {
            double maxValue;
            if (viewModel.GraphModeSetting == SlideratorVm.GraphMode.Velocity) // Integrate the graph to get the end value
                // Here we use SvGraphMultiplier to get an accurate conversion from SV to slider completion per beat
                // Completion = (100 * SliderMultiplier / PixelLength) * SV * Beats
                maxValue = Math.Max(AnchorCollection.GetMaxValue(anchors), -AnchorCollection.GetMinValue(anchors));
            else
                maxValue = Math.Max(AnchorCollection.GetMaxDerivative(anchors), -AnchorCollection.GetMinDerivative(anchors)) / viewModel.SvGraphMultiplier;

            return maxValue;
        }

        private async void ScaleCompleteButton_OnClick(object sender, RoutedEventArgs e) {
            var dialog = new TypeValueDialog(1);

            var result = await DialogHost.Show(dialog, "RootDialog");

            if (!(bool) result) return;
            if (!TypeConverters.TryParseDouble(dialog.ValueBox.Text, out var value)) return;

            var maxValue = GetMaxCompletion();
            if (Math.Abs(maxValue) < Precision.DOUBLE_EPSILON) return;
            Graph.ScaleAnchors(new Size(1, value / maxValue));
        }

        private void ClearButton_OnClick(object sender, RoutedEventArgs e) {
            var messageBoxResult = MessageBox.Show("Clear the graph?", "Confirm deletion", MessageBoxButton.YesNo);
            if (messageBoxResult != MessageBoxResult.Yes) return;

            Graph.Clear();
            if (ViewModel.GraphModeSetting == SlideratorVm.GraphMode.Velocity) {
                var sv = MathHelper.Clamp(ViewModel.PixelLength / ViewModel.GraphBeats / ViewModel.GlobalSv / 100,
                    -ViewModel.VelocityLimit, ViewModel.VelocityLimit);
                Graph.Anchors.First().Pos = new Vector2(0, sv);
                Graph.Anchors.Last().Pos = new Vector2(ViewModel.GraphBeats, sv);
            } else {
                Graph.Anchors.First().Pos = Vector2.Zero;
                Graph.Anchors.Last().Pos = new Vector2(ViewModel.GraphBeats, 1);
            }
        }

        public void UpdateGraphModeStuff() {
            switch (ViewModel.GraphModeSetting) {
                case SlideratorVm.GraphMode.Position:
                    GraphToggleContentTextBlock.Text = "X";
                    Graph.HorizontalAxisVisible = false;
                    Graph.VerticalAxisVisible = false;

                    // Make sure the start point is locked at y = 0
                    Graph.StartPointLockedY = true;
                    var firstAnchor = Graph.Anchors.FirstOrDefault();
                    if (firstAnchor != null) firstAnchor.Pos = new Vector2(firstAnchor.Pos.X, 0);

                    Graph.MinY = 0;
                    Graph.MaxY = 1;
                    Graph.VerticalMarkerGenerator = GetVerticalMarkerGenerator();
                    break;
                case SlideratorVm.GraphMode.Velocity:
                    GraphToggleContentTextBlock.Text = "V";
                    Graph.HorizontalAxisVisible = true;
                    Graph.VerticalAxisVisible = false;
                    Graph.StartPointLockedY = false;

                    Graph.MinY = -ViewModel.VelocityLimit;
                    Graph.MaxY = ViewModel.VelocityLimit;
                    Graph.VerticalMarkerGenerator = GetVerticalMarkerGenerator();
                    break;
                default:
                    GraphToggleContentTextBlock.Text = "";
                    break;
            }

            AnimateProgress(GraphHitObjectElement);
        }

        private bool ValidateToolInput(out string message) {
            if (GetMinCompletion() < -1E-4) {
                message = "Negative position is illegal.";
                return false;
            }

            var maxVelocity = ViewModel.NewVelocity;
            if (double.IsInfinity(maxVelocity)) {
                message = "Infinite slope on the path is illegal.";
                return false;
            }

            if (maxVelocity > ViewModel.VelocityLimit + Precision.DOUBLE_EPSILON) {
                message = "A velocity faster than the SV limit is illegal. Please check your graph or increase the SV limit.";
                return false;
            }

            if (double.IsInfinity(ViewModel.BeatsPerMinute) || double.IsNaN(ViewModel.BeatsPerMinute) ||
                Math.Abs(ViewModel.BeatsPerMinute) < Precision.DOUBLE_EPSILON) {
                message = "The beats per minute field has an illegal value";
                return false;
            }

            if (double.IsInfinity(ViewModel.GraphBeats) || double.IsNaN(ViewModel.GraphBeats) ||
                Math.Abs(ViewModel.GraphBeats) < Precision.DOUBLE_EPSILON) {
                message = "The beat length field has an illegal value";
                return false;
            }

            if (double.IsInfinity(ViewModel.GlobalSv) || double.IsNaN(ViewModel.GlobalSv) ||
                Math.Abs(ViewModel.GlobalSv) < Precision.DOUBLE_EPSILON) {
                message = "The global SV field has an illegal value";
                return false;
            }

            message = string.Empty;
            return true;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps()[0]);
        }

        private async void RunTool(string path, bool quick = false, bool reload = false) {
            if (!CanRun) return;

            // Remove logical focus to trigger LostFocus on any fields that didn't yet update the ViewModel
            FocusManager.SetFocusedElement(FocusManager.GetFocusScope(this), null);

            if (!ValidateToolInput(out var message)) {
                var dialog = new MessageDialog(message);
                await DialogHost.Show(dialog, "RootDialog");
                return;
            }

            BackupManager.SaveMapBackup(path);

            ViewModel.Path = path;
            ViewModel.Quick = quick;
            ViewModel.Reload = reload;
            ViewModel.GraphState = Graph.GetGraphState();
            if (ViewModel.GraphState.CanFreeze) ViewModel.GraphState.Freeze();

            BackgroundWorker.RunWorkerAsync(ViewModel);
            CanRun = false;
        }

        protected override void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e) {
            var bgw = sender as BackgroundWorker;
            e.Result = Sliderate((SlideratorVm) e.Argument, bgw);
        }

        private string Sliderate(SlideratorVm arg, BackgroundWorker worker) {
            // Make a position function for Sliderator
            PositionFunctionDelegate positionFunction;
            // Test if the function is a constant velocity
            bool constantVelocity;
            // We convert the graph GetValue function to a function that works like ms -> px
            // d is a value representing the number of milliseconds into the slider
            if (arg.GraphModeSetting == SlideratorVm.GraphMode.Velocity) {
                // Here we use SvGraphMultiplier to get an accurate conversion from SV to slider completion per beat
                // Completion = (100 * SliderMultiplier / PixelLength) * SV * Beats
                positionFunction = d =>
                    arg.GraphState.GetIntegral(0, d * arg.BeatsPerMinute / 60000) * arg.SvGraphMultiplier *
                    arg.PixelLength;

                constantVelocity = Precision.AlmostEquals(AnchorCollection.GetMaxValue(arg.GraphState.Anchors),
                    AnchorCollection.GetMinValue(arg.GraphState.Anchors));
            }
            else {
                positionFunction = d => arg.GraphState.GetValue(d * arg.BeatsPerMinute / 60000) * arg.PixelLength;

                constantVelocity = Precision.AlmostEquals(AnchorCollection.GetMaxDerivative(arg.GraphState.Anchors),
                    AnchorCollection.GetMinDerivative(arg.GraphState.Anchors));
            }

            // Dont do Sliderator if the velocity is constant AND equal to the new velocity
            var simplifyShape = constantVelocity && Precision.AlmostEquals(
                                    arg.PixelLength / arg.GraphBeats / arg.GlobalSv / 100,
                                    arg.NewVelocity);

            // Get the highest velocity occuring in the graph
            double velocity = arg.NewVelocity; // Velocity is in SV
            // Do bad stuff to the velocity to make sure its the same SV as after writing it to .osu code
            velocity = -100 / double.Parse((-100 / velocity).ToInvariant(), CultureInfo.InvariantCulture);
            // Other velocity is in px / ms
            var otherVelocity = velocity * arg.SvGraphMultiplier * arg.PixelLength * arg.BeatsPerMinute / 60000;

            // Time between timeline ticks for stream export
            var deltaT = 60000 / arg.BeatsPerMinute / arg.BeatSnapDivisor;

            // Update progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(10);

            List<Vector2> slideration = new List<Vector2>();
            var sliderator = new Classes.Tools.SlideratorStuff.Sliderator {
                PositionFunction = positionFunction, MaxT = arg.GraphBeats / arg.BeatsPerMinute * 60000,
                Velocity = otherVelocity,
                MinDendriteLength = arg.MinDendrite
            };

            if (!simplifyShape) {
                // Get slider path like from the hit object preview
                var sliderPath = new SliderPath(arg.VisibleHitObject.SliderType,
                    arg.VisibleHitObject.GetAllCurvePoints().ToArray(),
                    GetMaxCompletion(arg, arg.GraphState.Anchors) * arg.PixelLength);
                var path = new List<Vector2>();
                sliderPath.GetPathToProgress(path, 0, 1);

                // Update progressbar
                if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(20);

                // Do Sliderator
                sliderator.SetPath(path);

                slideration = arg.ExportAsStream ? 
                    sliderator.SliderateStream(deltaT) : 
                    sliderator.Sliderate();

                // Check for some illegal output
                if (double.IsInfinity(sliderator.MaxS) || double.IsNaN(sliderator.MaxS) ||
                    slideration.Any(v => double.IsNaN(v.X) || double.IsNaN(v.Y))) {
                    return "Encountered unexpected values from Sliderator. Please check your input.";
                }
            }
            
            // Update progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(60);

            // Exporting stuff
            BeatmapEditor editor;
            bool editorRead = false;
            if (arg.DoEditorRead) {
                editor = EditorReaderStuff.GetNewestVersionOrNot(arg.Path, out _, out var exception);

                if (exception == null)
                    editorRead = true;

                arg.DoEditorRead = false;
            } else {
                editor = new BeatmapEditor(arg.Path);
            }
            
            var beatmap = editor.Beatmap;
            var timing = beatmap.BeatmapTiming;

            // Get hit object that might be present at the export time or make a new one
            var hitObjectHere = beatmap.HitObjects.FirstOrDefault(o => Math.Abs(arg.ExportTime - o.Time) < 5) ??
                                new HitObject(arg.ExportTime, 0, SampleSet.Auto, SampleSet.Auto);


            // Clone the hit object to not affect the already existing hit object instance with changes
            var clone = new HitObject(hitObjectHere.GetLine()) {
                IsCircle = arg.ExportAsStream, IsSpinner = false, IsHoldNote = false, IsSlider = !arg.ExportAsStream
            };

            // Update progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(70);

            if (!arg.ExportAsStream) {
                // Give the new hit object the sliderated anchors
                if (simplifyShape) {
                    // The velocity is constant, so you can simplify to the original slider shape
                    clone.SetAllCurvePoints(arg.VisibleHitObject.GetAllCurvePoints());
                    clone.SliderType = arg.VisibleHitObject.SliderType;
                } else {
                    clone.SetAllCurvePoints(slideration);
                    clone.SliderType = PathType.Bezier;
                }

                clone.PixelLength = sliderator.MaxS;
                clone.SliderVelocity = -100 / velocity;
                
                // Add hit object
                if (arg.ExportModeSetting == SlideratorVm.ExportMode.Add) {
                    beatmap.HitObjects.Add(clone);
                } else {
                    beatmap.HitObjects.Remove(hitObjectHere);
                    beatmap.HitObjects.Add(clone);
                }

                // Add SV
                var timingPointsChanges = new List<TimingPointsChange>();

                if (arg.DelegateToBpm) {
                    var tpAfter = timing.GetRedlineAtTime(clone.Time).Copy();
                    var tpOn = tpAfter.Copy();

                    tpAfter.Offset = clone.Time;
                    tpOn.Offset = clone.Time - 1;  // This one will be on the slider

                    tpAfter.OmitFirstBarLine = true;
                    tpOn.OmitFirstBarLine = true;

                    // Express velocity in BPM
                    tpOn.MpB /= -100 / clone.SliderVelocity;
                    // NaN SV results in removal of slider ticks
                    clone.SliderVelocity = arg.RemoveSliderTicks ? double.NaN : -100;
                    
                    // Add redlines
                    timingPointsChanges.Add(new TimingPointsChange(tpOn, mpb:true, unInherited:true, omitFirstBarLine:true, fuzzyness:0));
                    timingPointsChanges.Add(new TimingPointsChange(tpAfter, mpb:true, unInherited:true, omitFirstBarLine:true, fuzzyness:0));

                    clone.Time -= 1;
                }

                // Add SV for every hit object so the SV doesnt change for anything else than the sliderated slider
                timingPointsChanges.AddRange(beatmap.HitObjects.Select(ho => {
                        var sv = ho == clone ? ho.SliderVelocity : timing.GetSvAtTime(ho.Time);
                        var tp = timing.GetTimingPointAtTime(ho.Time).Copy();
                        tp.MpB = sv;
                        tp.Offset = ho.Time;
                        return new TimingPointsChange(tp, mpb: true, fuzzyness:0);
                    }));

                TimingPointsChange.ApplyChanges(timing, timingPointsChanges);
            } else {
                // Add hit objects
                if (arg.ExportModeSetting == SlideratorVm.ExportMode.Override) {
                    beatmap.HitObjects.Remove(hitObjectHere);
                }

                double t = arg.ExportTime;
                foreach (var pos in slideration) {
                    clone.Pos = pos;
                    clone.Time = t;
                    beatmap.HitObjects.Add(clone);

                    clone = new HitObject(clone.GetLine()) {
                        IsCircle = true, IsSpinner = false, IsHoldNote = false, IsSlider = false, NewCombo = false
                    };
                    t += deltaT;
                }
            }

            // Update progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(80);

            beatmap.SortHitObjects();

            editor.SaveFile();

            // Complete progressbar
            if (worker != null && worker.WorkerReportsProgress) worker.ReportProgress(100);
            
            // Do stuff
            RunFinished?.Invoke(this, new RunToolCompletedEventArgs(true,  arg.Reload && editorRead, arg.Quick));

            return arg.Quick ? string.Empty : "Done!";
        }

        public SlideratorVm GetSaveData() {
            ViewModel.GraphState = Graph.GetGraphState();
            if (ViewModel.GraphState.CanFreeze) ViewModel.GraphState.Freeze();

            return ViewModel;
        }

        public void SetSaveData(SlideratorVm saveData) {
            DataContext = saveData;
            if (saveData.GraphState != null) {
                Graph.SetGraphState(saveData.GraphState);
            } else {
                Graph.Clear();
            }
            UpdateEverything();
            ViewModel.PropertyChanged += ViewModelOnPropertyChanged;
        }
        
        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "slideratorproject.json");

        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Sliderator Projects");

        public void RunFast() {
            var currentMap = MainWindow.AppWindow.GetCurrentMaps()[0];
            RunTool(currentMap, true);
        }

        public void QuickRun() {
            var currentMap = IOHelper.GetCurrentBeatmapOrCurrentBeatmap();

            ViewModel.Import(currentMap);
            RunTool(currentMap, true, true);
        }

        public event EventHandler RunFinished;
    }
}