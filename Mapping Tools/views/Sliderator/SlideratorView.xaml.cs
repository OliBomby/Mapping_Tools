using System;
using System.Collections.Generic;
using System.ComponentModel;
using Mapping_Tools.Classes.SystemTools;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Mapping_Tools.Classes.BeatmapHelper;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph;
using MaterialDesignColors.ColorManipulation;
using Mapping_Tools.Classes.Tools;
using System.Linq;
using System.Windows.Media.Animation;
using Mapping_Tools.Components.ObjectVisualiser;
using Mapping_Tools.Viewmodels;
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

            var markers = new List<GraphMarker>();
            for (int i = 0; i <= 10; i++) {
                markers.Add(new GraphMarker {Orientation = Orientation.Horizontal, Text = $"{i}x", Value = i});
            }
            for (int i = 0; i <= 12; i++) {
                markers.Add(new GraphMarker {Orientation = Orientation.Vertical, Value = i / 4d, DrawMarker = true,
                    MarkerColor = i % 4 == 0 ? Colors.White : i % 2 == 0 ? Colors.Red : Colors.DodgerBlue,
                    MarkerLength = i % 4 == 0 ? 12 : 7, Text = i % 4 == 0 ? (i / 4).ToString() : null
                });
            }

            Graph.SetMarkers(markers);

            Graph.SetBrush(new SolidColorBrush(Color.FromArgb(255, 0, 255, 255)));

            Graph.MoveAnchorTo(Graph.State.Anchors[0], Vector2.Zero);
            Graph.MoveAnchorTo(Graph.State.Anchors[Graph.State.Anchors.Count - 1], Vector2.One);
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case "GraphDuration":
                    AnimateProgress(GraphHitObjectElement);
                    break;
            }
        }

        private void AnimateProgress(HitObjectElement element) {
            var graphDuration = ViewModel.GraphDuration;
            var doubleDuration = graphDuration.Add(graphDuration);

            var animation = new GraphDoubleAnimation {
                GraphState = Graph.State, From = 0, To = 1,
                Duration = graphDuration,
                BeginTime = TimeSpan.Zero
            };
            var animation2 = new DoubleAnimation(0, 0, graphDuration) {BeginTime = graphDuration};

            Storyboard.SetTarget(animation, element);
            Storyboard.SetTarget(animation2, element);
            Storyboard.SetTargetProperty(animation, new PropertyPath(HitObjectElement.ProgressProperty));
            Storyboard.SetTargetProperty(animation2, new PropertyPath(HitObjectElement.ProgressProperty));

            var timeline = new ParallelTimeline {RepeatBehavior = RepeatBehavior.Forever, Duration = doubleDuration};
            timeline.Children.Add(animation);
            timeline.Children.Add(animation2);

            var storyboard = new Storyboard();
            storyboard.Children.Add(timeline);

            element.BeginStoryboard(storyboard);
        }

        private void Start_Click(object sender, RoutedEventArgs e) {
            RunTool(MainWindow.AppWindow.GetCurrentMaps(), quick: false);
        }

        private void RunTool(string[] paths, bool quick = false) {
            if (!CanRun) return;

            IOHelper.SaveMapBackup(paths);

            //BackgroundWorker.RunWorkerAsync(arguments);
            CanRun = false;
        }

        private void Import_Slider(object sender, RoutedEventArgs e)
        {
            bool editorRead = EditorReaderStuff.TryGetFullEditorReader(out var reader);
            foreach (string path in MainWindow.AppWindow.GetCurrentMaps())
            {
                var selected = new List<HitObject>();
                BeatmapEditor editor = editorRead ? EditorReaderStuff.GetNewestVersion(path, out selected, reader) : new BeatmapEditor(path);
                Beatmap beatmap = editor.Beatmap;
                Timing timing = beatmap.BeatmapTiming;
                List<HitObject> markedObjects = selected;

                try {
                    GraphHitObjectElement.HitObject = markedObjects.First(s => s.IsSlider);
                }
                catch (InvalidOperationException) {}
            }
        }

        private void SlideratorView_OnLoaded(object sender, RoutedEventArgs e) {
            GraphHitObjectElement.HitObject = new HitObject("159,226,0,2,0,B|299:155|275:42|143:56|139:176|263:232|263:232|315:193|319:105,1,489.9999833107");
            //GraphHitObjectElement.HitObject = new HitObject("74,270,665,1,0,0:0:0:0:");
            AnimateProgress(GraphHitObjectElement);
        }
    }
}
