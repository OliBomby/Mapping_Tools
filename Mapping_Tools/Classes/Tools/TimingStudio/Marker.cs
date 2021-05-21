namespace Mapping_Tools.Classes.Tools.TimingStudio
{
    namespace TimingStudio
    {
        public class Marker {
            public double Time { get; }
            public double BeatsFromLastMarker { get; set; }

            public Marker(double time) {
                Time = time;
                BeatsFromLastMarker = 0;
            }
        }
    }
}
