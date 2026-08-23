using System.Text;
using Mapping_Tools.Core.Classes.MathUtil;
using static Mapping_Tools.Core.Classes.BeatmapHelper.FileFormatHelper;

namespace Mapping_Tools.Core.Classes.BeatmapHelper.Events;

/// <summary>
///     Represents all the commands
///     The exceptions being loops and triggers because these have different syntax.
/// </summary>
#nullable disable
public class OtherCommand : Command, IHasEndTime
{
    /// <summary>
    ///     Gets or sets the interpolation curve applied between parameter values.
    /// </summary>
    public EasingType Easing { get; set; }

    /// <summary>
    ///     All other parameters
    /// </summary>
    public double[] Params { get; set; }

    /// <summary>
    ///     Used to describe <see cref="EventType" /> in case it is Unknown.
    /// </summary>
    public string FallbackEventType { get; set; }

    /// <summary>
    ///     <inheritdoc />
    public double EndTime { get; set; }

    /// <summary>
    ///     <inheritdoc />
    public override string GetLine()
    {
        var builder = new StringBuilder(8 + Params.Length * 2);

        builder.Append(EventType == EventType.Unknown ? FallbackEventType : EventType.ToString());
        builder.Append(',');
        builder.Append(((int)Easing).ToInvariant());
        builder.Append(',');
        builder.Append(SaveWithFloatPrecision ? StartTime.ToInvariant() : StartTime.ToRoundInvariant());
        builder.Append(',');
        if (!Precision.AlmostEquals(StartTime, EndTime)) builder.Append(SaveWithFloatPrecision ? EndTime.ToInvariant() : EndTime.ToRoundInvariant());

        foreach (double param in Params)
        {
            builder.Append(',');
            builder.Append(param.ToInvariant());
        }

        return builder.ToString();
    }

    /// <summary>
    ///     <inheritdoc />
    public override void SetLine(string line)
    {
        string subLine = RemoveIndents(line);
        string[] values = subLine.Split(',');

        EventType = Enum.TryParse(values[0], out EventType eventType) ? eventType : EventType.Unknown;

        if (EventType == EventType.Unknown) FallbackEventType = values[0];

        if (Enum.TryParse(values[1], out EasingType easingType))
            Easing = easingType;
        else throw new BeatmapParsingException("Failed to parse easing of command.", line);

        if (TryParseDouble(values[2], out double startTime))
            StartTime = startTime;
        else throw new BeatmapParsingException("Failed to parse start time of command.", line);

        // Set end time to start time if empty. This accounts for the shorthand
        if (string.IsNullOrEmpty(values[3]))
        {
            EndTime = StartTime;
        }
        else
        {
            if (TryParseDouble(values[3], out double endTime))
                EndTime = endTime;
            else throw new BeatmapParsingException("Failed to parse end time of command.", line);
        }

        Params = new double[values.Length - 4];
        for (int i = 4; i < values.Length; i++)
        {
            string stringValue = values[i];
            int index = i - 4;

            if (TryParseDouble(stringValue, out double value))
                Params[index] = value;
            else throw new BeatmapParsingException($"Failed to parse value at position {i} of command.", line);
        }
    }
}
