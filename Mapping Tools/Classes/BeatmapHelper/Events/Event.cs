using System;

namespace Mapping_Tools.Classes.BeatmapHelper.Events {
    /// <summary>
    /// Abstract event type. Represents everything that can be put in the [Events] section.
    /// TODO: When actually doing storyboard stuff some of the types should have child and parent events instead of indents, so we get a tree structure. BTW this would break ITextLine
    /// </summary>
    public abstract class Event : ITextLine {
        /// <summary>
        /// Factory method for making an <see cref="Event"/> from a serialized line of .osu code.
        /// Automatically recognizes the type of the event from the string and makes the appropriate object.
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        public static Event MakeEvent(string line) {
            string[] values = line.Split(',');
            string eventType = values[0].Trim();

            Event myEvent;
            switch (eventType) {
                case "0":
                    myEvent = new Background();
                    break;
                case "1":
                case "Video":
                    myEvent = new Video();
                    break;
                case "2":
                case "Break":
                    myEvent = new Break();
                    break;
                case "Sprite":
                    myEvent = new Sprite();
                    break;
                case "Animation":
                    myEvent = new Animation();
                    break;
                case "Sample":
                    myEvent = new StoryboardSoundSample();
                    break;
                case "P":
                    myEvent = new ParameterCommand();
                    break;
                case "L":
                    myEvent = new StandardLoop();
                    break;
                case "T":
                    myEvent = new TriggerLoop();
                    break;
                default:
                    myEvent = new OtherCommand();
                    break;
            }

            myEvent.SetLine(line);

            return myEvent;
        }

        public abstract string GetLine();

        public abstract void SetLine(string line);
    }
}