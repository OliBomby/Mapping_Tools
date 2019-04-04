using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Mapping_Tools.Views {
    public class ViewCollection {
        public void SaveSettings() {
            if (HitsoundMaker != null) {
                MainWindow.AppWindow.settingsManager.settings.HitsoundMakerSettings = ((HitsoundMakerView)HitsoundMaker).GetSettings();
            }
        }

        private UserControl Standard;
        public UserControl GetStandard() {
            if (Standard == null) {
                Standard = new StandardView();
            }
            return Standard;
        }

        private UserControl Preferences;
        public UserControl GetPreferences() {
            if (Preferences == null) {
                Preferences = new PreferencesView();
            }
            return Preferences;
        }

        private UserControl MapCleaner;
        public UserControl GetMapCleaner() {
            if (MapCleaner == null) {
                MapCleaner = new CleanerView();
            }
            return MapCleaner;
        }

        private UserControl HitsoundCopier;
        public UserControl GetHitsoundCopier() {
            if (HitsoundCopier == null) {
                HitsoundCopier = new HitsoundCopierView();
            }
            return HitsoundCopier;
        }

        private UserControl HitsoundMaker;
        public UserControl GetHitsoundMaker() {
            if (HitsoundMaker == null) {
                HitsoundMaker = new HitsoundMakerView();
            }
            return HitsoundMaker;
        }

        private UserControl SliderCompletionator;
        public UserControl GetSliderCompletionator() {
            if (SliderCompletionator == null) {
                SliderCompletionator = new SliderCompletionatorView();
            }
            return SliderCompletionator;
        }

        private UserControl SliderMerger;
        public UserControl GetSliderMerger() {
            if (SliderMerger == null) {
                SliderMerger = new SliderMergerView();
            }
            return SliderMerger;
        }

        private UserControl SnappingTools;
        public UserControl GetSnappingTools() {
            if (SnappingTools == null) {
                SnappingTools = new SnappingToolsView();
            }
            return SnappingTools;
        }
    }
}
