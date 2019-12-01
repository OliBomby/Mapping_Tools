namespace Mapping_Tools.Views
{
    namespace TimingHelper.Serialization
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
