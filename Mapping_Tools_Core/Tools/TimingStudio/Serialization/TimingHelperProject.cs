using System;

namespace Mapping_Tools_Core.Tools.TimingStudio.Serialization
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
