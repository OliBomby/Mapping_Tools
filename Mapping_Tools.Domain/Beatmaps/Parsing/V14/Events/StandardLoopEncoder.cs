using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class StandardLoopEncoder : IEncoder<StandardLoop>
{
    public string Encode(StandardLoop obj)
    {
        return $"{obj.CommandType},{obj.StartTime.ToRoundInvariant()},{obj.LoopCount.ToInvariant()}";
    }
}