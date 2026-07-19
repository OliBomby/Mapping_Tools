namespace Mapping_Tools.Classes.BeatmapHelper.Events {
#nullable disable

    public abstract class Command : Event, IHasStartTime {
        public int Indents { get; set; }
        public virtual EventType EventType { get; set; }
        public double StartTime { get; set; }
    }
}
