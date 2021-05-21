namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    public abstract class Command : Event, IHasStartTime {
        public int Indents { get; set; }
        public virtual EventType EventType { get; set; }
        public int StartTime { get; set; }
    }
}