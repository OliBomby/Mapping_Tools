namespace Mapping_Tools_Core.Tools.PatternGallery {
    public class SelectedPatternImportVm : BindableBase {
        private string _name;

        [DisplayName("Name")]
        [Description("The name for the pattern.")]
        public string Name { 
            get => _name; 
            set => Set(ref _name, value);
        }
    }
}