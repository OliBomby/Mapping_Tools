using System.Text;
using Mapping_Tools.Core.BeatmapHelper.Events;
using Mapping_Tools.Core.MathUtil;
using static Mapping_Tools.Core.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.BeatmapHelper.Serialization;

internal static class EventTextCodec
{
    public static Event DecodeLine(string line)
    {
        string[] values = line.Split(',');
        Event result = values[0].Trim() switch
        {
            "0" => new Background(),
            "1" or "Video" => new Video(),
            "2" or "Break" => new Break(),
            "3" or "Colour" => new Colour(),
            "Sprite" => new Sprite(),
            "Animation" => new Animation(),
            "5" or "Sample" => new StoryboardSoundSample(),
            "P" => new ParameterCommand(),
            "L" => new StandardLoop(),
            "T" => new TriggerLoop(),
            _ => new OtherCommand(),
        };

        DecodeLine(result, line);
        return result;
    }

    public static void DecodeLine(Event target, string line)
    {
        string unindented = line[ParseIndents(line)..];
        string[] values = unindented.Split(',');

        switch (target)
        {
            case Background background:
                if (values[0] != "0")
                    throw new BeatmapParsingException("This line is not a background.", line);

                background.EventType = values[0];

                // Background start times are usually zero, but preserve the value if the file specifies one.
                background.StartTime = ParseDouble(values[1], "Failed to parse start time of background.", line);
                background.Filename = values[2].Trim('"');

                // Stable allows an image offset to be omitted; when present it contains both coordinates.
                if (values.Length > 3)
                {
                    double xOffset = ParseDouble(values[3], "Failed to parse X offset of background.", line);
                    double yOffset = ParseDouble(values[4], "Failed to parse Y offset of background.", line);
                    background.Pos = new Vector2(xOffset, yOffset);
                }
                else
                {
                    background.Pos = Vector2.Zero;
                }

                break;

            case Video video:
                // Preserve either token spelling; they describe the same video event.
                if (values[0] != "1" && values[0] != "Video")
                    throw new BeatmapParsingException("This line is not a video.", line);

                video.EventType = values[0];

                // Video start times are usually zero, but preserve the value if the file specifies one.
                video.StartTime = ParseDouble(values[1], "Failed to parse start time of video.", line);
                video.Filename = values[2].Trim('"');

                // A zero offset is omitted when videos are encoded.
                if (values.Length > 3)
                {
                    double xOffset = ParseDouble(values[3], "Failed to parse X offset of video.", line);
                    double yOffset = ParseDouble(values[4], "Failed to parse Y offset of video.", line);
                    video.Pos = new Vector2(xOffset, yOffset);
                }
                else
                {
                    video.Pos = Vector2.Zero;
                }

                break;

            case Break value:
                // Preserve either token spelling so a decode/encode roundtrip does not rewrite it.
                if (values[0] != "2" && values[0] != "Break")
                    throw new BeatmapParsingException("This line is not a break.", line);

                value.EventType = values[0];
                value.StartTime = ParseDouble(values[1], "Failed to parse start time of break.", line);
                value.EndTime = ParseDouble(values[2], "Failed to parse end time of break.", line);
                break;

            case Colour value:
                if (values[0] != "3" && values[0] != "Colour")
                    throw new BeatmapParsingException("This line is not a background colour transformation.", line);

                value.EventType = values[0];
                value.StartTime = ParseDouble(values[1], "Failed to parse start time of background colour transformation.", line);

                if (!TryParseInt(values[2], out int r))
                    throw new BeatmapParsingException("Failed to parse red component of background colour transformation.", line);

                if (!TryParseInt(values[3], out int g))
                    throw new BeatmapParsingException("Failed to parse green component of background colour transformation.", line);

                if (!TryParseInt(values[4], out int b))
                    throw new BeatmapParsingException("Failed to parse blue component of background colour transformation.", line);

                value.Color = RgbaColour.FromRgb((byte)r, (byte)g, (byte)b);
                break;

            case Sprite value:
                if (values[0] != "Sprite")
                    throw new BeatmapParsingException("This line is not a sprite.", line);

                if (!Enum.TryParse(values[1], out StoryboardLayer spriteLayer))
                    throw new BeatmapParsingException("Failed to parse layer of sprite.", line);

                if (!Enum.TryParse(values[2], out Origin spriteOrigin))
                    throw new BeatmapParsingException("Failed to parse origin of sprite.", line);

                value.Layer = spriteLayer;
                value.Origin = spriteOrigin;
                value.FilePath = values[3].Trim('"');
                value.Pos = new Vector2(
                    ParseDouble(values[4], "Failed to parse X position of sprite.", line),
                    ParseDouble(values[5], "Failed to parse Y position of sprite.", line));
                break;

            case Animation value:
                if (values[0] != "Animation")
                    throw new BeatmapParsingException("This line is not an animation.", line);

                if (!Enum.TryParse(values[1], out StoryboardLayer animationLayer))
                    throw new BeatmapParsingException("Failed to parse layer of animation.", line);

                if (!Enum.TryParse(values[2], out Origin animationOrigin))
                    throw new BeatmapParsingException("Failed to parse origin of animation.", line);

                value.Layer = animationLayer;
                value.Origin = animationOrigin;
                value.FilePath = values[3].Trim('"');
                value.Pos = new Vector2(
                    ParseDouble(values[4], "Failed to parse X position of animation.", line),
                    ParseDouble(values[5], "Failed to parse Y position of animation.", line));

                if (!TryParseInt(values[6], out int frameCount))
                    throw new BeatmapParsingException("Failed to parse frame count of animation.", line);

                value.FrameCount = frameCount;
                value.FrameDelay = ParseDouble(values[7], "Failed to parse frame delay of animation.", line);

                if (!Enum.TryParse(values[8], out LoopType animationLoopType))
                    throw new BeatmapParsingException("Failed to parse loop type of animation.", line);

                value.LoopType = animationLoopType;
                break;

            case StoryboardSoundSample value:
                // Example: Sample,56056,0,"soft-hitnormal.wav",30
                if (values[0] != "Sample" && values[0] != "5")
                    throw new BeatmapParsingException("This line is not a storyboarded sample.", line);

                value.StartTime = ParseDouble(values[1], "Failed to parse time of storyboarded sample.", line);

                if (!Enum.TryParse(values[2], out StoryboardLayer sampleLayer))
                    throw new BeatmapParsingException("Failed to parse layer of storyboarded sample.", line);

                value.Layer = sampleLayer;
                value.FilePath = values[3].Trim('"');

                if (values.Length > 4)
                {
                    value.Volume = ParseDouble(values[4], "Failed to parse volume of storyboarded sample.", line);
                }
                else
                {
                    value.Volume = 100;
                }

                break;

            case ParameterCommand value:
                if (!Enum.TryParse(values[1], out EasingType parameterEasing))
                    throw new BeatmapParsingException("Failed to parse easing of command.", line);

                value.Easing = parameterEasing;
                value.StartTime = ParseDouble(values[2], "Failed to parse start time of param command.", line);

                // An empty end time is the command shorthand for using the start time.
                value.EndTime = string.IsNullOrEmpty(values[3])
                    ? value.StartTime
                    : ParseDouble(values[3], "Failed to parse end time of param command.", line);
                value.Parameter = values[4];
                break;

            case StandardLoop value:
                value.StartTime = ParseDouble(values[1], "Failed to parse start time of event param.", line);

                if (!TryParseInt(values[2], out int loopCount))
                    throw new BeatmapParsingException("Failed to parse loop count of event param.", line);

                value.LoopCount = loopCount;
                break;

            case TriggerLoop value:
                value.TriggerName = values[1];
                value.StartTime = ParseDouble(values[2], "Failed to parse start time of event param.", line);
                value.EndTime = ParseDouble(values[3], "Failed to parse end time of event param.", line);
                break;

            case OtherCommand value:
                value.EventType = Enum.TryParse(values[0], out EventType eventType) ? eventType : EventType.Unknown;
                if (value.EventType == EventType.Unknown)
                    value.FallbackEventType = values[0];

                if (!Enum.TryParse(values[1], out EasingType easing))
                    throw new BeatmapParsingException("Failed to parse easing of command.", line);

                value.Easing = easing;
                value.StartTime = ParseDouble(values[2], "Failed to parse start time of command.", line);

                // An empty end time is the command shorthand for using the start time.
                value.EndTime = string.IsNullOrEmpty(values[3])
                    ? value.StartTime
                    : ParseDouble(values[3], "Failed to parse end time of command.", line);

                value.Params = new double[values.Length - 4];
                for (int i = 4; i < values.Length; i++)
                {
                    value.Params[i - 4] = ParseDouble(
                        values[i],
                        $"Failed to parse value at position {i} of command.",
                        line);
                }

                break;

            default:
                throw new NotSupportedException($"Unsupported storyboard event type '{target.GetType().Name}'.");
        }
    }

