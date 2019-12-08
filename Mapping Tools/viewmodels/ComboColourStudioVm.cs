using Mapping_Tools.Classes.ComboColourStudio;
using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Viewmodels {

    public class ComboColourStudioVm : BindableBase {
        private ComboColourProject _project;

        public ComboColourStudioVm() {
            Project = new ComboColourProject();
        }

        public ComboColourProject Project {
            get => _project;
            set => Set(ref _project, value);
        }

        public string ExportPath { get; set; }
    }
}