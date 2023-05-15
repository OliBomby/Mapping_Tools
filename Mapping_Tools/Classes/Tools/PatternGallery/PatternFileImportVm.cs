using System.ComponentModel;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Dialogs.CustomDialog;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class PatternFileImportVm : BindableBase {
        private string name = string.Empty;
        private string filePath = string.Empty;
        private string filter = string.Empty;
        private double startTime = -1;
        private double endTime = -1;

        [DisplayName("Name")]
        [Description("The name for the pattern.")]
        public string Name { 
            get => name; 
            set => Set(ref name, value);
        }

        [BeatmapBrowse]
        [DisplayName("Pattern file path")]
        [Description("The path to the pattern file to import.")]
        public string FilePath {
            get => filePath;
            set => Set(ref filePath, value);
        }

        [DisplayName("Filter")]
        [Description("Input an optional time code here. Example time code: 00:56:823 (1,2,1,2) - ")]
        public string Filter {
            get => filter;
            set => Set(ref filter, value);
        }

        [TimeInput]
        [ConverterParameter(-1)]
        [DisplayName("Start time")]
        [Description("Optional lower bound time. All objects before this time will be ignored.")]
        public double StartTime {
            get => startTime;
            set => Set(ref startTime, value);
        }

        [TimeInput]
        [ConverterParameter(-1)]
        [DisplayName("End time")]
        [Description("Optional upper bound time. All objects after this time will be ignored.")]
        public double EndTime {
            get => endTime;
            set => Set(ref endTime, value);
        }
    }
}