    public static string EncodeLine(Event value, int targetVersion)
    {
        bool fullPrecision = targetVersion >= 128;
        string formatNumber(double number) => fullPrecision ? number.ToInvariant() : number.ToRoundInvariant();

        switch (value)
        {
            case Background background:
                // osu! writes the image offset even when it is zero.
                return $"{background.EventType},{formatNumber(background.StartTime)},\"{background.Filename}\"," +
                    $"{background.Pos.X.ToInvariant()},{background.Pos.Y.ToInvariant()}";

            case Video video:
                // Videos omit an unused zero offset.
                if (video.Pos == Vector2.Zero)
                    return $"{video.EventType},{formatNumber(video.StartTime)},\"{video.Filename}\"";

                return $"{video.EventType},{formatNumber(video.StartTime)},\"{video.Filename}\"," +
                    $"{video.Pos.X.ToInvariant()},{video.Pos.Y.ToInvariant()}";

            case Break breakEvent:
                // Preserve the source token ("2" or "Break") when writing the event back.
                return $"{breakEvent.EventType},{formatNumber(breakEvent.StartTime)},{formatNumber(breakEvent.EndTime)}";

            case Colour colour:
                return $"{colour.EventType},{formatNumber(colour.StartTime)},{colour.Color.R},{colour.Color.G},{colour.Color.B}";

            case Sprite sprite:
                return $"Sprite,{sprite.Layer},{sprite.Origin},\"{sprite.FilePath}\"," +
                    $"{sprite.Pos.X.ToInvariant()},{sprite.Pos.Y.ToInvariant()}";

            case Animation animation:
                return $"Animation,{animation.Layer},{animation.Origin},\"{animation.FilePath}\"," +
                    $"{animation.Pos.X.ToInvariant()},{animation.Pos.Y.ToInvariant()}," +
                    $"{animation.FrameCount.ToInvariant()},{animation.FrameDelay.ToInvariant()},{animation.LoopType}";

            case StoryboardSoundSample sample:
                return $"Sample,{formatNumber(sample.StartTime)},{sample.Layer.ToIntInvariant()}," +
                    $"\"{sample.FilePath}\",{sample.Volume.ToRoundInvariant()}";

            case ParameterCommand parameter:
                string endTime = Precision.AlmostEquals(parameter.StartTime, parameter.EndTime)
                    ? ""
                    : formatNumber(parameter.EndTime);
                return $"P,{((int)parameter.Easing).ToInvariant()},{formatNumber(parameter.StartTime)},{endTime},{parameter.Parameter}";

            case StandardLoop loop:
                return $"L,{formatNumber(loop.StartTime)},{loop.LoopCount.ToInvariant()}";

            case TriggerLoop trigger:
                return $"T,{trigger.TriggerName},{formatNumber(trigger.StartTime)},{formatNumber(trigger.EndTime)}";

            case OtherCommand command:
                var builder = new StringBuilder(8 + command.Params.Length * 2);
                builder.Append(command.EventType == EventType.Unknown ? command.FallbackEventType : command.EventType.ToString())
                    .Append(',')
                    .Append(((int)command.Easing).ToInvariant())
                    .Append(',')
                    .Append(formatNumber(command.StartTime))
                    .Append(',');

                if (!Precision.AlmostEquals(command.StartTime, command.EndTime))
                    builder.Append(formatNumber(command.EndTime));

                foreach (double parameter in command.Params)
                    builder.Append(',').Append(parameter.ToInvariant());

                return builder.ToString();

            default:
                throw new NotSupportedException($"Unsupported storyboard event type '{value.GetType().Name}'.");
        }
    }

