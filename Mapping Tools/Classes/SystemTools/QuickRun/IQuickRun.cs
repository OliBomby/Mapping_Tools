using System;

namespace Mapping_Tools.Classes.SystemTools {
    public interface IQuickRun {
        void QuickRun();
        event EventHandler RunFinished;
    }
}
