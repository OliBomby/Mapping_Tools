using Mapping_Tools.Classes.SystemTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Viewmodels {
    public class MainWindowVm : BindableBase {
        private object view;
        public object View {
            get => view;
            set => Set(ref view, value);
        }
    }
}
