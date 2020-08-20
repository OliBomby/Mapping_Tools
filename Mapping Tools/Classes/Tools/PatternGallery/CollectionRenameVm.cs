using System.ComponentModel;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class CollectionRenameVm {
        [DisplayName("New name")]
        [Description("The new name for the collection's directory in the Pattern Files directory.")]
        public string NewName { get; set; }
    }
}