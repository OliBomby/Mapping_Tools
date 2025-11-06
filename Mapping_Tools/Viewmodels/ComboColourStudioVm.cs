using Mapping_Tools.Classes.SystemTools;
using Mapping_Tools.Classes.Tools.ComboColourStudio;

namespace Mapping_Tools.Viewmodels;

public class ComboColourStudioVm : BindableBase {
    private ComboColourProject project;

    public ComboColourStudioVm() {
        Project = new ComboColourProject();
    }

    public ComboColourProject Project {
        get => project;
        set => Set(ref project, value);
    }

    public string ExportPath { get; set; }
}