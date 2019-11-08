using Mapping_Tools.Classes.SystemTools;
using System.Windows;
using Mapping_Tools.Components.Graph;

namespace Mapping_Tools.Views {
    [HiddenTool]
    public partial class SlideratorView {
        public static readonly string ToolName = "Sliderator";

        public static readonly string ToolDescription = "";

        public Graph Graph;

        public SlideratorView() {
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;

            Graph = new Graph {Width = 200, Height = 200};

            var anchor1 = new Anchor(Graph);
            anchor1.SetPosition(new Point(Graph.XMin, Graph.YMin));
            Graph.AddAnchor(anchor1);

            var anchor2 = new Anchor(Graph);
            anchor2.SetPosition(new Point(Graph.XMax, Graph.YMax));
            Graph.AddAnchor(anchor2);

            GraphHost.Content = Graph;
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
