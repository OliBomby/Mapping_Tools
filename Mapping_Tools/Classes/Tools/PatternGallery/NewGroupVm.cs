using Mapping_Tools.Classes.SystemTools;
using System.ComponentModel;
using Mapping_Tools.Core.Classes.SystemTools;

namespace Mapping_Tools.Classes.Tools.PatternGallery {
    public class NewGroupVm : BindableBase {
        private string groupName;

        [DisplayName("Group name")]
        [Description("The name for new group.")]
        public string GroupName {
            get => groupName;
            set => Set(ref groupName, value);
        }
    }
}