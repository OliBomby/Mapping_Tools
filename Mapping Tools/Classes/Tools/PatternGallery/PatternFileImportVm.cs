using System.ComponentModel;
using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Components.Dialogs.CustomDialog;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class PatternFileImportVm : BindableBase {
        private string _name = string.Empty;
        private string _filePath = string.Empty;
        private string _filter = string.Empty;
        private double _startTime = -1;
        private double _endTime = -1;

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

        [DisplayName("Filter")]
        [Description("Input an optional time code here. Example time code: 00:56:823 (1,2,1,2) - ")]
        public string Filter {
            get => _filter;
            set => Set(ref _filter, value);
        }

        [TimeInput]
        [ConverterParameter(-1)]
        [DisplayName("Start time")]
        [Description("Optional lower bound time. All objects before this time will be ignored.")]
        public double StartTime {
            get => _startTime;
            set => Set(ref _startTime, value);
        }

        [TimeInput]
        [ConverterParameter(-1)]
        [DisplayName("End time")]
        [Description("Optional upper bound time. All objects after this time will be ignored.")]
        public double EndTime {
            get => _endTime;
            set => Set(ref _endTime, value);
        }
    }
}