using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Graph;
using Mapping_Tools.Components.Graph.Markers;
using Mapping_Tools.Components.ObjectVisualiser;
using Mapping_Tools.Viewmodels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Mapping_Tools.Components.Graph.Interpolation;
using Mapping_Tools.Components.Graph.Interpolation.Interpolators;

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
                case "VisibleHitObject":
                case "GraphDuration":
                    AnimateProgress(GraphHitObjectElement);
                    break;
                case "BeatSnapDivisor":
                    Graph.HorizontalMarkerGenerator = new DividedBeatMarkerGenerator(ViewModel.BeatSnapDivisor);
                    break;
            }
        }

        private void AnimateProgress(HitObjectElement element) {
            if (ViewModel.VisibleHitObject == null) return;

            var graphDuration = ViewModel.GraphDuration;
            var extraDuration = graphDuration.Add(TimeSpan.FromSeconds(1));

            var animation = new GraphDoubleAnimation {
                GraphState = Graph.GetGraphState(), From = 0, To = 1,
                Duration = graphDuration,
                BeginTime = TimeSpan.Zero
            };
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
                    break;
                case GraphMode.Velocity:
                    GraphToggleContentTextBlock.Text = "V";
                    break;
                default:
                    GraphToggleContentTextBlock.Text = "";
                    break;
            }

            if (_graphMode == GraphMode.Position && graphMode == GraphMode.Velocity) {
                // Differentiate graph
                var newAnchors = new List<Anchor>();
                var newHeight = ViewModel.VelocityLimit;
                Anchor previousAnchor = null;
                foreach (var anchor in Graph.Anchors) {
                    if (previousAnchor != null) {
                        var p1 = Graph.GetValue(previousAnchor.Pos);
                        var p2 = Graph.GetValue(anchor.Pos);

                        var difference = p2 - p1;

                        if (anchor.Interpolator is IDerivableInterpolator derivableInterpolator) {
                            var startSlope = derivableInterpolator.GetDerivative(0) * difference.Y / difference.X;
                            var endSlope = derivableInterpolator.GetDerivative(1) * difference.Y / difference.X;

                            var a1 = new Anchor(Graph, new Vector2(previousAnchor.Pos.X, startSlope / newHeight / 2 + 0.5)) {
                                Interpolator = new LinearInterpolator()
                            };
                            var t1 = new TensionAnchor(Graph, Vector2.Zero, a1);
                            a1.TensionAnchor = t1;
                            newAnchors.Add(a1);
                            var a2 = new Anchor(Graph, new Vector2(anchor.Pos.X, endSlope / newHeight / 2 + 0.5)) {
                                Interpolator = derivableInterpolator.GetDerivativeInterpolator()
                            };
                            var t2 = new TensionAnchor(Graph, Vector2.Zero, a2);
                            a2.TensionAnchor = t2;
                            newAnchors.Add(a2);
                        } else {
                            var slope = difference.Y / difference.X;
                            
                            var a1 = new Anchor(Graph, new Vector2(previousAnchor.Pos.X, slope / newHeight / 2 + 0.5)) {
                                Interpolator = new LinearInterpolator()
                            };
                            var t1 = new TensionAnchor(Graph, Vector2.Zero, a1);
                            a1.TensionAnchor = t1;
                            newAnchors.Add(a1);
                            var a2 = new Anchor(Graph, new Vector2(anchor.Pos.X, slope / newHeight / 2 + 0.5)) {
                                Interpolator = new LinearInterpolator()
                            };
                            var t2 = new TensionAnchor(Graph, Vector2.Zero, a2);
                            a2.TensionAnchor = t2;
                            newAnchors.Add(a2);
                        }
                    }

                    previousAnchor = anchor;
                }

                Graph.Anchors = new ObservableCollection<Anchor>(newAnchors);
                Graph.MinY = -newHeight;
                Graph.MaxY = newHeight;
                Graph.VerticalMarkerGenerator = new DoubleMarkerGenerator(0, 1, "x");
            } else if (_graphMode == GraphMode.Velocity && graphMode == GraphMode.Position) {
                // Integrate graph
                var newAnchors = new List<Anchor> {new Anchor(Graph, new Vector2(0, 0))};
                double height = 0;
                Anchor previousAnchor = null;
                foreach (var anchor in Graph.Anchors) {
                    if (previousAnchor != null) {
                        var p1 = Graph.GetValue(previousAnchor.Pos);
                        var p2 = Graph.GetValue(anchor.Pos);

                        var difference = p2 - p1;

                        if (difference.X < Precision.DOUBLE_EPSILON) {
                            previousAnchor = anchor;
                            continue;
                        }

                        if (anchor.Interpolator is IIntegrableInterpolator integrableInterpolator) {
                            height += integrableInterpolator.GetIntegral(0, 1) * difference.X * difference.Y + difference.X * p1.Y;

                            var a = new Anchor(Graph, new Vector2(anchor.Pos.X, height)) {
                                Interpolator = integrableInterpolator.GetPrimitiveInterpolator()
                            };
                            var t = new TensionAnchor(Graph, Vector2.Zero, a);
                            a.TensionAnchor = t;
                            newAnchors.Add(a);
                        } else {
                            height += 0.5 * difference.X * difference.Y;

                            var a = new Anchor(Graph, new Vector2(anchor.Pos.X, height)) {
                                Interpolator = new LinearInterpolator()
                            };
                            var t = new TensionAnchor(Graph, Vector2.Zero, a);
                            a.TensionAnchor = t;
                            newAnchors.Add(a);
                        }
                    }

                    previousAnchor = anchor;
                }

                Graph.Anchors = new ObservableCollection<Anchor>(newAnchors);
                Graph.MinY = 0;
                Graph.MaxY = 1;
                Graph.VerticalMarkerGenerator = new DoubleMarkerGenerator(0, 0.25);
            }

            _graphMode = graphMode;
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps());
        }

        private void RunTool(string[] paths, bool quick = false) {
            if (!CanRun) return;

            IOHelper.SaveMapBackup(paths);

            //BackgroundWorker.RunWorkerAsync(arguments);
            CanRun = false;
        }
    }
}
