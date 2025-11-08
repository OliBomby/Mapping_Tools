using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class BreakEncoder : IEncoder<Break>
{
    public string Encode(Break obj)
    {
        return $"{obj.EventType},{obj.StartTime.ToRoundInvariant()},{obj.EndTime.ToRoundInvariant()}";
    }
}