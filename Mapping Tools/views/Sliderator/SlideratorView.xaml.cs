using System;
using Mapping_Tools.Classes.SystemTools;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Components.Graph;
using MaterialDesignColors.ColorManipulation;

namespace Mapping_Tools.Views {
    //[HiddenTool]
    public partial class SlideratorView {
        public static readonly string ToolName = "Sliderator";

        public static readonly string ToolDescription = "";

        public Graph Graph;
        private DispatcherTimer timer;
        private double hue;

        public SlideratorView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;

            Graph = new Graph {
                Width = 200, Height = 200
            };

            Graph.SetBrush(new SolidColorBrush(Color.FromArgb(255, 0, 255, 255)));

            Graph.MoveAnchorTo(Graph.Anchors[0], Vector2.Zero);
            Graph.MoveAnchorTo(Graph.Anchors[Graph.Anchors.Count - 1], Vector2.One);

            GraphHost.Content = Graph;

            timer = new DispatcherTimer {Interval = TimeSpan.FromMilliseconds(16)};
            timer.Tick += TimerOnTick;
            timer.Start();
        }

        private void TimerOnTick(object sender, EventArgs e) {
            Graph.SetBrush(new SolidColorBrush(new Hsb(hue, 1, 1).ToColor()));
            hue = (hue + 1) % 360;
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
    }
}
