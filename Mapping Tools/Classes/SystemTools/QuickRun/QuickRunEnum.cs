using System.ComponentModel;

namespace Mapping_Tools.Classes.SystemTools {
    public enum SingleQuickRunEnum {
        [Description("<Current Tool>")]
        Current = 0,
        [Description("Map Cleaner")]
        Cleaner = 1,
        [Description("Slider Completionator")]
        Completionator = 2,
    }

    public enum MultipleQuickRunEnum {
        [Description("<Current Tool>")]
        Current = 0,
        [Description("Map Cleaner")]
        Cleaner = 1,
        [Description("Slider Completionator")]
        Completionator = 2,
        [Description("Slider Merger")]
        Merger = 3,
    }
}
