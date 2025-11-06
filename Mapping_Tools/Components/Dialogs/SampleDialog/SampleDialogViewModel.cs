using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Components.Dialogs.SampleDialog;

public class SampleDialogViewModel : BindableBase
{
    private string name;

    public string Name {
        get => name;
        set => Set(ref name, value);
    }
}