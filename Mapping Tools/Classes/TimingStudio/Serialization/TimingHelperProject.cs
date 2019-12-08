using Mapping_Tools.Classes.SystemTools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapping_Tools.Classes.TimingStudio.Serialization
{
    /// <summary>
    /// 
    /// </summary>
    public class TimingStudioProject : BindableBase, IDisposable
    {
        private string baseBeatmap;
        public string BaseBeatmap { get => baseBeatmap; set => Set(ref baseBeatmap, value); }
        private TimingStudioPreferences timingStudioPreferences;
        public TimingStudioPreferences StudioPreferences { get => timingStudioPreferences;
                                                            set => Set(ref timingStudioPreferences, value); }



        public TimingStudioProject()
        {
        }



        public void Dispose()
        {
            
        }


    }
}
