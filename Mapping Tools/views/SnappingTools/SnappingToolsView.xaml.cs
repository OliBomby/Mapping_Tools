using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SnappingTools.Serialization;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Viewmodels;
using Mapping_Tools.Views.SnappingTools;
using System;
using System.IO;
using System.Windows.Controls;
using System.Windows.Input;

namespace Mapping_Tools.Views {
    public partial class SnappingToolsView : ISavable<SnappingToolsProject> {

        public static readonly string ToolName = "Geometry Dashboard";

        public static readonly string ToolDescription = $@"Generates and keeps track of a list virtual objects that are geometrically relevant to the objects visible on your screen. Press and hold the Activation Key to let your cursor snap to the closest virtual object.{Environment.NewLine}⚠ You must specify your user config file in the Preferences for this tool to function.";

        private double _scrollOffset;

        public SnappingToolsVm ViewModel {
            get => (SnappingToolsVm) DataContext;
            set => DataContext = value;
        }

        public SnappingToolsView() {
            DataContext = new SnappingToolsVm();
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        private void PreferencesButton_Click(object sender, System.Windows.RoutedEventArgs e) {
            var preferencesWindow = new SnappingToolsPreferencesWindow(ViewModel.Project.GetCurrentPreferences());
            var result = preferencesWindow.ShowDialog();
            if (result.GetValueOrDefault()) {
                ViewModel.Project.SetCurrentPreferences(preferencesWindow.Preferences);
            } 
        }

        public SnappingToolsProject GetSaveData() => ViewModel.GetProject();

        public void SetSaveData(SnappingToolsProject saveData) {
            ViewModel.SetProject(saveData);
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "geometrydashboardproject.json");
        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Geometry Dashboard Projects");

        private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e) {
            var scv = (ScrollViewer)sender;
            _scrollOffset = scv.VerticalOffset - e.Delta;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        public override void Activate() {
            ViewModel.Activate();
            base.Activate();
        }

        public override void Deactivate() {
            ViewModel.Deactivate();
            base.Deactivate();
        }

        public override void Dispose() {
            ViewModel.Dispose();
            base.Dispose();
        }

        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e) {
            var scv = (ScrollViewer) sender;
            if (Math.Abs(_scrollOffset - scv.VerticalOffset) > Precision.DOUBLE_EPSILON) {
                scv.ScrollToVerticalOffset(_scrollOffset);
            }
        }
    }
}
