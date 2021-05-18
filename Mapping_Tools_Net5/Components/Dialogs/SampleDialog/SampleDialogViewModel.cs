using Mapping_Tools.Classes.SystemTools;

namespace Mapping_Tools.Components.Dialogs.SampleDialog {
    public class SampleDialogViewModel : BindableBase
    {
        private string _name;

        public string Name {
            get => _name;
            set => Set(ref _name, value);
        }
    }
}