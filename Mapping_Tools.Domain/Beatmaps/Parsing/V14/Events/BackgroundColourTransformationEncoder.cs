using Mapping_Tools.Domain.Beatmaps.Events;

namespace Mapping_Tools.Domain.Beatmaps.Parsing.V14.Events;

public class BackgroundColourTransformationEncoder : IEncoder<BackgroundColourTransformation>
{
    public string Encode(BackgroundColourTransformation obj)
    {
        return $"{obj.EventType},{obj.StartTime.ToRoundInvariant()},{obj.R.ToRoundInvariant()},{obj.G.ToRoundInvariant()},{obj.B.ToRoundInvariant()}";
    }
}