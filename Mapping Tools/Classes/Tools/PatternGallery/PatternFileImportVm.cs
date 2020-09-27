using System.ComponentModel;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Dialogs.CustomDialog;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class PatternFileImportVm : BindableBase {
        private string _name;
        private string _filePath;

        [DisplayName("Name")]
        [Description("The name for the pattern.")]
        public string Name { 
            get => _name; 
            set => Set(ref _name, value);
        }

        [BeatmapBrowse]
        [DisplayName("Pattern file path")]
        [Description("The path to the pattern file to import.")]
        public string FilePath {
            get => _filePath;
            set => Set(ref _filePath, value);
        }
    }
}