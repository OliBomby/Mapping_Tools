using Mapping_Tools.Classes.SystemTools;
using System.ComponentModel;

namespace Mapping_Tools.Classes.Tools.PatternGallery;

public class SelectedPatternImportVm : BindableBase {
    private string name;

    [DisplayName("Name")]
    [Description("The name for the pattern.")]
    public string Name { 
        get => name; 
        set => Set(ref name, value);
    }
}