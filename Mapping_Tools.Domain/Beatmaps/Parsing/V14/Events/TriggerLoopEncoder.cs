using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class TriggerLoopEncoder : IEncoder<TriggerLoop>
{
    public string Encode(TriggerLoop obj)
    {
        return !obj.DurationDefined
            ? $"{obj.CommandType},{obj.TriggerName}"
            : $"{obj.CommandType},{obj.TriggerName},{obj.StartTime.ToRoundInvariant()},{obj.EndTime.ToRoundInvariant()}";
    }
}