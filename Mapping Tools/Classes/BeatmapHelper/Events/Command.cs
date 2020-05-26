using System.Linq;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    public abstract class Command : Event {
        public int Indents { get; set; }
        public virtual EventType EventType { get; set; }
        public int StartTime { get; set; }


        public string GetIndents() {
            return new string(' ', Indents);
        }

        /// <summary>
        /// Counts the indents on a line and sets the <see cref="Indents"/> property.
        /// </summary>
        /// <param name="line"></param>
        /// <returns>The input string without the indents.</returns>
        public string ParseIndents(string line) {
            int indents = line.TakeWhile(char.IsWhiteSpace).Count();
            Indents = indents;
            return line.Substring(Indents);
        }
    }
}