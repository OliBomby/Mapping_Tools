using System;

namespace Mapping_Tools.Classes.SystemTools.QuickRun {
    /// <summary>
    /// Interface for the Quick Runnable Tools.
    /// </summary>
    public interface IQuickRun {
        /// <summary>
        /// 
        /// </summary>
        void QuickRun();
        /// <summary>
        /// 
        /// </summary>
        event EventHandler RunFinished;
    }
}
