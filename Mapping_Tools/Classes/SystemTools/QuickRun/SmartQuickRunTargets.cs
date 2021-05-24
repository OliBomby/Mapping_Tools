using System;

namespace Mapping_Tools.Classes.SystemTools.QuickRun {
    [Flags]
    public enum SmartQuickRunTargets {
        NoSelection = 1,
        SingleSelection = 1 << 1,
        MultipleSelection = 1 << 2,

        AnySelection = SingleSelection | MultipleSelection,
        Always = NoSelection | SingleSelection | MultipleSelection,
    }
}