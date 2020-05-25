using System;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Abstract event type. Represents everything that can be put in the [Events] section.
    /// </summary>
    public abstract class Event : ITextLine {
        /// <summary>
        /// Factory method for making an <see cref="Event"/> from a serialized line of .osu code.
        /// Automatically recognizes the type of the event from the string and makes the appropriate object.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public Event MakeEvent(string line) {
            throw new NotImplementedException();
        }

        public abstract string GetLine();

        public abstract void SetLine(string line);
    }
}