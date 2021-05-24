using System;

namespace Mapping_Tools.Classes.SystemTools.QuickRun {
    /// <summary>
    /// Attributes an IQuickRun tool to show up in SmartQuickRun options
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class SmartQuickRunUsageAttribute : Attribute {
        public SmartQuickRunTargets Targets;
        public SmartQuickRunUsageAttribute(SmartQuickRunTargets targets) {
            Targets = targets;
        }
    }
}