    public static IEnumerable<Event> DecodeTree(IEnumerable<string> lines)
    {
        var parents = new LinkedList<Event?>();
        Event? last = null;

        // Starting below level zero makes the first event create a null root parent.
        int lastIndent = -1;
        foreach (string line in lines)
        {
            Event value = DecodeLine(line);
            int indent = ParseIndents(line);

            // Store indentation on command nodes because it is part of their editable state.
            if (value is Command command)
                command.Indents = indent;

            if (indent > lastIndent)
            {
                // One more leading space means this command belongs to the previous event.
                parents.AddLast(last);
            }
            else if (indent < lastIndent)
            {
                // Each nested level adds exactly one space, so the indentation delta gives the pop count.
                for (int i = 0; i < lastIndent - indent; i++)
                    parents.RemoveLast();
            }

            // Return roots to the caller and attach indented commands to their parent event.
            Event? parent = parents.Last?.Value;
            if (parent == null)
            {
                yield return value;
            }
            else
            {
                parent.ChildEvents.Add(value);
                value.ParentEvent = parent;
            }

            last = value;
            lastIndent = indent;
        }
    }

    public static IEnumerable<string> EncodeTree(IEnumerable<Event> events, int targetVersion, int depth = 0)
    {
        foreach (Event value in events)
        {
            yield return new string(' ', depth) + EncodeLine(value, targetVersion);

            if (value.ChildEvents.Count > 0)
            {
                foreach (string child in EncodeTree(value.ChildEvents, targetVersion, depth + 1))
                    yield return child;
            }
        }
    }

    public static int ParseIndents(string line)
    {
        return line.TakeWhile(char.IsWhiteSpace).Count();
    }

    private static double ParseDouble(string value, string error, string line)
    {
        if (TryParseDouble(value, out double result))
            return result;

        throw new BeatmapParsingException(error, line);
    }
}
