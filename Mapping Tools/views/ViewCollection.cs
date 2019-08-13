using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Reflection;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Views {
    public class ViewCollection {
        public void AutoSaveSettings() {
            foreach (var prop in typeof(ViewCollection).GetProperties(BindingFlags.NonPublic | BindingFlags.Instance)) {
                var value = prop.GetValue(this);
                if (value == null)
                    continue;
                if (ProjectManager.IsSavable(value)) {
                    dynamic v = prop.GetValue(this);
                    ProjectManager.SaveProject(v);
                }
            }
        }

        private UserControl Standard { get; set; }
        public UserControl GetStandard() {
            if (Standard == null) {
                Standard = new StandardView();
            }
            return Standard;
        }

        private UserControl Preferences { get; set; }
        public UserControl GetPreferences() {
            if (Preferences == null) {
                Preferences = new PreferencesView();
            }
            return Preferences;
        }

        private UserControl MapCleaner { get; set; }
        public UserControl GetMapCleaner() {
            if (MapCleaner == null) {
                MapCleaner = new CleanerView();
            }
            return MapCleaner;
        }

        private UserControl MatadataManager { get; set; }
        public UserControl GetMetadataManager() {
            if (MatadataManager == null) {
                MatadataManager = new MetadataManagerView();
            }
            return MatadataManager;
        }

        private UserControl HitsoundPreviewHelper { get; set; }
        public UserControl GetHitsoundPreviewHelper() {
            if (HitsoundPreviewHelper == null) {
                HitsoundPreviewHelper = new HitsoundPreviewHelperView();
            }
            return HitsoundPreviewHelper;
        }

        private UserControl PropertyTransformer { get; set; }
        public UserControl GetPropertyTransformer() {
            if (PropertyTransformer == null) {
                PropertyTransformer = new PropertyTransformerView();
            }
            return PropertyTransformer;
        }

        private UserControl HitsoundCopier { get; set; }
        public UserControl GetHitsoundCopier() {
            if (HitsoundCopier == null) {
                HitsoundCopier = new HitsoundCopierView();
            }
            return HitsoundCopier;
        }

        private UserControl HitsoundStudio { get; set; }
        public UserControl GetHitsoundStudio() {
            if (HitsoundStudio == null) {
                HitsoundStudio = new HitsoundStudioView();
            }
            return HitsoundStudio;
        }

        private UserControl SliderCompletionator { get; set; }
        public UserControl GetSliderCompletionator() {
            if (SliderCompletionator == null) {
                SliderCompletionator = new SliderCompletionatorView();
            }
            return SliderCompletionator;
        }

        private UserControl SliderMerger { get; set; }
        public UserControl GetSliderMerger() {
            if (SliderMerger == null) {
                SliderMerger = new SliderMergerView();
            }
            return SliderMerger;
        }

        private UserControl SnappingTools { get; set; }
        public UserControl GetSnappingTools() {
            if (SnappingTools == null) {
                SnappingTools = new SnappingToolsView();
            }
            return SnappingTools;
        }

        private UserControl TimingHelper { get; set; }
        public UserControl GetTimingHelper() {
            if (TimingHelper == null) {
                TimingHelper = new TimingHelperView();
            }
            return TimingHelper;
        }
    }
}
