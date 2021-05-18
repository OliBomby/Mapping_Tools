using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.MathUtil;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.SnappingTools.DataStructure.RelevantObjectCollection;
using Mapping_Tools.Classes.Tools.SnappingTools.Serialization;
using Mapping_Tools.Viewmodels;
using MaterialDesignThemes.Wpf;

namespace Mapping_Tools.Views.SnappingTools
{
    public partial class SnappingToolsView : ISavable<SnappingToolsProject>, IHaveExtraProjectMenuItems
    {

        public static readonly string ToolName = "Geometry Dashboard";

        public static readonly string ToolDescription = $@"Generates and keeps track of a list virtual objects that are geometrically relevant to the objects visible on your screen. Press and hold the Activation Key to let your cursor snap to the closest virtual object.{Environment.NewLine}⚠ You must specify your user config file in the Preferences for this tool to function.";

        private double _scrollOffset;
        private bool _resetScroll = true;

        public SnappingToolsVm ViewModel
        {
            get => (SnappingToolsVm)DataContext;
            set => DataContext = value;
        }
        public SnappingToolsProjectWindow ProjectWindow;

        public SnappingToolsView()
        {
            DataContext = new SnappingToolsVm();
            InitializeComponent();
            Width = MainWindow.AppWindow.content_views.Width;
            Height = MainWindow.AppWindow.content_views.Height;
            ProjectManager.LoadProject(this, message: false);
        }

        private void PreferencesButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            var preferencesWindow = new SnappingToolsPreferencesWindow(ViewModel.Project.GetCurrentPreferences());
            var result = preferencesWindow.ShowDialog();
            if (result.GetValueOrDefault())
            {
                ViewModel.Project.SetCurrentPreferences(preferencesWindow.Preferences);
            }
        }

        private void ProjectsButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (ProjectWindow == null)
            {
                ProjectWindow = new SnappingToolsProjectWindow(ViewModel.GetProject());
                ProjectWindow.Closed += ProjectWindowOnClosed;
                ProjectWindow.Show();
            }
            else
            {
                ProjectWindow.Activate();
            }
        }

        private void ProjectWindowOnClosed(object sender, EventArgs e)
        {
            ProjectWindow = null;
        }

        public SnappingToolsProject GetSaveData() => ViewModel.GetProject();

        public void SetSaveData(SnappingToolsProject saveData)
        {
            ViewModel.SetProject(saveData);
        }

        public string AutoSavePath => Path.Combine(MainWindow.AppDataPath, "geometrydashboardproject.json");
        public string DefaultSaveFolder => Path.Combine(MainWindow.AppDataPath, "Geometry Dashboard Projects");

        private void UIElement_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scv = (ScrollViewer)sender;
            _scrollOffset = scv.VerticalOffset - e.Delta;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        public override void Activate()
        {
            ViewModel.Activate();
            base.Activate();
        }

        public override void Deactivate()
        {
            ViewModel.Deactivate();
            base.Deactivate();
        }

        public override void Dispose()
        {
            ViewModel.Dispose();
            base.Dispose();
        }

        private void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var scv = (ScrollViewer)sender;
            if (_resetScroll && Math.Abs(_scrollOffset - scv.VerticalOffset) > Precision.DOUBLE_EPSILON)
            {
                scv.ScrollToVerticalOffset(_scrollOffset);
            }
        }

        private void UIElement_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            _resetScroll = !Equals(e.Source, GeneratorsScrollViewer);
        }

        #region IHaveExtraMenuItems members

        public MenuItem[] GetMenuItems() {
            var saveObjects = new MenuItem {
                Header = "_Save virtual objects", Icon = new PackIcon { Kind = PackIconKind.ContentSaveOutline },
                ToolTip = "Save locked virtual objects to a file."
            };
            saveObjects.Click += SaveLockedRelevantObjectsFromFile;

            var loadObjects = new MenuItem {
                Header = "_Load virtual objects", Icon = new PackIcon { Kind = PackIconKind.FolderOpenOutline },
                ToolTip = "Load locked virtual objects from a save file."
            };
            loadObjects.Click += LoadLockedRelevantObjectsFromFile;

            return new[] { saveObjects, loadObjects };
        }

        private void SaveLockedRelevantObjectsFromFile(object sender, RoutedEventArgs e) {
            try {
                ProjectManager.SaveToolFile(this, ViewModel.GetLockedObjects(), true);

                Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("Successfully saved locked virtual objects!"));
            } catch (ArgumentException) { } catch (Exception ex) {
                ex.Show();
            }
        }

        private void LoadLockedRelevantObjectsFromFile(object sender, RoutedEventArgs e) {
            try {
                var objects = ProjectManager.LoadToolFile<SnappingToolsProject, RelevantObjectCollection>(this, true);
                ViewModel.SetLockedObjects(objects);

                Task.Factory.StartNew(() => MainWindow.MessageQueue.Enqueue("Successfully loaded locked virtual objects!"));
            } catch (ArgumentException) { } catch (Exception ex) {
                ex.Show();
            }
        }

        #endregion
    }
}
