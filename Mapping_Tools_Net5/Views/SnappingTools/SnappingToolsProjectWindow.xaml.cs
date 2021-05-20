using System;
using System.Windows;
using Mapping_Tools.Classes;
using Mapping_Tools.Classes.Tools.SnappingTools.Serialization;
using Mapping_Tools.Components.Domain;

namespace Mapping_Tools.Views.SnappingTools {
    /// <summary>
    /// Interaction logic for SnappingToolsProjectWindow.xaml
    /// </summary>
    public partial class SnappingToolsProjectWindow {
        public SnappingToolsProject Project { get; set; }

        public CommandImplementation AddCommand { get; }
        public CommandImplementation RemoveCommand { get; }
        public CommandImplementation DuplicateCommand { get; }

        public SnappingToolsProjectWindow(SnappingToolsProject project) {
            InitializeComponent();
            Project = project;
            DataContext = this;

            AddCommand = new CommandImplementation(_ => {
                var newSave = new SnappingToolsSaveSlot {Name = $"Save {Project.SaveSlots.Count + 1}"};
                Project.SaveToSlot(newSave, false);
                newSave.Activate();
                Project.SaveSlots.Add(newSave);
            });
            RemoveCommand = new CommandImplementation(_ => {
                if (SaveSlotsGrid.SelectedItems.Count == 0 && Project.SaveSlots.Count > 0) {
                    Project.SaveSlots.RemoveAt(Project.SaveSlots.Count - 1);
                    return;
                }

                Project.SaveSlots.RemoveAll(o => SaveSlotsGrid.SelectedItems.Contains(o));
            });
            DuplicateCommand = new CommandImplementation(_ => {
                var itemsToDupe = new SnappingToolsSaveSlot[SaveSlotsGrid.SelectedItems.Count];
                var i = 0;
                foreach (var listSelectedItem in SaveSlotsGrid.SelectedItems) {
                    itemsToDupe[i++] = (SnappingToolsSaveSlot) listSelectedItem;
                }
                foreach (var listSelectedItem in itemsToDupe) {
                    var clone = (SnappingToolsSaveSlot) listSelectedItem.Clone();
                    clone.Name += " - Copy";
                    Project.SaveSlots.Insert(SaveSlotsGrid.Items.IndexOf(listSelectedItem) + 1, clone);
                }
            });
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void DebugButton_OnClick(object sender, RoutedEventArgs e) {
            foreach (var saveSlot in Project.SaveSlots) {
                saveSlot.RefreshHotkey();
            }
        }
    }
}
