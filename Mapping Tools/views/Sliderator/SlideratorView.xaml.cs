using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Graph;
using Mapping_Tools.Components.Graph.Markers;
using Mapping_Tools.Components.ObjectVisualiser;
using Mapping_Tools.Viewmodels;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using HitObject = Mapping_Tools.Classes.BeatmapHelper.HitObject;

namespace Mapping_Tools.Views {
    //[HiddenTool]
    public partial class SlideratorView {
        public static readonly string ToolName = "Sliderator";

        public static readonly string ToolDescription = "";

        private SlideratorVm ViewModel => (SlideratorVm) DataContext;

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
            var messageBoxResult = MessageBox.Show("Clear the graph?", "Confirm deletion", MessageBoxButton.YesNoCancel);
            if (messageBoxResult != MessageBoxResult.Yes) return;

            Graph.Clear();
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
