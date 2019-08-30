using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.SystemTools {
    public interface IQuickRun {
        void QuickRun();
        event EventHandler RunFinished;
    }
}
