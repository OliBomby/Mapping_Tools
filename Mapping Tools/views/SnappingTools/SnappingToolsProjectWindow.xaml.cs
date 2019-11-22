using System.Windows;
using Mapping_Tools.Classes.SnappingTools.Serialization;

namespace Mapping_Tools.Views.SnappingTools {
    /// <summary>
    /// Interaction logic for SnappingToolsProjectWindow.xaml
    /// </summary>
    public partial class SnappingToolsProjectWindow {
        public SnappingToolsProject Project {
            get => (SnappingToolsProject)DataContext;
            set => DataContext = value;
        }

        public SnappingToolsProjectWindow(SnappingToolsProject project) {
            Project = project;
            InitializeComponent();
        }

        private SnappingToolsSaveSlot GetSelectedSaveSlot() {
            return (SnappingToolsSaveSlot)SaveSlotsGrid.SelectedItem;
        }

        private void AddButton_Click(object sender, RoutedEventArgs e) {
            var newSave = new SnappingToolsSaveSlot {Name = $"Save {Project.SaveSlots.Count + 1}"};
            Project.SaveToSlot(newSave, false);
            Project.SaveSlots.Add(newSave);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e) {
            Project.SaveSlots.Remove(GetSelectedSaveSlot());
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
